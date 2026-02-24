using System.Net.WebSockets;

namespace Serwer.API.Services;

public interface IPlayerSocketRegistry
{
    Guid Register(WebSocket socket, string source);
    void BindPlayer(Guid connectionId, string playerId);
    void Unregister(Guid connectionId);
    IReadOnlyCollection<WebSocket> GetPlayerSockets(string playerId);
    IReadOnlyDictionary<string, int> GetOnlinePlayerConnectionCounts();
}
