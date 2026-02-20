using System.Text.Json;
using Game.API.Services.Interfaces;
using Game.API.Services.Models;

namespace Game.API.Services.Engine;

public sealed class GenericGameEngine : IGameEngine
{
    public GameStateSnapshot CreateInitialState(string gameId)
    {
        return GameStateSnapshot.Initial;
    }

    public GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command)
    {
        var nextData = ResolveNextData(state, command);
        return state with
        {
            Version = state.Version + 1,
            Data = nextData,
            UpdatedAt = DateTimeOffset.UtcNow
        };
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
