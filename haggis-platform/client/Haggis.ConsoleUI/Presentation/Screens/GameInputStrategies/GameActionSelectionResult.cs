namespace Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;

internal sealed record GameActionSelectionResult(
    RemotePossibleActionDto? SelectedAction,
    string? SelectedCategory,
    string? StatusMessage,
    bool RequiresRender = false);
