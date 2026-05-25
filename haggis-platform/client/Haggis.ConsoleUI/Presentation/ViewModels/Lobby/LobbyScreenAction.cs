using Haggis.ConsoleUI.Presentation.Panels;

namespace Haggis.ConsoleUI.Presentation.ViewModels.Lobby;

public abstract record LobbyScreenAction : IInputAction
{
    public sealed record Refresh : LobbyScreenAction;

    public sealed record Quit : LobbyScreenAction;

    public sealed record SendChat(string Text) : LobbyScreenAction;

    public sealed record CreateRoom(string? RoomName) : LobbyScreenAction;

    public sealed record JoinRoom(string Token) : LobbyScreenAction;

    public sealed record Unknown(string Command) : LobbyScreenAction;
}
