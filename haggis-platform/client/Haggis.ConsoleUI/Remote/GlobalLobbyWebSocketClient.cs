using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public sealed class GlobalLobbyWebSocketClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ClientWebSocket _socket = new();

    public async Task ConnectAsync(string baseUrl, CancellationToken cancellationToken)
    {
        await _socket.ConnectAsync(BuildUri(baseUrl), cancellationToken);
    }

    public Task<JsonDocument?> ReceiveAsync(CancellationToken cancellationToken) => ReceiveDocumentAsync(cancellationToken);

    public Task SendListRoomsAsync(CancellationToken cancellationToken) =>
        SendAsync(new { operation = "listroom" }, cancellationToken);

    public Task SendChatAsync(string playerId, string text, CancellationToken cancellationToken) =>
        SendAsync(new
        {
            operation = "chat",
            payload = new
            {
                playerId,
                text
            }
        }, cancellationToken);

    public Task SendCreateRoomAsync(string playerId, string roomName, string roomId, CancellationToken cancellationToken) =>
        SendAsync(new
        {
            operation = "createroom",
            payload = new
            {
                playerId,
                roomName,
                roomId,
                gameType = "haggis"
            }
        }, cancellationToken);

    private async Task SendAsync(object payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions));
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<JsonDocument?> ReceiveDocumentAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return JsonDocument.Parse(ms.ToArray());
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch
            {
            }
        }

        _socket.Dispose();
    }

    private static Uri BuildUri(string baseUrl)
    {
        var builder = new UriBuilder(baseUrl)
        {
            Path = "/ws/global/chat"
        };

        builder.Scheme = builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        return builder.Uri;
    }
}
