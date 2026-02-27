using System.Text.Json;
using Haggis.Infrastructure.Services.Engine.Loop;
using Haggis.Domain.Model;
using Haggis.Infrastructure.Services.Application;
using Haggis.Infrastructure.Services.Engine;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Infrastructure.Services.GameRooms;
using Haggis.Infrastructure.Services.Infrastructure.Sessions;
using Haggis.Infrastructure.Services.Models;
using NUnit.Framework;

namespace Haggis.Infrastructure.Tests;

[TestFixture]
public class HaggisAiGameFlowIntegrationMatrixTests
{
    [TestCaseSource(nameof(GameFlowCases))]
    public void SimulatedGame_ShouldReachGameOver_WithConfiguredAiFlow(int seed, bool oneHumanTwoAi, bool useMonteCarlo)
    {
        var (service, gameId) = CreateService(seed, oneHumanTwoAi, useMonteCarlo);
        var init = CreateInitializeMessage(seed, oneHumanTwoAi, useMonteCarlo);
        var initResult = service.Handle(gameId, init);

        Assert.That(initResult.Type, Is.EqualTo("CommandApplied"), initResult.Error);
        Assert.That(initResult.State, Is.Not.Null);

        var appliedCommands = 0;
        var currentEvent = initResult;

        while (appliedCommands++ < 800)
        {
            var data = currentEvent.State!.Data;
            var isGameOver = data.GetProperty("gameOver").GetBoolean();
            if (isGameOver)
            {
                Assert.That(data.GetProperty("players").EnumerateArray().Any(player => player.GetProperty("score").GetInt32() >= data.GetProperty("winScore").GetInt32()), Is.True);
                return;
            }

            var currentPlayerId = data.GetProperty("currentPlayerId").GetString();
            var players = data.GetProperty("players").EnumerateArray().ToList();
            var currentPlayer = players.First(player =>
                string.Equals(player.GetProperty("id").GetString(), currentPlayerId, StringComparison.OrdinalIgnoreCase));
            var isAi = currentPlayer.GetProperty("isAi").GetBoolean();

            GameClientMessage nextMessage;
            if (isAi)
            {
                nextMessage = new GameClientMessage(
                    Type: "Command",
                    Command: new GameCommand("NextMove", string.Empty, EmptyPayload()),
                    State: null);
            }
            else
            {
                var action = data.GetProperty("possibleActions").EnumerateArray().First();
                var commandType = action.GetProperty("type").GetString() ?? "Pass";
                var commandPayload = commandType.Equals("Pass", StringComparison.OrdinalIgnoreCase)
                    ? EmptyPayload()
                    : ToJsonElement(new { action = action.GetProperty("action").GetString() });

                nextMessage = new GameClientMessage(
                    Type: "Command",
                    Command: new GameCommand(commandType, currentPlayerId!, commandPayload),
                    State: null);
            }

            currentEvent = service.Handle(gameId, nextMessage);
            Assert.That(currentEvent.Type, Is.EqualTo("CommandApplied"), currentEvent.Error);
            Assert.That(currentEvent.State, Is.Not.Null);
        }

        Assert.Fail("Game did not reach game over within command limit.");
    }

    public static IEnumerable<TestCaseData> GameFlowCases()
    {
        for (var seed = 1; seed <= 75; seed++)
        {
            yield return new TestCaseData(seed, false, false).SetName($"Integration_Seed{seed}_2Human1Ai_Heuristic");
            yield return new TestCaseData(seed, false, true).SetName($"Integration_Seed{seed}_2Human1Ai_MonteCarlo");
            yield return new TestCaseData(seed, true, false).SetName($"Integration_Seed{seed}_1Human2Ai_Heuristic");
            yield return new TestCaseData(seed, true, true).SetName($"Integration_Seed{seed}_1Human2Ai_MonteCarlo");
        }
    }

    private static (IGameCommandApplicationService service, string gameId) CreateService(int seed, bool oneHumanTwoAi, bool useMonteCarlo)
    {
        var gameId = $"matrix-{seed}-{(oneHumanTwoAi ? "1h2a" : "2h1a")}-{(useMonteCarlo ? "mc" : "heur")}";
        var aiMoveStrategy = new HaggisAiMoveStrategy();
        var moveRuleValidator = new HaggisMoveRuleValidator();
        var gameLoop = new HaggisServerGameLoop(aiMoveStrategy, moveRuleValidator);
        var gameEngine = new HaggisGameEngine(gameLoop);
        var sessionStore = new GameSessionStore(gameEngine);
        var roomStore = new GameRoomStore();

        roomStore.GetOrCreateRoom(gameId, "human-1", "haggis");
        roomStore.TryJoinRoom(gameId, "human-2", out _);

        var service = new GameCommandApplicationService(sessionStore, roomStore);
        return (service, gameId);
    }

    private static GameClientMessage CreateInitializeMessage(int seed, bool oneHumanTwoAi, bool useMonteCarlo)
    {
        object aiDescriptor = useMonteCarlo
            ? new
            {
                strategy = "montecarlo",
                simulations = 20,
                timeBudgetMs = 1
            }
            : new
            {
                strategy = "heuristic",
                useWildsInContinuations = true,
                takeLessValueTrickFirst = true,
                filter = "continuations",
                filterLimit = 5
            };

        object[] players = oneHumanTwoAi
            ? new object[]
            {
                new { id = "human-1", type = "human" },
                new { id = "bot-1", type = "ai", ai = aiDescriptor },
                new { id = "bot-2", type = "ai", ai = aiDescriptor }
            }
            : new object[]
            {
                new { id = "human-1", type = "human" },
                new { id = "human-2", type = "human" },
                new { id = "bot-1", type = "ai", ai = aiDescriptor }
            };

        var payload = ToJsonElement(new
        {
            players,
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
        });

        return new GameClientMessage(
            Type: "Command",
            Command: new GameCommand("Initialize", "human-1", payload),
            State: null);
    }

    private static JsonElement ToJsonElement<T>(T model)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(model));
        return doc.RootElement.Clone();
    }

    private static JsonElement EmptyPayload()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }
}
