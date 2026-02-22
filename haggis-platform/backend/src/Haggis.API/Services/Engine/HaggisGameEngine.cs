using System.Text.Json;
using Haggis.API.Services.Engine.Haggis;
using Haggis.Model;
using Haggis.API.Services.Interfaces;
using Haggis.API.Services.Models;

namespace Haggis.API.Services.Engine;

public sealed class HaggisGameEngine : IGameEngine
{
    private readonly HaggisServerGameLoop _gameLoop;

    public HaggisGameEngine(HaggisServerGameLoop gameLoop)
    {
        _gameLoop = gameLoop;
    }

    public GameStateSnapshot CreateInitialState(string gameId)
    {
        return GameStateSnapshot.Initial;
    }

    public GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command)
    {
        var nextData = _gameLoop.TryExecute(gameId, command, out var haggisState, out var appliedMove)
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
        var data = new
        {
            game = "haggis",
            currentPlayerId = state.CurrentPlayer.Name,
            roundOver = state.RoundOver(),
            players = state.Players.Select(player => new
            {
                id = player.Name,
                score = player.Score,
                handCount = player.Hand.Count,
                finished = player.Finished
            }),
            trick = state.CurrentTrickPlay.Actions.Select(action => new
            {
                playerId = action.PlayerName,
                isPass = action.IsPass,
                desc = action.Desc
            }),
            possibleActions = state.Actions.Select(action => new
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
