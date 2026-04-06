using System.Text.Json;
using Haggis.ConsoleUI.Presentation.Panels.InputActions;
using Haggis.ConsoleUI.Presentation.Screens;
using Haggis.ConsoleUI.Presentation.ViewModels.Game;

namespace Haggis.ConsoleUI.Application.Game;

public sealed class GameController
{
    private static readonly TimeSpan SnapshotRetryInterval = TimeSpan.FromMilliseconds(750);

    private readonly RemoteGameOptions _options;
    private readonly RemoteGameWebSocketClient _client;
    private readonly GameScreen _screen;
    private readonly RoundOverScreen _roundOverScreen = new();
    private readonly ScoreHistoryScreen _scoreHistoryScreen = new();
    private readonly GameState _gameState = new();

    private RemoteGameSnapshotDto? _state;
    private List<string> _roomPlayers = new();
    private string _status = "Connecting...";
    private long? _promptedVersion;
    private DateTimeOffset _lastSnapshotRequestAt = DateTimeOffset.MinValue;
    private bool _snapshotRequestedAfterRoomUpdate;
    private RoundOverState? _pendingRoundSummary;
    private readonly List<RoundOverState> _completedRounds = new();

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

                if (ShouldRefreshSnapshot())
                {
                    await RequestSnapshotAsync(cancellationToken);
                }

                if (_pendingRoundSummary is not null)
                {
                    var roundSummary = _pendingRoundSummary;
                    _pendingRoundSummary = null;
                    await _roundOverScreen.ShowAsync(
                        roundSummary,
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);
                    Render();
                    continue;
                }

                if (_screen.TryReadShortcut(out var shortcut) &&
                    shortcut is GameInputAction.ShowScoreHistory or GameInputAction.ShowLastRoundSummary)
                {
                    await HandleShortcutAsync(shortcut, listener, cancellationToken);
                    Render();
                    continue;
                }

                if (!IsGameInitialized())
                {
                    var command = await _screen.ReadCommandAsync(
                        BuildGameState(),
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);

                    if (command is GameInputAction.ShowScoreHistory or GameInputAction.ShowLastRoundSummary)
                    {
                        await HandleShortcutAsync(command, listener, cancellationToken);
                        Render();
                        continue;
                    }

                    var submittedCommand = (command as GameInputAction.Submit)?.Value.Trim() ?? string.Empty;

                    if (string.Equals(submittedCommand, "/back", StringComparison.OrdinalIgnoreCase))
                    {
                        return RemoteGameLoopResult.BackToLobby;
                    }

                    if (string.Equals(submittedCommand, "/start", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsHostPlayer())
                        {
                            _status = "Only the host can start the game.";
                            Render();
                            continue;
                        }

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

                    if (!string.IsNullOrWhiteSpace(submittedCommand))
                    {
                        _status = "Available commands: /start, /back";
                        Render();
                    }

                    continue;
                }

                if (ShouldPromptForAction())
                {
                    var currentState = _state!;
                    var actionResult = await _screen.ReadInputAsync(
                        BuildGameState(),
                        () => Task.FromResult(DrainMessages(listener)),
                        cancellationToken);
                    if (actionResult is GameInputAction.ShowScoreHistory or GameInputAction.ShowLastRoundSummary)
                    {
                        await HandleShortcutAsync(actionResult, listener, cancellationToken);
                        Render();
                        continue;
                    }

                    if (actionResult is not GameInputAction.SelectedAction selectedAction)
                    {
                        continue;
                    }

                    _promptedVersion = currentState.Version ?? 0;
                    await _client.SendActionAsync(_options.PlayerId, selectedAction.Action, cancellationToken);
                    _status = $"Sent: {selectedAction.Action.Type ?? string.Empty} {selectedAction.Action.DisplayAction}".TrimEnd();
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
        SyncGameState();
    }

    private void HandleCommandApplied(RemoteGameInboundMessageDto? message)
    {
        var previousState = _state;
        if (message?.State is not null)
        {
            _state = message.State;
            TryBuildRoundSummary(previousState, _state);
        }

        var appliedBy = message?.Command?.PlayerId ?? string.Empty;
        var commandType = message?.Command?.Type ?? string.Empty;
        _status = $"Applied: {commandType} by {appliedBy}";
        SyncGameState();
    }

    private void HandleSnapshot(RemoteGameInboundMessageDto? message)
    {
        var previousState = _state;
        if (message?.State is not null)
        {
            _state = message.State;
            TryBuildRoundSummary(previousState, _state);
            _status = (_state.Data?.Players?.Count ?? 0) > 0
                ? $"Snapshot loaded. Current player: {_state.Data?.CurrentPlayerId ?? string.Empty}"
                : "Snapshot loaded. Game not initialized yet.";
            SyncGameState();
            return;
        }

        _status = "Snapshot received without state.";
        SyncGameState();
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

        if (!IsGameInitialized())
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
        SyncGameState();
        return _gameState;
    }

    private void SyncGameState()
    {
        _gameState.Snapshot = _state;
        _gameState.Status = _status;
        _gameState.RoomPlayers = _roomPlayers;
    }

    private void TryBuildRoundSummary(RemoteGameSnapshotDto? previousState, RemoteGameSnapshotDto? currentState)
    {
        var previousRound = previousState?.Data?.RoundNumber;
        var currentRound = currentState?.Data?.RoundNumber;
        if (!previousRound.HasValue || !currentRound.HasValue || currentRound.Value <= previousRound.Value)
        {
            return;
        }

        var players = BuildRoundSummaryPlayers(currentState);

        var lastSequenceLines = Array.Empty<string>();
        if (previousState?.Data?.AppliedMoves is { Count: > 0 } appliedMoves)
        {
            lastSequenceLines = appliedMoves
                .Select(move => $"{move.PlayerId ?? string.Empty}: {move.Action ?? string.Empty}")
                .ToArray();
        }

        var roundSummary = new RoundOverState
        {
            GameId = _options.GameId,
            RoundNumber = previousRound.Value,
            NextRoundNumber = currentRound.Value,
            Status = $"Round {previousRound.Value} finished. Round {currentRound.Value} started.",
            WinnerPlayerId = currentState?.Data?.PreviousRound?.WinnerPlayerName ?? string.Empty,
            Players = players
                .OrderByDescending(player => player.TotalPoints)
                .ThenBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            HaggisCards = currentState?.Data?.PreviousRound?.HaggisCards?
                .Where(card => !string.IsNullOrWhiteSpace(card))
                .Select(card => card ?? string.Empty)
                .ToArray() ?? Array.Empty<string>(),
            LastSequenceLines = lastSequenceLines
        };
        _completedRounds.Add(roundSummary);
        _pendingRoundSummary = roundSummary;
    }

    private List<RoundOverPlayerSummary> BuildRoundSummaryPlayers(RemoteGameSnapshotDto? currentState)
    {
        var currentPlayers = currentState?.Data?.Players ?? new List<RemotePlayerStateDto>();
        var previousRoundScores = currentState?.Data?.PreviousRound?.PlayerScores ?? new List<RemotePreviousRoundPlayerScoreDto>();

        var players = new List<RoundOverPlayerSummary>();
        foreach (var score in previousRoundScores)
        {
            var playerId = score.PlayerName ?? string.Empty;
            var totalPoints = currentPlayers.FirstOrDefault(player =>
                string.Equals(player.Id, playerId, StringComparison.OrdinalIgnoreCase))?.Score ?? 0;

            players.Add(new RoundOverPlayerSummary
            {
                PlayerId = playerId,
                TricksPoints = score.TricksPoints ?? 0,
                OpponentsRemainingCardsPoints = score.OpponentsRemainingCardsPoints ?? 0,
                HaggisPoints = score.HaggisPoints ?? 0,
                RoundPoints = score.RoundPoints ?? 0,
                TotalPoints = totalPoints
            });
        }

        if (players.Count > 0)
        {
            return players;
        }

        return currentPlayers
            .Select(player => new RoundOverPlayerSummary
            {
                PlayerId = player.Id ?? string.Empty,
                TotalPoints = player.Score ?? 0
            })
            .ToList();
    }

    private async Task ShowScoreHistoryAsync(JsonEventListener listener, CancellationToken cancellationToken)
    {
        await _scoreHistoryScreen.ShowAsync(
            BuildScoreHistoryState(),
            () => Task.FromResult(DrainMessages(listener)),
            cancellationToken);
    }

    private async Task ShowLastRoundSummaryAsync(JsonEventListener listener, CancellationToken cancellationToken)
    {
        var lastRoundSummary = _completedRounds.LastOrDefault();
        if (lastRoundSummary is null)
        {
            _status = "No finished round yet.";
            return;
        }

        await _roundOverScreen.ShowAsync(
            lastRoundSummary,
            () => Task.FromResult(DrainMessages(listener)),
            cancellationToken);
    }

    private async Task HandleShortcutAsync(
        GameInputAction shortcut,
        JsonEventListener listener,
        CancellationToken cancellationToken)
    {
        switch (shortcut)
        {
            case GameInputAction.ShowScoreHistory:
                await ShowScoreHistoryAsync(listener, cancellationToken);
                return;
            case GameInputAction.ShowLastRoundSummary:
                await ShowLastRoundSummaryAsync(listener, cancellationToken);
                return;
        }
    }

    private ScoreHistoryState BuildScoreHistoryState()
    {
        var roundNumbers = _completedRounds
            .Select(round => round.RoundNumber)
            .Distinct()
            .OrderBy(round => round)
            .ToArray();

        var currentPlayers = _state?.Data?.Players ?? new List<RemotePlayerStateDto>();
        var playerIds = currentPlayers
            .Select(player => player.Id ?? string.Empty)
            .Where(playerId => !string.IsNullOrWhiteSpace(playerId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(playerId => playerId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rows = new List<ScoreHistoryPlayerRow>();
        foreach (var playerId in playerIds)
        {
            var roundPoints = new List<int>();
            foreach (var roundNumber in roundNumbers)
            {
                var round = _completedRounds.FirstOrDefault(summary => summary.RoundNumber == roundNumber);
                var player = round?.Players.FirstOrDefault(item =>
                    string.Equals(item.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
                roundPoints.Add(player?.RoundPoints ?? 0);
            }

            var totalPoints = currentPlayers.FirstOrDefault(player =>
                string.Equals(player.Id, playerId, StringComparison.OrdinalIgnoreCase))?.Score ?? 0;

            rows.Add(new ScoreHistoryPlayerRow
            {
                PlayerId = playerId,
                RoundPoints = roundPoints,
                TotalPoints = totalPoints
            });
        }

        return new ScoreHistoryState
        {
            GameId = _options.GameId,
            RoundNumbers = roundNumbers,
            Players = rows
        };
    }

    private bool IsHostPlayer()
    {
        return _roomPlayers.Count > 0 &&
               string.Equals(_roomPlayers[0], _options.PlayerId, StringComparison.OrdinalIgnoreCase);
    }
}
