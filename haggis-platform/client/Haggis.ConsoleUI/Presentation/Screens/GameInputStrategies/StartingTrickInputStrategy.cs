namespace Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;

internal sealed class StartingTrickInputStrategy : IGameActionInputStrategy
{
    public string InputPrompt => "Type index: ";

    public string? InitializeCategory(IReadOnlyList<RemotePossibleActionDto> actions) => null;

    public IReadOnlyList<string> BuildActionLines(
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory)
    {
        var lines = new List<string>
        {
            $"Actions: {actions.Count}"
        };

        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            var categories = GameActionInputStrategyHelper.GetSelectableActionCategories(actions);
            lines.Add("Choose trick type:");
            lines.AddRange(categories.Select((category, index) =>
                $"[{index}] {category} ({GameActionInputStrategyHelper.CountActionsForCategory(actions, category)})"));
            lines.Add(string.Empty);
            lines.Add("Then choose concrete action.");
            return lines;
        }

        lines.Add($"Type: {selectedCategory}");
        lines.Add("[t] back to types");
        lines.Add(string.Empty);

        var displayedActions = GameActionInputStrategyHelper.GetDisplayedActions(actions, selectedCategory);
        lines.AddRange(displayedActions.Select((action, index) =>
            $"[{index}] {action.Type ?? string.Empty} {action.DisplayAction}".TrimEnd()));
        return lines;
    }

    public GameActionSelectionResult HandleCommand(
        string command,
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory)
    {
        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            var categories = GameActionInputStrategyHelper.GetSelectableActionCategories(actions);
            if (int.TryParse(command, out var categoryIndex) &&
                categoryIndex >= 0 &&
                categoryIndex < categories.Count)
            {
                var nextCategory = categories[categoryIndex];
                return new GameActionSelectionResult(
                    SelectedAction: null,
                    SelectedCategory: nextCategory,
                    StatusMessage: $"Selected type: {nextCategory}",
                    RequiresRender: true);
            }

            return new GameActionSelectionResult(
                SelectedAction: null,
                SelectedCategory: null,
                StatusMessage: $"Invalid type index: {command}");
        }

        if (string.Equals(command, "t", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "/types", StringComparison.OrdinalIgnoreCase))
        {
            return new GameActionSelectionResult(
                SelectedAction: null,
                SelectedCategory: null,
                StatusMessage: "Select trick type.",
                RequiresRender: true);
        }

        var filteredActions = GameActionInputStrategyHelper.GetDisplayedActions(actions, selectedCategory);
        if (int.TryParse(command, out var actionIndex) &&
            actionIndex >= 0 &&
            actionIndex < filteredActions.Count)
        {
            return new GameActionSelectionResult(
                SelectedAction: filteredActions[actionIndex],
                SelectedCategory: selectedCategory,
                StatusMessage: null);
        }

        return new GameActionSelectionResult(
            SelectedAction: null,
            SelectedCategory: selectedCategory,
            StatusMessage: $"Invalid action index: {command}");
    }
}
