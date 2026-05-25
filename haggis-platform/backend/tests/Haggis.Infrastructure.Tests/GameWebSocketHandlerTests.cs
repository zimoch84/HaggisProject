using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Engine;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Infrastructure.Services.Infrastructure;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.WebSocketHandlers;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameWebSocketHubTests
{
    [Test]
    public async Task HandleClientAsync_BroadcastsAppliedCommandToClientsInSameGame()
    {
        var senderSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}"),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}}", delayMs: 50),
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
        Assert.That(GetRequiredPropertyIgnoreCase(payload.RootElement, "Type").GetString(), Is.EqualTo("CommandApplied"));
        Assert.That(GetRequiredPropertyIgnoreCase(payload.RootElement, "GameId").GetString(), Is.EqualTo("game-1"));
        Assert.That(GetRequiredPropertyIgnoreCase(payload.RootElement, "OrderPointer").GetInt64(), Is.EqualTo(1));
        Assert.That(GetRequiredPropertyIgnoreCase(
                GetRequiredPropertyIgnoreCase(payload.RootElement, "Command"),
                "Type").GetString(),
            Is.EqualTo("Initialize"));
    }

    [Test]
    public async Task HandleClientAsync_IncrementsOrderPointerPerGame()
    {
        var senderSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}"),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}}", delayMs: 50),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":456}}}}"),
            FakeWebSocket.Close());

        var hub = CreateHub();

        await hub.HandleClientAsync("game-2", senderSocket, CancellationToken.None);

        var sent = senderSocket.GetSentTextMessages().Where(IsCommandApplied).ToList();
        Assert.That(sent.Count, Is.EqualTo(2));

        using var msg1 = JsonDocument.Parse(sent[0]);
        using var msg2 = JsonDocument.Parse(sent[1]);

        Assert.That(GetRequiredPropertyIgnoreCase(msg1.RootElement, "OrderPointer").GetInt64(), Is.EqualTo(1));
        Assert.That(GetRequiredPropertyIgnoreCase(msg2.RootElement, "OrderPointer").GetInt64(), Is.EqualTo(2));
    }

    [Test]
    public async Task HandleClientAsync_DoesNotBroadcastAcrossDifferentGames()
    {
        var senderGameA = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}"),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}}", delayMs: 50),
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

    [Test]
    public async Task HandleClientAsync_RemovesDisconnectedPlayerAfterNetworkLoss_AndKeepsBroadcastingToRemainingPlayer()
    {
        var disconnectedPlayerSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}"),
            FakeWebSocket.NetworkLoss(delayMs: 50));

        var remainingPlayerSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p2\"}}"),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p2\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":123}}}}", delayMs: 150),
            FakeWebSocket.Close(delayMs: 50));

        var registry = new PlayerSocketRegistry();
        var hub = CreateHub(registry);

        var disconnectedTask = hub.HandleClientAsync("game-network-loss", disconnectedPlayerSocket, CancellationToken.None);
        var remainingTask = hub.HandleClientAsync("game-network-loss", remainingPlayerSocket, CancellationToken.None);

        await Task.WhenAll(disconnectedTask, remainingTask);

        Assert.That(registry.GetOnlinePlayerConnectionCounts().ContainsKey("p1"), Is.False);
        Assert.That(registry.GetOnlinePlayerConnectionCounts().ContainsKey("p2"), Is.False);
        Assert.That(disconnectedPlayerSocket.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(0));
        Assert.That(remainingPlayerSocket.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task HandleClientAsync_ReconnectForSamePlayer_RemovesStaleClientConnection()
    {
        var staleSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}"),
            FakeWebSocket.Close(delayMs: 400));

        var reconnectedSocket = FakeWebSocket.FromClientMessages(
            FakeWebSocket.Text("{\"operation\":\"join\",\"payload\":{\"playerId\":\"p1\"}}", delayMs: 50),
            FakeWebSocket.Text("{\"operation\":\"command\",\"payload\":{\"command\":{\"type\":\"Initialize\",\"playerId\":\"p1\",\"payload\":{\"players\":[\"p1\",\"p2\",\"p3\"],\"seed\":321}}}}", delayMs: 100),
            FakeWebSocket.Close(delayMs: 50));

        var hub = CreateHub();

        var staleTask = hub.HandleClientAsync("game-reconnect", staleSocket, CancellationToken.None);
        var reconnectedTask = hub.HandleClientAsync("game-reconnect", reconnectedSocket, CancellationToken.None);

        await Task.WhenAll(staleTask, reconnectedTask);

        Assert.That(staleSocket.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(0));
        Assert.That(reconnectedSocket.GetSentTextMessages().Where(IsCommandApplied).Count(), Is.EqualTo(1));
    }

    private static bool IsCommandApplied(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return TryGetPropertyIgnoreCase(doc.RootElement, "Type", out var type) &&
               string.Equals(type.GetString(), "CommandApplied", StringComparison.Ordinal);
    }

    private static JsonElement GetRequiredPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Missing JSON property '{propertyName}'.");
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static GameWebSocketHandler CreateHub(IPlayerSocketRegistry? registry = null)
    {
        var gameLoop = new HaggisServerGameLoop(
            new HaggisAiMoveStrategy(),
            new HaggisMoveRuleValidator());
        var engine = new HaggisGameEngine(gameLoop);
        var store = new GameSessionStore(engine);
        var roomStore = new GameRoomStore();
        var appService = new GameCommandApplicationService(store, roomStore);
        var effectiveRegistry = registry ?? new PlayerSocketRegistry();
        var connectionManager = new GameConnectionManager(effectiveRegistry);
        return new GameWebSocketHandler(appService, connectionManager, roomStore, new NoOpGameWebSocketAuditLogger());
    }

    private sealed class NoOpGameWebSocketAuditLogger : IGameWebSocketAuditLogger
    {
        public void Log(GameWebSocketAuditEntry entry)
        {
        }
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

        public static IncomingFrame NetworkLoss(int delayMs = 0)
        {
            return new IncomingFrame(WebSocketMessageType.Binary, Array.Empty<byte>(), true, delayMs, FailWithNetworkLoss: true);
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

            if (frame.FailWithNetworkLoss)
            {
                _state = WebSocketState.Aborted;
                throw new WebSocketException("Simulated network loss.");
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

        public readonly record struct IncomingFrame(
            WebSocketMessageType MessageType,
            byte[] Payload,
            bool EndOfMessage,
            int DelayMs,
            bool FailWithNetworkLoss = false);
    }
}
