using System.Net.WebSockets;
using Haggis.Infrastructure.Services.WebSocketHandlers;

namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGameConnectionManager
{
    GameConnectionRegistration Register(string gameId, WebSocket socket);
    void BindPlayer(string gameId, Guid clientId, string playerId);
    void Unregister(string gameId, Guid clientId);
    IReadOnlyCollection<WebSocket> GetSockets(string gameId);
    IReadOnlyCollection<string> GetConnectedPlayers(string gameId);
}
