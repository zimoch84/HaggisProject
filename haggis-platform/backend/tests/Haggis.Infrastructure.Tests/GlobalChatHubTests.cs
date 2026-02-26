using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Haggis.Infrastructure.Dtos.Chat;
using Haggis.Infrastructure.Services;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GlobalChatHubTests
{
    [Test]
    public async Task HandleClientAsync_WhenPayloadInvalid_SendsProblemDetailsMessage()
    {
        var socket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"chat\",\"payload\":{\"playerId\":\"\",\"text\":\"hello\"}}"),
            FakeWebSocket.Close());

        var hub = new GlobalChatHub(new PlayerSocketRegistry());

        await hub.HandleClientAsync(socket, CancellationToken.None);

        var sentMessages = socket.GetSentTextMessages();
        Assert.That(sentMessages.Count, Is.EqualTo(2));

        var payload = JsonSerializer.Deserialize<ProblemDetailsMessage>(sentMessages[^1]);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Title, Is.EqualTo("Invalid chat payload."));
        Assert.That(payload.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task HandleClientAsync_WhenValidPayload_BroadcastsToAllConnectedClients()
    {
        var senderSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"chat\",\"payload\":{\"playerId\":\"player-1\",\"text\":\"  hello world  \"}}"),
            FakeWebSocket.Close());

        var receiverSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Close(delayMs: 250));

        var hub = new GlobalChatHub(new PlayerSocketRegistry());

        var receiverTask = hub.HandleClientAsync(receiverSocket, CancellationToken.None);
        var senderTask = hub.HandleClientAsync(senderSocket, CancellationToken.None);

        await Task.WhenAll(senderTask, receiverTask);

        Assert.That(senderSocket.GetSentTextMessages().Count, Is.EqualTo(2));
        Assert.That(receiverSocket.GetSentTextMessages().Count, Is.EqualTo(2));

        var payload = JsonSerializer.Deserialize<ChatMessage>(receiverSocket.GetSentTextMessages()[1]);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.PlayerId, Is.EqualTo("player-1"));
        Assert.That(payload.Text, Is.EqualTo("hello world"));
        Assert.That(payload.MessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(payload.CreatedAt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    private sealed class FakeWebSocket : WebSocket
    {
        private readonly ConcurrentQueue<IncomingFrame> _incomingFrames;
        private readonly List<string> _sentTextMessages = new();
        private readonly object _sentLock = new();

        private WebSocketState _state = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeStatusDescription;

        private FakeWebSocket(IEnumerable<IncomingFrame> incomingFrames)
        {
            _incomingFrames = new ConcurrentQueue<IncomingFrame>(incomingFrames);
        }

        public static FakeWebSocket FromClientMessages(params IncomingFrame[] messages)
        {
            return new FakeWebSocket(messages);
        }

        public static IncomingFrame Text(string text, int delayMs = 0)
        {
            return new IncomingFrame(WebSocketMessageType.Text, Encoding.UTF8.GetBytes(text), true, delayMs);
        }

        public static IncomingFrame Close(int delayMs = 0)
        {
            return new IncomingFrame(WebSocketMessageType.Close, Array.Empty<byte>(), true, delayMs);
        }

        public IReadOnlyList<string> GetSentTextMessages()
        {
            lock (_sentLock)
            {
                return _sentTextMessages.ToList();
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string? CloseStatusDescription => _closeStatusDescription;

        public override WebSocketState State => _state;

        public override string SubProtocol => string.Empty;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _closeStatus = closeStatus;
            _closeStatusDescription = statusDescription;
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;

        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_incomingFrames.TryDequeue(out var frame))
            {
                _state = WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }

            if (frame.DelayMs > 0)
            {
                await Task.Delay(frame.DelayMs, cancellationToken);
            }

            if (frame.MessageType == WebSocketMessageType.Close)
            {
                _state = WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }

            var bytesToCopy = Math.Min(buffer.Count, frame.Payload.Length);
            frame.Payload.AsSpan(0, bytesToCopy).CopyTo(buffer.AsSpan(0, bytesToCopy));
            return new WebSocketReceiveResult(bytesToCopy, frame.MessageType, frame.EndOfMessage);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (messageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
                lock (_sentLock)
                {
                    _sentTextMessages.Add(text);
                }
            }

            return Task.CompletedTask;
        }

        public readonly record struct IncomingFrame(WebSocketMessageType MessageType, byte[] Payload, bool EndOfMessage, int DelayMs);
    }
}


