using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Serwer.API.Services;

public sealed class PlayerSocketRegistry : IPlayerSocketRegistry
{
    private readonly ConcurrentDictionary<Guid, SocketConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, string> _connectionPlayers = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _playerConnections = new(StringComparer.OrdinalIgnoreCase);

    public Guid Register(WebSocket socket, string source)
    {
        var connectionId = Guid.NewGuid();
        _connections[connectionId] = new SocketConnection(socket, source);
        return connectionId;
    }

    public void BindPlayer(Guid connectionId, string playerId)
    {
        var normalizedPlayerId = playerId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPlayerId) || !_connections.ContainsKey(connectionId))
        {
            return;
        }

        if (_connectionPlayers.TryGetValue(connectionId, out var existingPlayerId) &&
            !existingPlayerId.Equals(normalizedPlayerId, StringComparison.OrdinalIgnoreCase))
        {
            RemoveConnectionFromPlayer(existingPlayerId, connectionId);
        }

        _connectionPlayers[connectionId] = normalizedPlayerId;
        var playerConnections = _playerConnections.GetOrAdd(normalizedPlayerId, static _ => new ConcurrentDictionary<Guid, byte>());
        playerConnections[connectionId] = 0;
    }

    public void Unregister(Guid connectionId)
    {
        _connections.TryRemove(connectionId, out _);

        if (_connectionPlayers.TryRemove(connectionId, out var playerId))
        {
            RemoveConnectionFromPlayer(playerId, connectionId);
        }
    }

    public IReadOnlyCollection<WebSocket> GetPlayerSockets(string playerId)
    {
        if (!_playerConnections.TryGetValue(playerId, out var playerConnections))
        {
            return Array.Empty<WebSocket>();
        }

        var sockets = new List<WebSocket>(playerConnections.Count);
        foreach (var pair in playerConnections)
        {
            if (_connections.TryGetValue(pair.Key, out var connection))
            {
                sockets.Add(connection.Socket);
            }
        }

        return sockets;
    }

    public IReadOnlyDictionary<string, int> GetOnlinePlayerConnectionCounts()
    {
        var snapshot = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _playerConnections)
        {
            snapshot[pair.Key] = pair.Value.Count;
        }

        return snapshot;
    }

    private void RemoveConnectionFromPlayer(string playerId, Guid connectionId)
    {
        if (!_playerConnections.TryGetValue(playerId, out var playerConnections))
        {
            return;
        }

        playerConnections.TryRemove(connectionId, out _);
        if (playerConnections.IsEmpty)
        {
            _playerConnections.TryRemove(playerId, out _);
        }
    }

    private sealed record SocketConnection(WebSocket Socket, string Source);
}
