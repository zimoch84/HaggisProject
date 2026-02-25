using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Engine;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Infrastructure.Services.GameRooms;
using Haggis.Infrastructure.Services.Hubs;
using Haggis.Infrastructure.Services.Infrastructure.Sessions;
using Haggis.Infrastructure.Services;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameWebSocketHubTests
{
    [Test]
    public async Task HandleClientAsync_BroadcastsAppliedCommandToClientsInSameGame()
    {
        var senderSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"type\":\"JoinRoom\",\"playerId\":\"p1\"}"),
            FakeWebSocket.Text("{\"type\":\"Command\",\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}", delayMs: 50),
            FakeWebSocket.Close());

        var receiverSocket = FakeWebSocket.FromClientMessages(FakeWebSocket.Close(delayMs: 250));
        var hub = CreateHub();

        var receiverTask = hub.HandleClientAsync("game-1", receiverSocket, CancellationToken.None);
        var senderTask = hub.HandleClientAsync("game-1", senderSocket, CancellationToken.None);

        await Task.WhenAll(senderTask, receiverTask);

        var senderApplied = senderSocket.GetSentTextMessages().Where(IsCommandApplied).ToList();
        var receiverApplied = receiverSocket.GetSentTextMessages().Where(IsCommandApplied).ToList();
        Assert.That(senderApplied.Count, Is.EqualTo(1));
        Assert.That(receiverApplied.Count, Is.EqualTo(1));

        using var payload = JsonDocument.Parse(receiverApplied[0]);
        Assert.That(payload.RootElement.GetProperty("Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(payload.RootElement.GetProperty("GameId").GetString(), Is.EqualTo("game-1"));
        Assert.That(payload.RootElement.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(1));
        Assert.That(payload.RootElement.GetProperty("Command").GetProperty("Type").GetString(), Is.EqualTo("Initialize"));
    }

    [Test]
    public async Task HandleClientAsync_IncrementsOrderPointerPerGame()
    {
        var senderSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"type\":\"JoinRoom\",\"playerId\":\"p1\"}"),
            FakeWebSocket.Text("{\"type\":\"Command\",\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}", delayMs: 50),
            FakeWebSocket.Text("{\"type\":\"Command\",\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":456}}}"),
            FakeWebSocket.Close());

        var hub = CreateHub();

        await hub.HandleClientAsync("game-2", senderSocket, CancellationToken.None);

        var sent = senderSocket.GetSentTextMessages().Where(IsCommandApplied).ToList();
        Assert.That(sent.Count, Is.EqualTo(2));

        using var msg1 = JsonDocument.Parse(sent[0]);
        using var msg2 = JsonDocument.Parse(sent[1]);

        Assert.That(msg1.RootElement.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(1));
        Assert.That(msg2.RootElement.GetProperty("OrderPointer").GetInt64(), Is.EqualTo(2));
    }

    [Test]
    public async Task HandleClientAsync_DoesNotBroadcastAcrossDifferentGames()
    {
        var senderGameA = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"type\":\"JoinRoom\",\"playerId\":\"p1\"}"),
            FakeWebSocket.Text("{\"type\":\"Command\",\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}", delayMs: 50),
            FakeWebSocket.Close());

        var receiverGameA = FakeWebSocket.FromClientMessages(FakeWebSocket.Close(delayMs: 250));
        var receiverGameB = FakeWebSocket.FromClientMessages(FakeWebSocket.Close(delayMs: 250));

        var hub = CreateHub();

        var taskA1 = hub.HandleClientAsync("game-A", senderGameA, CancellationToken.None);
        var taskA2 = hub.HandleClientAsync("game-A", receiverGameA, CancellationToken.None);
        var taskB1 = hub.HandleClientAsync("game-B", receiverGameB, CancellationToken.None);

        await Task.WhenAll(taskA1, taskA2, taskB1);

        Assert.That(receiverGameA.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(1));
        Assert.That(receiverGameB.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(0));
    }

    private static bool IsCommandApplied(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.TryGetProperty("Type", out var type) &&
               string.Equals(type.GetString(), "CommandApplied", StringComparison.Ordinal);
    }

    private static GameWebSocketHub CreateHub()
    {
        var gameLoop = new HaggisServerGameLoop(
            new HaggisAiMoveStrategy(),
            new HaggisMoveRuleValidator());
        var engine = new HaggisGameEngine(gameLoop);
        var store = new GameSessionStore(engine);
        var roomStore = new GameRoomStore();
        var appService = new GameCommandApplicationService(store, roomStore);
        var registry = new PlayerSocketRegistry();
        return new GameWebSocketHub(appService, registry, roomStore);
    }

    private sealed class FakeWebSocket : WebSocket
    {
        private readonly ConcurrentQueue<IncomingFrame> _incomingFrames;
        private readonly List<string> _sentTextMessages = new();
        private readonly object _sync = new();

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
            lock (_sync)
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
                lock (_sync)
                {
                    _sentTextMessages.Add(text);
                }
            }

            return Task.CompletedTask;
        }

        public readonly record struct IncomingFrame(WebSocketMessageType MessageType, byte[] Payload, bool EndOfMessage, int DelayMs);
    }
}
