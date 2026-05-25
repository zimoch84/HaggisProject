namespace Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;

internal sealed class ContinuationInputStrategy : IGameActionInputStrategy
{
    public string InputPrompt => "Action index: ";

    public string? InitializeCategory(IReadOnlyList<RemotePossibleActionDto> actions) => null;

    public IReadOnlyList<string> BuildActionLines(
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory)
    {
        var lines = new List<string>
        {
            $"Actions: {actions.Count}"
        };

        lines.AddRange(actions.Select((action, index) =>
            $"[{index}] {action.Type ?? string.Empty} {action.DisplayAction}".TrimEnd()));
        return lines;
    }

    public GameActionSelectionResult HandleCommand(
        string command,
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory)
    {
        if (int.TryParse(command, out var actionIndex) &&
            actionIndex >= 0 &&
            actionIndex < actions.Count)
        {
            return new GameActionSelectionResult(
                SelectedAction: actions[actionIndex],
                SelectedCategory: null,
                StatusMessage: null);
        }

        return new GameActionSelectionResult(
            SelectedAction: null,
            SelectedCategory: null,
            StatusMessage: $"Invalid action index: {command}");
    }
}
