using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class GameWebSocketAiCompositionIntegrationTests
{
    [Test]
    public async Task WebSocketSimulation_ShouldFinishGame_ForThreeHumanPlayers()
    {
        await RunScenarioAsync(
            gameId: "ws-3-human",
            humans: new[] { "h1", "h2", "h3" },
            playersPayload: new object[]
            {
                new { id = "h1", type = "human" },
                new { id = "h2", type = "human" },
                new { id = "h3", type = "human" }
            },
            seed: 11);
    }

    [Test]
    public async Task WebSocketSimulation_ShouldFinishGame_ForTwoHumanOneAi()
    {
        await RunScenarioAsync(
            gameId: "ws-2-human-1-ai",
            humans: new[] { "h1", "h2" },
            playersPayload: new object[]
            {
                new { id = "h1", type = "human" },
                new { id = "h2", type = "human" },
                new
                {
                    id = "ai-1",
                    type = "ai",
                    ai = new
                    {
                        strategy = "heuristic",
                        useWildsInContinuations = true,
                        takeLessValueTrickFirst = true,
                        filter = "continuations",
                        filterLimit = 5
                    }
                }
            },
            seed: 12);
    }

    [Test]
    public async Task WebSocketSimulation_ShouldFinishGame_ForOneHumanTwoAi()
    {
        await RunScenarioAsync(
            gameId: "ws-1-human-2-ai",
            humans: new[] { "h1" },
            playersPayload: new object[]
            {
                new { id = "h1", type = "human" },
                new
                {
                    id = "ai-1",
                    type = "ai",
                    ai = new
                    {
                        strategy = "montecarlo",
                        simulations = 20,
                        timeBudgetMs = 1
                    }
                },
                new
                {
                    id = "ai-2",
                    type = "ai",
                    ai = new
                    {
                        strategy = "montecarlo",
                        simulations = 20,
                        timeBudgetMs = 1
                    }
                }
            },
            seed: 13);
    }

    private static async Task RunScenarioAsync(string gameId, string[] humans, object[] playersPayload, int seed)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = timeoutCts.Token;

        var wsClient = factory.Server.CreateWebSocketClient();
        var sockets = new Dictionary<string, WebSocket>();

        try
        {
            foreach (var human in humans)
            {
                var socket = await wsClient.ConnectAsync(new Uri($"ws://localhost/ws/games/{gameId}"), token);
                sockets[human] = socket;

                await SendTextAsync(socket,
                    JsonSerializer.Serialize(new
                    {
                        operation = "join",
                        payload = new { playerId = human }
                    }),
                    token);

                await ReceiveUntilRoomJoinedAsync(socket, token);
            }

            var ownerSocket = sockets[humans[0]];
            await SendTextAsync(ownerSocket,
                JsonSerializer.Serialize(new
                {
                    operation = "command",
                    payload = new
                    {
                        command = new
                        {
                            type = "Initialize",
                            playerId = humans[0],
                            payload = new
                            {
                                players = playersPayload,
                                seed,
                                options = new
                                {
                                    scoring = new
                                    {
                                        strategy = "EveryCardOnePoint",
                                        gameOverScore = 40,
                                        runOutMultiplier = 2
                                    }
                                }
                            }
                        }
                    }
                }),
                token);

            JsonElement stateData = default;
            for (var commandCount = 0; commandCount < 800; commandCount++)
            {
                stateData = await ReceiveAppliedStateForAllSocketsAsync(sockets.Values.ToList(), token);
                if (stateData.GetProperty("gameOver").GetBoolean())
                {
                    Assert.That(stateData.GetProperty("players").EnumerateArray().Any(player => player.GetProperty("score").GetInt32() >= stateData.GetProperty("winScore").GetInt32()), Is.True);
                    return;
                }

                var currentPlayerId = stateData.GetProperty("currentPlayerId").GetString();
                var possibleAction = stateData.GetProperty("possibleActions").EnumerateArray().First();
                var commandType = possibleAction.GetProperty("type").GetString() ?? "Pass";
                object payload = commandType.Equals("Pass", StringComparison.OrdinalIgnoreCase)
                    ? new { }
                    : new { action = possibleAction.GetProperty("action").GetString() };
                var senderSocket = sockets[currentPlayerId!];

                await SendTextAsync(senderSocket,
                    JsonSerializer.Serialize(new
                    {
                        operation = "command",
                        payload = new
                        {
                            command = new
                            {
                                type = commandType,
                                playerId = currentPlayerId,
                                payload
                            }
                        }
                    }),
                    token);
            }

            Assert.Fail("Scenario did not reach game over within command limit.");
        }
        finally
        {
            foreach (var socket in sockets.Values)
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                }

                socket.Dispose();
            }
        }
    }

    private static async Task<JsonElement> ReceiveAppliedStateForAllSocketsAsync(IReadOnlyList<WebSocket> sockets, CancellationToken token)
    {
        JsonElement? firstState = null;
        foreach (var socket in sockets)
        {
            while (true)
            {
                var message = await ReceiveTextAsync(socket, token);
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                if (!root.TryGetProperty("Type", out var typeElement) || typeElement.GetString() != "CommandApplied")
                {
                    continue;
                }

                var state = root.GetProperty("State").GetProperty("Data").Clone();
                firstState ??= state;
                break;
            }
        }

        return firstState!.Value;
    }

    private static async Task ReceiveUntilRoomJoinedAsync(WebSocket socket, CancellationToken token)
    {
        while (true)
        {
            var message = await ReceiveTextAsync(socket, token);
            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "RoomJoined")
            {
                return;
            }
        }
    }

    private static async Task SendTextAsync(WebSocket socket, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return string.Empty;
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
