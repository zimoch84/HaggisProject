using System.Text.Json;
using Haggis.Infrastructure.Services.Engine.Haggis;
using Haggis.Domain.Model;
using Haggis.Infrastructure.Services.Interfaces;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Engine;

public sealed class HaggisGameEngine : IGameEngine
{
    private HaggisServerGameLoop GameLoop { get; }

    public HaggisGameEngine(HaggisServerGameLoop gameLoop)
    {
        GameLoop = gameLoop;
    }

    public GameStateSnapshot CreateInitialState(string gameId)
    {
        return GameStateSnapshot.Initial;
    }

    public GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command)
    {
        var nextData = GameLoop.TryExecute(gameId, command, out var haggisState, out var appliedMove)
            ? BuildHaggisStateData(haggisState!, command, appliedMove)
            : ResolveNextData(state, command);

        return state with
        {
            Version = state.Version + 1,
            Data = nextData,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static JsonElement BuildHaggisStateData(HaggisGameState state, GameCommand command, HaggisAction? appliedMove)
    {
        var gameOverScore = state.ScoringStrategy.GameOverScore;
        var roundOver = state.RoundOver();
        var gameOver = roundOver && state.Players.Any(player => player.Score >= gameOverScore);

        var data = new
        {
            game = "haggis",
            winScore = gameOverScore,
            roundNumber = state.RoundNumber,
            moveIteration = state.MoveIteration,
            currentPlayerId = state.CurrentPlayer.Name,
            roundOver,
            gameOver,
            players = state.Players.Select(player => new
            {
                id = player.Name,
                score = player.Score,
                handCount = player.Hand.Count,
                hand = player.Hand.Select(card => card.ToString()),
                finished = player.Finished
            }),
            trick = state.CurrentTrickPlay.Actions.Select(action => new
            {
                playerId = action.PlayerName,
                isPass = action.IsPass,
                desc = action.Desc
            }),
            possibleActions = state.PossibleActions.Select(action => new
            {
                type = action.IsPass ? "Pass" : "Play",
                action = action.Desc
            }),
            appliedMove = appliedMove is null
                ? null
                : new
                {
                    playerId = appliedMove.PlayerName,
                    isPass = appliedMove.IsPass,
                    action = appliedMove.Desc
                },
            lastCommand = new
            {
                type = command.Type,
                playerId = command.PlayerId
            }
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(data));
        return document.RootElement.Clone();
    }

    private static JsonElement ResolveNextData(GameStateSnapshot state, GameCommand command)
    {
        if (command.Payload.ValueKind == JsonValueKind.Object &&
            command.Payload.TryGetProperty("state", out var explicitState))
        {
            return explicitState.Clone();
        }

        var nextData = new
        {
            previousVersion = state.Version,
            lastCommand = new
            {
                type = command.Type,
                playerId = command.PlayerId,
                payload = command.Payload
            }
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(nextData));
        return document.RootElement.Clone();
    }
}


