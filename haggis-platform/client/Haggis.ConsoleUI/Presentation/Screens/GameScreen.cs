using Haggis.Domain.Enums;
using Haggis.ConsoleUI.Presentation.Panels;
using Haggis.ConsoleUI.Presentation.Panels.InputActions;
using Haggis.ConsoleUI.Presentation.Panels.Inputs;
using Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;
using Haggis.ConsoleUI.Presentation.ViewModels.Game;

namespace Haggis.ConsoleUI.Presentation.Screens;

public sealed class GameScreen : PanelScreenBase
{
    private readonly UITextPanel _summaryPanel;
    private readonly UITextPanel _playersPanel;
    private readonly UITextPanel _tablePanel;
    private readonly UITextPanel _actionsPanel;
    private readonly IGameActionInputStrategy _startingTrickStrategy = new StartingTrickInputStrategy();
    private readonly IGameActionInputStrategy _continuationStrategy = new ContinuationInputStrategy();

    private GameState _state = new();
    private string _inputPrompt = "> ";
    private string? _selectedActionCategory;

    public GameScreen() : base(CreateInputPanel())
    {
        var width = SafeConsoleWidth();
        var height = SafeConsoleHeight();
        var topHeight = Math.Max(8, (height - 5) / 3);
        var bodyHeight = Math.Max(8, height - topHeight - 5);
        var leftWidth = width / 2;
        var rightWidth = width - leftWidth + 1;
        var bodyY = 1 + topHeight;

        _summaryPanel = new UITextPanel("Summary", 0, 1, leftWidth, topHeight);
        _playersPanel = new UITextPanel("Players", leftWidth - 1, 1, rightWidth, topHeight);
        _tablePanel = new UITextPanel("Table", 0, bodyY, leftWidth, bodyHeight);
        _actionsPanel = new UITextPanel("Possible Actions", leftWidth - 1, bodyY, rightWidth, bodyHeight);

        AddPanel(_summaryPanel);
        AddPanel(_playersPanel);
        AddPanel(_tablePanel);
        AddPanel(_actionsPanel);
    }

    public void Render(
        GameState state,
        string inputText = "")
    {
        SetModel(state, string.IsNullOrEmpty(inputText) ? InputPanel.CurrentText : inputText);
        base.Render();
    }

    public async Task<GameInputAction> ReadInputAsync(
        GameState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        SetModel(state, InputPanel.CurrentText);
        InputPanel.Clear();
        var actions = state.Snapshot?.Data?.PossibleActions;
        var strategy = ResolveInputStrategy(state.Snapshot?.Data);
        _selectedActionCategory = actions is null ? null : strategy.InitializeCategory(actions);
        UpdateInputPrompt();

        while (!cancellationToken.IsCancellationRequested)
        {
            UpdateInputPrompt();
            var action = await ReadCommandCoreAsync<GameInputAction>(
                pumpMessagesAsync,
                cancellationToken,
                new GameInputAction.Submit("0"));
            if (action is GameInputAction.ShowScoreHistory or GameInputAction.ShowLastRoundSummary)
            {
                return action;
            }

            var command = (action as GameInputAction.Submit)?.Value.Trim() ?? string.Empty;
            if (actions is null || actions.Count == 0)
            {
                _state.Status = "No actions available.";
                InputPanel.Clear();
                continue;
            }

            var selection = strategy.HandleCommand(command, actions, _selectedActionCategory);
            _selectedActionCategory = selection.SelectedCategory;

            if (selection.SelectedAction is not null)
            {
                return new GameInputAction.SelectedAction(selection.SelectedAction);
            }

            _state.Status = selection.StatusMessage ?? "Invalid input.";
            InputPanel.Clear();
            if (selection.RequiresRender)
            {
                Render();
            }
        }

        var fallbackAction = state.Snapshot?.Data?.PossibleActions?.FirstOrDefault();
        return fallbackAction is null
            ? new GameInputAction.Submit(string.Empty)
            : new GameInputAction.SelectedAction(fallbackAction);
    }

    public async Task<GameInputAction> ReadCommandAsync(
        GameState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        SetModel(state, InputPanel.CurrentText);
        InputPanel.Clear();
        _inputPrompt = "> ";
        return await ReadCommandCoreAsync<GameInputAction>(
            pumpMessagesAsync,
            cancellationToken,
            new GameInputAction.Submit("/back"));
    }

    public bool TryReadShortcut(out GameInputAction? action)
    {
        action = null;
        if (!InputPanel.TryReadKey(out var key))
        {
            return false;
        }

        HandleKey(key);
        if (!TryConsumeCommand<GameInputAction>(out var parsedAction))
        {
            RenderInputPanel();
            return false;
        }

        action = parsedAction;
        return true;
    }

    protected override void PreparePanels()
    {
        UpdateInputPrompt();
        _summaryPanel.SetLines(BuildSummaryLines());
        _playersPanel.SetLines(BuildPlayerLines());
        _tablePanel.SetLines(BuildTableLines());
        _actionsPanel.SetLines(BuildActionLines());
        InputPanel.SetPrompt(_inputPrompt);
    }

    private void SetModel(
        GameState state,
        string inputText)
    {
        _state = state;

        if (InputPanel.CurrentText != inputText)
        {
            InputPanel.SetText(inputText);
        }
    }

    private IReadOnlyList<string> BuildSummaryLines()
    {
        var lines = new List<string>
        {
            $"Game: {_state.GameId}",
            $"Player: {_state.PlayerId}",
            $"Status: {_state.Status}",
            $"Room players: {(_state.RoomPlayers.Count == 0 ? "(none)" : string.Join(", ", _state.RoomPlayers))}"
        };

        if (_state.Snapshot is null)
        {
            lines.Add("No game state yet.");
            lines.Add("Use /start to initialize the game or /back to return to lobby.");
            return lines;
        }

        lines.Add($"State version: {_state.Snapshot.Version ?? 0}");
        lines.Add($"Current player: {_state.Snapshot.Data?.CurrentPlayerId ?? string.Empty}");
        lines.Add($"Round over: {_state.Snapshot.Data?.RoundOver ?? false}");
        lines.Add($"Possible actions: {_state.Snapshot.Data?.PossibleActions?.Count ?? 0}");

        if (_state.Snapshot.Data?.AppliedMove is not null)
        {
            lines.Add($"Last move: {_state.Snapshot.Data.AppliedMove.PlayerId ?? string.Empty} -> {_state.Snapshot.Data.AppliedMove.Action ?? string.Empty}");
        }

        return lines;
    }

    private IReadOnlyList<string> BuildPlayerLines()
    {
        if (_state.Snapshot?.Data?.Players is null)
        {
            if (_state.RoomPlayers.Count > 0)
            {
                return _state.RoomPlayers
                    .Select(player => $"{player} (room)")
                    .ToList();
            }

            return new[] { "(waiting for snapshot)" };
        }

        var lines = new List<string>();
        foreach (var player in _state.Snapshot.Data.Players)
        {
            var marker = string.Equals(player.Id, _state.Snapshot.Data.CurrentPlayerId, StringComparison.OrdinalIgnoreCase) ? "*" : " ";
            var finished = player.Finished == true ? " finished" : string.Empty;
            lines.Add($"{marker} {player.Id ?? string.Empty}: score={player.Score ?? 0}, hand={player.HandCount ?? 0}{finished}");
            lines.Add($"cards: {FormatCards(player.Hand)}");
            lines.Add(string.Empty);
        }

        return lines;
    }

    private IReadOnlyList<string> BuildTableLines()
    {
        if (_state.Snapshot?.Data?.Trick is null || _state.Snapshot.Data.Trick.Count == 0)
        {
            return new[] { "(empty)" };
        }

        return _state.Snapshot.Data.Trick
            .Select(action => $"{action.PlayerId ?? string.Empty}: {action.Description ?? string.Empty}")
            .ToList();
    }

    private IReadOnlyList<string> BuildActionLines()
    {
        if (!IsGameInitialized())
        {
            var lines = new List<string>
            {
                "Game is not initialized.",
                "/back - return to lobby"
            };

            if (IsHostPlayer())
            {
                lines.Insert(1, "/start - start the game");
            }
            else
            {
                lines.Insert(1, "Waiting for host to start.");
            }

            if (_state.RoomPlayers.Count < 2)
            {
                lines.Add(string.Empty);
                lines.Add("Need at least 2 players.");
                lines.Add($"Players in room: {_state.RoomPlayers.Count}");
            }

            return lines;
        }

        if (!IsCurrentPlayersTurn())
        {
            return new[]
            {
                "Waiting for active player.",
                $"Current player: {_state.Snapshot?.Data?.CurrentPlayerId ?? string.Empty}"
            };
        }

        if (_state.Snapshot?.Data?.PossibleActions is null || _state.Snapshot.Data.PossibleActions.Count == 0)
        {
            return new[] { "(waiting)" };
        }

        var actions = _state.Snapshot.Data.PossibleActions;
        var strategy = ResolveInputStrategy(_state.Snapshot?.Data);
        return strategy.BuildActionLines(actions, _selectedActionCategory);
    }

    private static bool ShouldChooseCategoryStep(RemoteGameStateDataDto? data)
    {
        return (data?.Trick?.Count ?? 0) == 0;
    }

    private void UpdateInputPrompt()
    {
        if (!IsGameInitialized() || !IsCurrentPlayersTurn() || (_state.Snapshot?.Data?.PossibleActions?.Count ?? 0) == 0)
        {
            _inputPrompt = "> ";
            return;
        }

        _inputPrompt = ResolveInputStrategy(_state.Snapshot?.Data).InputPrompt;
    }

    private IGameActionInputStrategy ResolveInputStrategy(RemoteGameStateDataDto? data)
    {
        return ShouldChooseCategoryStep(data)
            ? _startingTrickStrategy
            : _continuationStrategy;
    }

    private bool IsGameInitialized()
    {
        if (_state.Snapshot is null)
        {
            return false;
        }

        if ((_state.Snapshot.Version ?? 0) > 0 && !string.IsNullOrWhiteSpace(_state.Snapshot.Data?.CurrentPlayerId))
        {
            return true;
        }

        var lastCommandType = _state.Snapshot.Data?.LastCommand?.Type;
        return string.Equals(lastCommandType, "Initialize", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCurrentPlayersTurn()
    {
        return string.Equals(
            _state.Snapshot?.Data?.CurrentPlayerId,
            _state.PlayerId,
            StringComparison.OrdinalIgnoreCase);
    }

    private bool IsHostPlayer()
    {
        return _state.RoomPlayers.Count > 0 &&
               string.Equals(_state.RoomPlayers[0], _state.PlayerId, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatCards(IReadOnlyList<string?>? cards)
    {
        if (cards is null || cards.Count == 0)
        {
            return "(none)";
        }

        var text = string.Join(" ", cards.Where(card => !string.IsNullOrWhiteSpace(card)).Select(card => card ?? string.Empty));
        return string.IsNullOrWhiteSpace(text) ? "(none)" : text;
    }

    private static GameInput CreateInputPanel()
    {
        return new GameInput("Input", 0, SafeConsoleHeight() - 5, SafeConsoleWidth(), 5);
    }

    private static int SafeConsoleWidth()
    {
        try
        {
            return Math.Max(40, Console.WindowWidth - 1);
        }
        catch
        {
            return 119;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Math.Max(12, Console.WindowHeight - 1);
        }
        catch
        {
            return 29;
        }
    }
}
