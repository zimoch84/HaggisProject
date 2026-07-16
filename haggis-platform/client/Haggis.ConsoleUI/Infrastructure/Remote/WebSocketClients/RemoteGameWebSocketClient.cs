using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.ConsoleUI.Application.Game;

public sealed class RemoteGameWebSocketClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();

    public async Task ConnectAsync(RemoteGameOptions options, CancellationToken cancellationToken)
    {
        await _socket.ConnectAsync(BuildGameUri(options.ServerBaseUrl, options.GameId), cancellationToken);
    }

    public Task SendJoinAsync(string playerId, CancellationToken cancellationToken) =>
        SendAsync(new RemoteJoinGameRequestDto
        {
            Payload = new RemotePlayerPayloadDto { PlayerId = playerId }
        }, cancellationToken);

    public Task SendSnapshotAsync(string playerId, CancellationToken cancellationToken) =>
        SendAsync(new RemoteSnapshotRequestDto
        {
            Payload = new RemotePlayerPayloadDto { PlayerId = playerId }
        }, cancellationToken);

    public Task SendCreateAsync(
        string playerId,
        int? seed,
        int? playerCount,
        IReadOnlyList<RemoteCreateGamePlayerDto>? players,
        CancellationToken cancellationToken)
    {
        return SendAsync(new RemoteCreateGameRequestDto
        {
            Payload = new RemoteCreateGameEnvelopeDto
            {
                PlayerId = playerId,
                Payload = new RemoteCreateGamePayloadDto
                {
                    Seed = seed,
                    PlayerCount = playerCount,
                    Players = players?.ToList()
                }
            }
        }, cancellationToken);
    }

    public Task SendActionAsync(string playerId, RemotePossibleActionDto action, CancellationToken cancellationToken)
    {
        if (string.Equals(action.Type, "Pass", StringComparison.OrdinalIgnoreCase))
        {
            return SendAsync(new RemoteCommandRequestDto
            {
                Payload = new RemoteCommandEnvelopeDto
                {
                    Command = new RemoteOutboundCommandDto
                    {
                        Type = "Pass",
                        PlayerId = playerId,
                        Payload = new RemoteGameCommandPayloadDto()
                    }
                }
            }, cancellationToken);
        }

        return SendAsync(new RemoteCommandRequestDto
        {
            Payload = new RemoteCommandEnvelopeDto
            {
                Command = new RemoteOutboundCommandDto
                {
                    Type = "Play",
                    PlayerId = playerId,
                    Payload = new RemoteGameCommandPayloadDto
                    {
                        Action = action.DisplayAction
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
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, RemoteJsonSerializer.Options));
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
