using System.Collections.Concurrent;
using System.Text.Json;
using Haggis.Interfaces;
using Haggis.Model;
using Haggis.API.Services.Interfaces;
using Haggis.API.Services.Models;

namespace Haggis.API.Services.Engine;

public sealed class HaggisGameEngine : IGameEngine
{
    private readonly ConcurrentDictionary<string, HaggisGameState> _haggisStates = new();

    public GameStateSnapshot CreateInitialState(string gameId)
    {
        return GameStateSnapshot.Initial;
    }

    public GameStateSnapshot SimulateNext(string gameId, GameStateSnapshot state, GameCommand command)
    {
        var nextData = TryResolveHaggisData(gameId, command, out var haggisData)
            ? haggisData
            : ResolveNextData(state, command);

        return state with
        {
            Version = state.Version + 1,
            Data = nextData,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private bool TryResolveHaggisData(string gameId, GameCommand command, out JsonElement data)
    {
        data = default;

        if (IsInitCommand(command.Type))
        {
            var initialized = InitializeHaggisState(gameId, command);
            data = BuildHaggisStateData(initialized, command);
            return true;
        }

        if (!IsPlayOrPass(command.Type))
        {
            return false;
        }

        if (!_haggisStates.TryGetValue(gameId, out var haggisState))
        {
            return false;
        }

        var action = MapToAction(haggisState, command);
        haggisState.ApplyAction(action);
        data = BuildHaggisStateData(haggisState, command);
        return true;
    }

    private HaggisGameState InitializeHaggisState(string gameId, GameCommand command)
    {
        var playerIds = ReadPlayers(command.Payload);
        if (playerIds.Count < 2)
        {
            throw new InvalidOperationException("Haggis requires at least 2 players in payload.players.");
        }

        var players = playerIds.Select(id => (IHaggisPlayer)new HaggisPlayer(id)).ToList();
        var game = new HaggisGame(players);

        if (command.Payload.ValueKind == JsonValueKind.Object &&
            command.Payload.TryGetProperty("seed", out var seedElement) &&
            seedElement.ValueKind == JsonValueKind.Number &&
            seedElement.TryGetInt32(out var seed))
        {
            game.SetSeed(seed);
        }

        game.NewRound();
        var state = new HaggisGameState(players);
        _haggisStates[gameId] = state;
        return state;
    }

    private static bool IsInitCommand(string type)
    {
        return type.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Start", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayOrPass(string type)
    {
        return type.Equals("Play", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Pass", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ReadPlayers(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("players", out var playersElement) ||
            playersElement.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return playersElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HaggisAction MapToAction(HaggisGameState state, GameCommand command)
    {
        var player = state.Players.FirstOrDefault(p =>
            p.Name.Equals(command.PlayerId, StringComparison.OrdinalIgnoreCase));
        if (player is null)
        {
            throw new InvalidOperationException($"Player '{command.PlayerId}' is not part of this Haggis game.");
        }

        if (!state.CurrentPlayer.GUID.Equals(player.GUID))
        {
            throw new InvalidOperationException($"It is not '{command.PlayerId}' turn.");
        }

        if (command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            var passAction = state.Actions.FirstOrDefault(a => a.IsPass);
            if (passAction is null)
            {
                throw new InvalidOperationException("Pass is not a legal action right now.");
            }

            return passAction;
        }

        var action = ResolvePlayAction(state, player, command.Payload);
        var isLegal = state.Actions.Any(x => x.Equals(action));
        if (!isLegal)
        {
            throw new InvalidOperationException("Provided play action is not legal in current state.");
        }

        return action;
    }

    private static HaggisAction ResolvePlayAction(HaggisGameState state, IHaggisPlayer player, JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("action", out var actionElement) &&
            actionElement.ValueKind == JsonValueKind.String)
        {
            var actionValue = actionElement.GetString();
            var matchingAction = state.Actions.FirstOrDefault(x => !x.IsPass && x.Desc.Equals(actionValue, StringComparison.Ordinal));
            if (matchingAction is not null)
            {
                return matchingAction;
            }
        }

        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty("trick", out var trickElement) &&
            trickElement.ValueKind == JsonValueKind.String)
        {
            return HaggisAction.FromTrick(trickElement.GetString()!, player);
        }

        throw new InvalidOperationException("Play command payload must contain either 'action' or 'trick'.");
    }

    private static JsonElement BuildHaggisStateData(HaggisGameState state, GameCommand command)
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
