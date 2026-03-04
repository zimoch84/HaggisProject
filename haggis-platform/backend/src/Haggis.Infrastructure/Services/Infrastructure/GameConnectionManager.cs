using System.Collections.Concurrent;
using System.Net.WebSockets;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.WebSocketHandlers;

namespace Haggis.Infrastructure.Services.Infrastructure;

public sealed class GameConnectionManager : IGameConnectionManager
{
    private readonly IPlayerSocketRegistry _playerSocketRegistry;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, GameClientConnection>> _gameClients = new(StringComparer.OrdinalIgnoreCase);

    public GameConnectionManager(IPlayerSocketRegistry playerSocketRegistry)
    {
        _playerSocketRegistry = playerSocketRegistry;
    }

    public GameConnectionRegistration Register(string gameId, WebSocket socket)
    {
        var normalizedGameId = gameId.Trim();
        var clients = _gameClients.GetOrAdd(normalizedGameId, static _ => new ConcurrentDictionary<Guid, GameClientConnection>());
        var clientId = Guid.NewGuid();
        var connectionId = _playerSocketRegistry.Register(socket, $"games:{normalizedGameId}");

        clients[clientId] = new GameClientConnection(clientId, connectionId, socket);
        return new GameConnectionRegistration(clientId, connectionId);
    }

    public void BindPlayer(string gameId, Guid clientId, string playerId)
    {
        var normalizedGameId = gameId.Trim();
        var normalizedPlayerId = playerId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPlayerId) ||
            !_gameClients.TryGetValue(normalizedGameId, out var clients) ||
            !clients.TryGetValue(clientId, out var currentConnection))
        {
            return;
        }

        currentConnection.PlayerId = normalizedPlayerId;
        _playerSocketRegistry.BindPlayer(currentConnection.ConnectionId, normalizedPlayerId);

        var staleConnections = clients.Values
            .Where(x => x.ClientId != clientId && string.Equals(x.PlayerId, normalizedPlayerId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var staleConnection in staleConnections)
        {
            clients.TryRemove(staleConnection.ClientId, out _);
            _playerSocketRegistry.Unregister(staleConnection.ConnectionId);
            TryAbort(staleConnection.Socket);
        }

        if (clients.IsEmpty)
        {
            _gameClients.TryRemove(normalizedGameId, out _);
        }
    }

    public void Unregister(string gameId, Guid clientId)
    {
        var normalizedGameId = gameId.Trim();
        if (!_gameClients.TryGetValue(normalizedGameId, out var clients) ||
            !clients.TryRemove(clientId, out var connection))
        {
            return;
        }

        _playerSocketRegistry.Unregister(connection.ConnectionId);
        if (clients.IsEmpty)
        {
            _gameClients.TryRemove(normalizedGameId, out _);
        }
    }

    public IReadOnlyCollection<WebSocket> GetSockets(string gameId)
    {
        var normalizedGameId = gameId.Trim();
        if (!_gameClients.TryGetValue(normalizedGameId, out var clients))
        {
            return Array.Empty<WebSocket>();
        }

        return clients.Values.Select(x => x.Socket).ToArray();
    }

    public IReadOnlyCollection<string> GetConnectedPlayers(string gameId)
    {
        var normalizedGameId = gameId.Trim();
        if (!_gameClients.TryGetValue(normalizedGameId, out var clients))
        {
            return Array.Empty<string>();
        }

        return clients.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.PlayerId))
            .Select(x => x.PlayerId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void TryAbort(WebSocket socket)
    {
        try
        {
            socket.Abort();
            socket.Dispose();
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (WebSocketException)
        {
        }
    }

    private sealed class GameClientConnection
    {
        public GameClientConnection(Guid clientId, Guid connectionId, WebSocket socket)
        {
            ClientId = clientId;
            ConnectionId = connectionId;
            Socket = socket;
        }

        public Guid ClientId { get; }
        public Guid ConnectionId { get; }
        public WebSocket Socket { get; }
        public string? PlayerId { get; set; }
    }
}
