using System.Text.Json;

public sealed class RemoteGameLoop
{
    private readonly RemoteGameOptions _options;
    private readonly RemoteGameWebSocketClient _client;
    private readonly GameScreen _screen;

    private RemoteGameState? _state;
    private List<string> _roomPlayers = new();
    private string _status = "Connecting...";
    private bool _createSent;
    private long? _promptedVersion;

    public RemoteGameLoop(RemoteGameOptions options, GameScreen screen)
    {
        _options = options;
        _client = new RemoteGameWebSocketClient();
        _screen = screen;
    }

    public async Task<RemoteGameLoopResult> RunAsync(CancellationToken cancellationToken)
    {
        await using (_client)
        {
            await _client.ConnectAsync(_options, cancellationToken);
            await _client.SendJoinAsync(_options.PlayerId, cancellationToken);
            _status = $"Connected to {_options.GameId} as {_options.PlayerId}.";
            Render();

            await using var listener = new JsonEventListener(_client.ReceiveAsync);

            while (!cancellationToken.IsCancellationRequested)
            {
                var updated = DrainMessages(listener);
                if (updated)
                {
                    Render();
                }

                if (_state is null)
                {
                    var command = await _screen.ReadCommandAsync(
                        _state,
                        _options.PlayerId,
                        _options.GameId,
                        _status,
                        _roomPlayers,
                        _options.AutoStart,
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

                        await _client.SendCreateAsync(_options.PlayerId, _options.Seed, cancellationToken);
                        _createSent = true;
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

                if (ShouldAutoStart())
                {
                    await _client.SendCreateAsync(_options.PlayerId, _options.Seed, cancellationToken);
                    _createSent = true;
                    _status = "Start command sent.";
                    Render();
                }

                if (ShouldPromptForAction())
                {
                    var action = await _screen.ReadInputAsync(
                        _state!,
                        _options.PlayerId,
                        _options.GameId,
                        _status,
                        _roomPlayers,
                        _options.AutoStart,
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);
                    _promptedVersion = _state!.Version;
                    await _client.SendActionAsync(_options.PlayerId, action, cancellationToken);
                    _status = $"Sent: {action.Type} {action.Action}".TrimEnd();
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
        var type = ReadString(message, "type");
        switch (type)
        {
            case "RoomJoined":
                HandleRoomJoined(message);
                return;
            case "CommandApplied":
                HandleCommandApplied(message);
                return;
            case "CommandRejected":
            case "OperationRejected":
            case "ChatRejected":
                _status = $"Server rejected request: {ReadString(message, "error")}";
                if (type == "CommandRejected" && ReadNestedString(message, "command", "type").Equals("Initialize", StringComparison.OrdinalIgnoreCase))
                {
                    _createSent = false;
                }
                return;
            case "ChatPosted":
            case "ServerAnnouncement":
                var author = ReadNestedString(message, "chat", "playerId");
                var text = ReadNestedString(message, "chat", "text");
                _status = $"Chat {author}: {text}";
                return;
            default:
                _status = $"Received: {type}";
                return;
        }
    }

    private void HandleRoomJoined(JsonElement message)
    {
        if (TryGetPropertyIgnoreCase(message, "room", out var room) &&
            TryGetPropertyIgnoreCase(room, "players", out var players) &&
            players.ValueKind == JsonValueKind.Array)
        {
            _roomPlayers = players.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        var joinedPlayer = ReadString(message, "playerId");
        _status = $"Player joined: {joinedPlayer}";
    }

    private void HandleCommandApplied(JsonElement message)
    {
        if (TryGetPropertyIgnoreCase(message, "state", out var snapshot) && snapshot.ValueKind == JsonValueKind.Object)
        {
            _state = RemoteGameState.FromSnapshot(snapshot);
        }

        var appliedBy = ReadNestedString(message, "command", "playerId");
        var commandType = ReadNestedString(message, "command", "type");
        _status = $"Applied: {commandType} by {appliedBy}";
    }

    private bool ShouldAutoStart() =>
        _options.AutoStart && !_createSent && _state is null && _roomPlayers.Count >= 2;

    private bool ShouldPromptForAction() =>
        _state is not null &&
        _state.CurrentPlayerId.Equals(_options.PlayerId, StringComparison.OrdinalIgnoreCase) &&
        _state.PossibleActions.Count > 0 &&
        _promptedVersion != _state.Version;

    private void Render()
    {
        _screen.Render(_state, _options.PlayerId, _options.GameId, _status, _roomPlayers, _options.AutoStart);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ReadNestedString(JsonElement element, string parentProperty, string childProperty)
    {
        if (TryGetPropertyIgnoreCase(element, parentProperty, out var parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            TryGetPropertyIgnoreCase(parent, childProperty, out var child) &&
            child.ValueKind == JsonValueKind.String)
        {
            return child.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
