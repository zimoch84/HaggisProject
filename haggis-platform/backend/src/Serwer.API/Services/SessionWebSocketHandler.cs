using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serwer.API.Dtos.Chat;
using Serwer.API.Dtos.Session;

namespace Serwer.API.Services;

public sealed class SessionWebSocketHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IPlayerSocketRegistry _playerSocketRegistry;

    public SessionWebSocketHandler(IPlayerSocketRegistry playerSocketRegistry)
    {
        _playerSocketRegistry = playerSocketRegistry;
    }

    public async Task HandleLoginAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection expected.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = _playerSocketRegistry.Register(socket, "session.login");

        try
        {
            while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
            {
                var raw = await ReceiveTextAsync(socket, context.RequestAborted);
                if (raw is null)
                {
                    break;
                }

                var request = JsonSerializer.Deserialize<SessionLoginRequest>(raw, SerializerOptions);
                if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
                {
                    await SendAsync(socket, new ProblemDetailsMessage("Invalid login payload.", 400), context.RequestAborted);
                    continue;
                }

                var playerId = request.PlayerId.Trim();
                _playerSocketRegistry.BindPlayer(connectionId, playerId);

                var response = new SessionLoginResponse(
                    SessionId: connectionId.ToString("N"),
                    PlayerId: playerId,
                    ConnectedAt: DateTimeOffset.UtcNow,
                    Status: "authenticated");

                await SendAsync(socket, response, context.RequestAborted);
            }
        }
        finally
        {
            _playerSocketRegistry.Unregister(connectionId);

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed.", CancellationToken.None);
            }
        }
    }

    private static async Task SendAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
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
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
