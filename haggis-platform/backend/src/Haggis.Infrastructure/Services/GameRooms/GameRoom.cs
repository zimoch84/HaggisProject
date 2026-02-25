namespace Haggis.Infrastructure.Services.GameRooms;

public sealed class GameRoom
{
    public string RoomId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public string GameType { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public List<string> Players { get; init; } = new();
}



