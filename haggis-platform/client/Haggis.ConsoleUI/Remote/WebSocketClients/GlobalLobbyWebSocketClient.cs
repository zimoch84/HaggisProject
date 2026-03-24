using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public sealed class GlobalLobbyWebSocketClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();

    public string ServerBaseUrl { get; private set; } = string.Empty;

    public async Task ConnectAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ServerBaseUrl = baseUrl;
        await _socket.ConnectAsync(BuildUri(baseUrl), cancellationToken);
    }

    public Task<JsonDocument?> ReceiveAsync(CancellationToken cancellationToken) => ReceiveDocumentAsync(cancellationToken);

    public Task SendListRoomsAsync(CancellationToken cancellationToken) =>
        SendAsync(new RemoteListRoomsRequestDto(), cancellationToken);

    public Task SendChatAsync(string playerId, string text, CancellationToken cancellationToken) =>
        SendAsync(new RemoteLobbyChatRequestDto
        {
            Payload = new RemoteLobbyChatPayloadDto
            {
                PlayerId = playerId,
                Text = text
            }
        }, cancellationToken);

    public Task SendCreateRoomAsync(string playerId, string roomName, string roomId, CancellationToken cancellationToken) =>
        SendAsync(new RemoteCreateRoomRequestDto
        {
            Payload = new RemoteCreateRoomPayloadDto
            {
                PlayerId = playerId,
                RoomName = roomName,
                RoomId = roomId
            }
        }, cancellationToken);

    public Task SendPrivateChatAsync(string playerId, string targetPlayerId, string? roomName, string? roomId, CancellationToken cancellationToken) =>
        SendAsync(new RemotePrivateChatRequestDto
        {
            Payload = new RemotePrivateChatPayloadDto
            {
                PlayerId = playerId,
                TargetPlayerId = targetPlayerId,
                RoomName = roomName,
                RoomId = roomId
            }
        }, cancellationToken);

    private async Task SendAsync(object payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, RemoteJsonSerializer.Options));
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
