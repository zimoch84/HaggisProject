using System.Text.Json;

public sealed class GameController
{
    private static readonly TimeSpan SnapshotRetryInterval = TimeSpan.FromMilliseconds(750);

    private readonly RemoteGameOptions _options;
    private readonly RemoteGameWebSocketClient _client;
    private readonly GameScreen _screen;
    private readonly GameState _gameState = new();

    private RemoteGameSnapshotDto? _state;
    private List<string> _roomPlayers = new();
    private string _status = "Connecting...";
    private long? _promptedVersion;
    private DateTimeOffset _lastSnapshotRequestAt = DateTimeOffset.MinValue;
    private bool _snapshotRequestedAfterRoomUpdate;

    public GameController(RemoteGameOptions options, GameScreen screen)
    {
        _options = options;
        _client = new RemoteGameWebSocketClient();
        _screen = screen;
        _gameState.PlayerId = options.PlayerId;
        _gameState.GameId = options.GameId;
    }

    public async Task<RemoteGameLoopResult> RunAsync(CancellationToken cancellationToken)
    {
        await using (_client)
        {
            await _client.ConnectAsync(_options, cancellationToken);
            await using var listener = new JsonEventListener(_client.ReceiveAsync);
            await _client.SendJoinAsync(_options.PlayerId, cancellationToken);
            await RequestSnapshotAsync(cancellationToken);
            _status = $"Connected to {_options.GameId} as {_options.PlayerId}.";
            Render();

            while (!cancellationToken.IsCancellationRequested)
            {
                var updated = DrainMessages(listener);
                if (updated)
                {
                    Render();
                }

                if (!IsGameInitialized())
                {
                    var command = await _screen.ReadCommandAsync(
                        BuildGameState(),
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);

                    if (string.Equals(command, "/back", StringComparison.OrdinalIgnoreCase))
                    {
                        return RemoteGameLoopResult.BackToLobby;
                    }

                    if (string.Equals(command, "/start", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_roomPlayers.Count < 2)
                        {
                            _status = "Need at least 2 players to start the game.";
                            Render();
                            continue;
                        }

                        await _client.SendCreateAsync(_options.PlayerId, _options.Seed, GetRequestedPlayerCount(), cancellationToken);
                        await RequestSnapshotAsync(cancellationToken);
                        _status = "Start command sent.";
                        Render();
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        _status = "Available commands: /start, /back";
                        Render();
                    }

                    continue;
                }

                if (ShouldRefreshSnapshot())
                {
                    await RequestSnapshotAsync(cancellationToken);
                }

                if (ShouldPromptForAction())
                {
                    var currentState = _state!;
                    var action = await _screen.ReadInputAsync(
                        BuildGameState(),
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);
                    _promptedVersion = currentState.Version ?? 0;
                    await _client.SendActionAsync(_options.PlayerId, action, cancellationToken);
                    _status = $"Sent: {action.Type ?? string.Empty} {action.Action ?? string.Empty}".TrimEnd();
                    Render();
                    continue;
                }

                await Task.Delay(50, cancellationToken);
            }
        }

        return RemoteGameLoopResult.Closed;
    }

    private bool DrainMessages(JsonEventListener listener)
    {
        var updated = false;
        while (listener.TryRead(out var message))
        {
            using (message)
            {
                HandleMessage(message.RootElement);
                updated = true;
            }
        }

        return updated;
    }

    private void HandleMessage(JsonElement message)
    {
        var dto = RemoteGameMessageParser.Parse(message);
        var type = dto?.Type ?? string.Empty;
        switch (type)
        {
            case "RoomJoined":
                HandleRoomJoined(dto);
                return;
            case "GameSnapshot":
                HandleSnapshot(dto);
                return;
            case "CommandApplied":
                HandleCommandApplied(dto);
                return;
            case "SnapshotRejected":
                _status = $"Snapshot rejected: {dto?.Error ?? string.Empty}";
                return;
            case "CommandRejected":
            case "OperationRejected":
            case "ChatRejected":
                _status = $"Server rejected request: {dto?.Error ?? string.Empty}";
                return;
            case "ChatPosted":
            case "ServerAnnouncement":
                var author = dto?.Chat?.PlayerId ?? string.Empty;
                var text = dto?.Chat?.Text ?? string.Empty;
                _status = $"Chat {author}: {text}";
                return;
            default:
                _status = $"Received: {type}";
                return;
        }
    }

    private void HandleRoomJoined(RemoteGameInboundMessageDto? message)
    {
        _roomPlayers = message?.Room?.Players?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x ?? string.Empty)
            .ToList() ?? new List<string>();

        _snapshotRequestedAfterRoomUpdate = false;
        var joinedPlayer = message?.PlayerId ?? string.Empty;
        _status = $"Player joined: {joinedPlayer}";
    }

    private void HandleCommandApplied(RemoteGameInboundMessageDto? message)
    {
        if (message?.State is not null)
        {
            _state = message.State;
        }

        var appliedBy = message?.Command?.PlayerId ?? string.Empty;
        var commandType = message?.Command?.Type ?? string.Empty;
        _status = $"Applied: {commandType} by {appliedBy}";
    }

    private void HandleSnapshot(RemoteGameInboundMessageDto? message)
    {
        if (message?.State is not null)
        {
            _state = message.State;
            _status = (_state.Data?.Players?.Count ?? 0) > 0
                ? $"Snapshot loaded. Current player: {_state.Data?.CurrentPlayerId ?? string.Empty}"
                : "Snapshot loaded. Game not initialized yet.";
            return;
        }

        _status = "Snapshot received without state.";
    }

    private int? GetRequestedPlayerCount()
    {
        return _roomPlayers.Count is 2 or 3
            ? _roomPlayers.Count
            : null;
    }

    private bool IsGameInitialized()
    {
        if (_state is null)
        {
            return false;
        }

        if ((_state.Version ?? 0) > 0 && !string.IsNullOrWhiteSpace(_state.Data?.CurrentPlayerId))
        {
            return true;
        }

        var lastCommandType = _state.Data?.LastCommand?.Type;
        return string.Equals(lastCommandType, "Initialize", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldRefreshSnapshot()
    {
        if (_state is null)
        {
            return DateTimeOffset.UtcNow - _lastSnapshotRequestAt >= SnapshotRetryInterval;
        }

        if (_roomPlayers.Count > 0 &&
            (_state.Data?.Players?.Count ?? 0) != _roomPlayers.Count &&
            !_snapshotRequestedAfterRoomUpdate &&
            DateTimeOffset.UtcNow - _lastSnapshotRequestAt >= SnapshotRetryInterval)
        {
            return true;
        }

        return false;
    }

    private bool ShouldPromptForAction() =>
        _state is not null &&
        string.Equals(_state.Data?.CurrentPlayerId, _options.PlayerId, StringComparison.OrdinalIgnoreCase) &&
        (_state.Data?.PossibleActions?.Count ?? 0) > 0 &&
        _promptedVersion != (_state.Version ?? 0);

    private async Task RequestSnapshotAsync(CancellationToken cancellationToken)
    {
        _lastSnapshotRequestAt = DateTimeOffset.UtcNow;
        _snapshotRequestedAfterRoomUpdate = _state is not null;
        await _client.SendSnapshotAsync(_options.PlayerId, cancellationToken);
    }

    private void Render()
    {
        _screen.Render(BuildGameState());
    }

    private GameState BuildGameState()
    {
        _gameState.Snapshot = _state;
        _gameState.Status = _status;
        _gameState.RoomPlayers = _roomPlayers;
        return _gameState;
    }
}
