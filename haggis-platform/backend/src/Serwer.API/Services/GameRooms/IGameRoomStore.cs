namespace Serwer.API.Services.GameRooms;

public interface IGameRoomStore
{
    GameRoom CreateRoom(string hostPlayerId, string gameType, string? roomName = null);
    IReadOnlyList<GameRoom> ListRooms();
    bool TryGetRoom(string roomId, out GameRoom? room);
    bool TryJoinRoom(string roomId, string playerId, out GameRoom? room);
}

