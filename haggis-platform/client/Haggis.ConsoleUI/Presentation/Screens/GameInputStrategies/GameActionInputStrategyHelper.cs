using Haggis.Domain.Enums;

namespace Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;

internal static class GameActionInputStrategyHelper
{
    public static IReadOnlyList<string> GetSelectableActionCategories(IReadOnlyList<RemotePossibleActionDto> actions)
    {
        return actions
            .Select(GetActionCategory)
            .Where(category => !string.Equals(category, "PASS", StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(category, "BOMB", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetCategoryOrder)
            .ThenBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<RemotePossibleActionDto> GetDisplayedActions(
        IReadOnlyList<RemotePossibleActionDto> actions,
        string selectedCategory)
    {
        return actions
            .Where(action =>
            {
                var category = GetActionCategory(action);
                return string.Equals(category, selectedCategory, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(category, "PASS", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(category, "BOMB", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }

    public static int CountActionsForCategory(
        IReadOnlyList<RemotePossibleActionDto> actions,
        string category)
    {
        return actions.Count(action =>
            string.Equals(GetActionCategory(action), category, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetActionCategory(RemotePossibleActionDto action)
    {
        if (string.Equals(action.Type, "Pass", StringComparison.OrdinalIgnoreCase))
        {
            return "PASS";
        }

        var text = action.DisplayAction;
        if (string.IsNullOrWhiteSpace(text))
        {
            return action.Type?.ToUpperInvariant() ?? "UNKNOWN";
        }

        var bracketIndex = text.IndexOf('[', StringComparison.Ordinal);
        var category = bracketIndex > 0
            ? text[..bracketIndex]
            : text;

        return category.Trim().ToUpperInvariant();
    }

    private static int GetCategoryOrder(string category)
    {
        return Enum.TryParse<TrickType>(category, ignoreCase: true, out var trickType)
            ? (int)trickType
            : int.MaxValue;
    }
}
