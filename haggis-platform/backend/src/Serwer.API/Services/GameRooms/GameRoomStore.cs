using System.Collections.Concurrent;

namespace Serwer.API.Services.GameRooms;

public sealed class GameRoomStore : IGameRoomStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom(string hostPlayerId, string gameType, string? roomName = null)
    {
        var normalizedRoomName = string.IsNullOrWhiteSpace(roomName)
            ? BuildDefaultRoomName(hostPlayerId, gameType)
            : roomName.Trim();

        var room = new GameRoom
        {
            RoomId = Guid.NewGuid().ToString("N"),
            GameId = Guid.NewGuid().ToString("N"),
            GameType = gameType,
            RoomName = normalizedRoomName,
            CreatedAt = DateTimeOffset.UtcNow,
            Players = new List<string> { hostPlayerId }
        };

        _rooms[room.RoomId] = room;
        return Clone(room);
    }

    public IReadOnlyList<GameRoom> ListRooms()
    {
        return _rooms.Values
            .OrderByDescending(x => x.CreatedAt)
            .Select(Clone)
            .ToList();
    }

    public bool TryGetRoom(string roomId, out GameRoom? room)
    {
        room = null;
        if (!_rooms.TryGetValue(roomId, out var existing))
        {
            return false;
        }

        room = Clone(existing);
        return true;
    }

    public bool TryJoinRoom(string roomId, string playerId, out GameRoom? room)
    {
        room = null;
        if (!_rooms.TryGetValue(roomId, out var current))
        {
            return false;
        }

        lock (current)
        {
            if (!current.Players.Contains(playerId, StringComparer.OrdinalIgnoreCase))
            {
                current.Players.Add(playerId);
            }

            room = Clone(current);
            return true;
        }
    }

    private static GameRoom Clone(GameRoom room)
    {
        return new GameRoom
        {
            RoomId = room.RoomId,
            GameId = room.GameId,
            GameType = room.GameType,
            RoomName = room.RoomName,
            CreatedAt = room.CreatedAt,
            Players = room.Players.ToList()
        };
    }

    private static string BuildDefaultRoomName(string hostPlayerId, string gameType)
    {
        return $"{hostPlayerId} {ToDisplayGameName(gameType)} game";
    }

    private static string ToDisplayGameName(string gameType)
    {
        if (string.IsNullOrWhiteSpace(gameType))
        {
            return "Game";
        }

        var normalized = gameType.Trim().ToLowerInvariant();
        return char.ToUpperInvariant(normalized[0]) + normalized[1..];
    }
}

