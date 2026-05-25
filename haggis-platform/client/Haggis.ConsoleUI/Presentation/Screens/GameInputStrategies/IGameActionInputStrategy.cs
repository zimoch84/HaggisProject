namespace Haggis.ConsoleUI.Presentation.Screens.GameInputStrategies;

internal interface IGameActionInputStrategy
{
    string InputPrompt { get; }

    string? InitializeCategory(IReadOnlyList<RemotePossibleActionDto> actions);

    IReadOnlyList<string> BuildActionLines(
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory);

    GameActionSelectionResult HandleCommand(
        string command,
        IReadOnlyList<RemotePossibleActionDto> actions,
        string? selectedCategory);
}
