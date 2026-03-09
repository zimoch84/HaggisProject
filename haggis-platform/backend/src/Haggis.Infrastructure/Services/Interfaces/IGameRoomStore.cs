using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGameRoomStore
{
    IReadOnlyList<GameRoom> ListRooms();
    GameRoom GetOrCreateRoom(string roomId, string hostPlayerId, string gameType, string? roomName = null);
    bool TryGetRoom(string roomId, out GameRoom? room);
    bool TryJoinRoom(string roomId, string playerId, out GameRoom? room);
}
