public sealed class GameScreen : PanelScreenBase
{
    private readonly UITextPanel _summaryPanel;
    private readonly UITextPanel _playersPanel;
    private readonly UITextPanel _tablePanel;
    private readonly UITextPanel _actionsPanel;

    private GameState _state = new();
    private string _inputPrompt = "> ";

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

    public async Task<RemotePossibleActionDto> ReadInputAsync(
        GameState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        SetModel(state, InputPanel.CurrentText);
        InputPanel.Clear();
        _inputPrompt = "Action index: ";

        while (!cancellationToken.IsCancellationRequested)
        {
            var action = await ReadCommandCoreAsync(
                pumpMessagesAsync,
                cancellationToken,
                new GameInputAction.Submit("0"));
            var command = action.Value;
            var actions = state.Snapshot?.Data?.PossibleActions;
            if (actions is not null &&
                int.TryParse(command, out var index) &&
                index >= 0 &&
                index < actions.Count)
            {
                return actions[index];
            }

            _state.Status = $"Invalid action index: {command}";
            InputPanel.Clear();
        }

        return state.Snapshot?.Data?.PossibleActions?.FirstOrDefault() ?? new RemotePossibleActionDto();
    }

    public async Task<string> ReadCommandAsync(
        GameState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        SetModel(state, InputPanel.CurrentText);
        InputPanel.Clear();
        _inputPrompt = "> ";
        var action = await ReadCommandCoreAsync(
            pumpMessagesAsync,
            cancellationToken,
            new GameInputAction.Submit("/back"));
        return action.Value.Trim();
    }

    protected override void PreparePanels()
    {
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
        if (_state.Snapshot?.Data?.PossibleActions is null || _state.Snapshot.Data.PossibleActions.Count == 0)
        {
            return new[] { "(waiting)" };
        }

        return _state.Snapshot.Data.PossibleActions
            .Select((action, index) => $"[{index}] {action.Type ?? string.Empty} {action.Action ?? string.Empty}".TrimEnd())
            .ToList();
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
