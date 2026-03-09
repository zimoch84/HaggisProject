using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public sealed class RemoteGameWebSocketClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly ClientWebSocket _socket = new();

    public async Task ConnectAsync(RemoteGameOptions options, CancellationToken cancellationToken)
    {
        await _socket.ConnectAsync(BuildGameUri(options.ServerBaseUrl, options.GameId), cancellationToken);
    }

    public Task SendJoinAsync(string playerId, CancellationToken cancellationToken) =>
        SendAsync(new
        {
            operation = "join",
            payload = new
            {
                playerId
            }
        }, cancellationToken);

    public Task SendCreateAsync(string playerId, int? seed, CancellationToken cancellationToken)
    {
        object payload = seed.HasValue
            ? new { seed = seed.Value }
            : new { };

        return SendAsync(new
        {
            operation = "create",
            payload = new
            {
                playerId,
                payload
            }
        }, cancellationToken);
    }

    public Task SendActionAsync(string playerId, RemotePossibleAction action, CancellationToken cancellationToken)
    {
        if (action.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            return SendAsync(new
            {
                operation = "command",
                payload = new
                {
                    command = new
                    {
                        type = "Pass",
                        playerId,
                        payload = new { }
                    }
                }
            }, cancellationToken);
        }

        return SendAsync(new
        {
            operation = "command",
            payload = new
            {
                command = new
                {
                    type = "Play",
                    playerId,
                    payload = new
                    {
                        action = action.Action
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<JsonDocument?> ReceiveAsync(CancellationToken cancellationToken)
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

    private async Task SendAsync(object payload, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, SerializerOptions));
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static Uri BuildGameUri(string baseUrl, string gameId)
    {
        var builder = new UriBuilder(baseUrl)
        {
            Path = $"/ws/games/{gameId}"
        };

        builder.Scheme = builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
        return builder.Uri;
    }
}
