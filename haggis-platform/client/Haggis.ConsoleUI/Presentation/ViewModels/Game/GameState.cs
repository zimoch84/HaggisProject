namespace Haggis.ConsoleUI.Presentation.ViewModels.Game;

public sealed class GameState
{
    public RemoteGameSnapshotDto? Snapshot { get; set; }

    public string PlayerId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string Status { get; set; } = "Connecting...";

    public IReadOnlyList<string> RoomPlayers { get; set; } = Array.Empty<string>();
}
