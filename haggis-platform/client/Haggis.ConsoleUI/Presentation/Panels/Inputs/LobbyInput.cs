using Haggis.ConsoleUI.Presentation.ViewModels.Lobby;

namespace Haggis.ConsoleUI.Presentation.Panels.Inputs;

public sealed class LobbyInput : PanelRegionInputBase
{
    public LobbyInput(string header, int x, int y, int width, int height)
        : base(header, x, y, width, height)
    {
    }

    public override IInputAction ParseCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return new LobbyScreenAction.Refresh();
        }

        if (command.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            return new LobbyScreenAction.Quit();
        }

        if (command.Equals("/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return new LobbyScreenAction.Refresh();
        }

        if (command.StartsWith("/chat ", StringComparison.OrdinalIgnoreCase))
        {
            return new LobbyScreenAction.SendChat(command[6..].Trim());
        }

        if (command.StartsWith("/create", StringComparison.OrdinalIgnoreCase))
        {
            var roomName = command.Length > 7 ? command[7..].Trim() : string.Empty;
            return new LobbyScreenAction.CreateRoom(roomName);
        }

        if (command.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
        {
            return new LobbyScreenAction.JoinRoom(command[6..].Trim());
        }

        return new LobbyScreenAction.Unknown(command);
    }
}
