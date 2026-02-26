using System.Text.Json;
using Haggis.AI.Model;
using Haggis.Domain.Model;
using Haggis.Infrastructure.Services.Engine.Haggis;
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
        JsonElement nextData;
        if (GameLoop.TryExecute(gameId, command, out var haggisState, out var appliedMove))
        {
            var appliedMoves = new List<HaggisAction>();
            if (appliedMove is not null)
            {
                appliedMoves.Add(appliedMove);
            }
            var finalState = AdvanceGameUntilHumanTurnOrGameOver(gameId, haggisState!, appliedMoves);
            nextData = BuildHaggisStateData(finalState, command, appliedMove, appliedMoves);
        }
        else
        {
            nextData = ResolveNextData(state, command);
        }

        return state with
        {
            Version = state.Version + 1,
            Data = nextData,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private HaggisGameState AdvanceGameUntilHumanTurnOrGameOver(
        string gameId,
        HaggisGameState state,
        List<HaggisAction> appliedMoves)
    {
        var safetyCounter = 0;
        while (safetyCounter++ < 5000)
        {
            var gameOver = state.RoundOver() && state.Players.Any(player => player.Score >= state.ScoringStrategy.GameOverScore);
            if (gameOver)
            {
                return state;
            }

            if (state.RoundOver())
            {
                if (!GameLoop.TryCreateNextRound(gameId, state, out var nextRoundState) || nextRoundState is null)
                {
                    return state;
                }

                state = nextRoundState;
                continue;
            }

            if (state.CurrentPlayer is not AIPlayer)
            {
                return state;
            }

            if (!GameLoop.TryExecuteAiStep(gameId, out var aiState, out var aiAppliedMove) || aiState is null)
            {
                return state;
            }

            if (aiAppliedMove is not null)
            {
                appliedMoves.Add(aiAppliedMove);
            }
            state = aiState;
        }

        throw new InvalidOperationException("AI progression safety threshold was reached.");
    }

    private static JsonElement BuildHaggisStateData(
        HaggisGameState state,
        GameCommand command,
        HaggisAction? appliedMove,
        IReadOnlyList<HaggisAction> appliedMoves)
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
                finished = player.Finished,
                isAi = player is AIPlayer
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
            appliedMoves = appliedMoves.Select(move => new
            {
                playerId = move.PlayerName,
                isPass = move.IsPass,
                action = move.Desc
            }),
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
