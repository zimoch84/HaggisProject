using System.Text.Json;
using Game.Core.Engine.Loop;
using Haggis.API.Services.Models;
using Haggis.Interfaces;
using Haggis.Model;

namespace Haggis.API.Services.Engine.Haggis;

public sealed class HaggisServerGameLoop : GameLoopEngineBase<HaggisGameState, HaggisAction, GameCommand>
{
    private readonly IAiMoveStrategy<HaggisGameState, HaggisAction> _aiMoveStrategy;
    private readonly IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand> _moveRuleValidator;

    public HaggisServerGameLoop(
        IAiMoveStrategy<HaggisGameState, HaggisAction> aiMoveStrategy,
        IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand> moveRuleValidator)
    {
        _aiMoveStrategy = aiMoveStrategy;
        _moveRuleValidator = moveRuleValidator;
    }

    public bool TryExecute(string gameId, GameCommand command, out HaggisGameState? state, out HaggisAction appliedMove)
    {
        var result = Execute(gameId, command);
        if (!result.Handled || result.State is null)
        {
            state = default;
            appliedMove = default!;
            return false;
        }

        state = result.State;
        appliedMove = result.AppliedMove;
        return true;
    }

    protected override bool IsStartCommand(GameCommand command) =>
        command.Type.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Start", StringComparison.OrdinalIgnoreCase);

    protected override bool IsNextMoveCommand(GameCommand command) =>
        command.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("NextMove", StringComparison.OrdinalIgnoreCase);

    protected override HaggisGameState CreateInitialState(GameCommand command)
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
        return new HaggisGameState(players);
    }

    protected override IReadOnlyList<HaggisAction> GetLegalMoves(HaggisGameState state) =>
        state.Actions.ToList();

    protected override bool TryResolveMoveFromCommand(HaggisGameState state, GameCommand command, out HaggisAction move)
    {
        move = default!;

        if (command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            var passAction = state.Actions.FirstOrDefault(a => a.IsPass);
            if (passAction is null)
            {
                throw new InvalidOperationException("Pass is not a legal action right now.");
            }

            move = passAction;
            return true;
        }

        if (!command.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) &&
            !command.Type.Equals("NextMove", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TryResolvePlayByActionDescription(state, command.Payload, out move))
        {
            return true;
        }

        if (PayloadHasTrick(command.Payload))
        {
            if (string.IsNullOrWhiteSpace(command.PlayerId))
            {
                return false;
            }

            var player = ResolvePlayer(state, command.PlayerId);
            if (TryResolvePlayByTrick(player, command.Payload, out move))
            {
                return true;
            }
        }

        return false;
    }

    protected override bool ShouldUseAiMove(HaggisGameState state, GameCommand command) => state.CurrentPlayer.IsAI;

    protected override HaggisAction ResolveAiMove(HaggisGameState state, IReadOnlyList<HaggisAction> legalMoves) =>
        _aiMoveStrategy.ChooseMove(state, legalMoves);

    protected override MoveValidationResult ValidateMove(
        HaggisGameState state,
        GameCommand command,
        HaggisAction move,
        IReadOnlyList<HaggisAction> legalMoves) =>
        _moveRuleValidator.Validate(state, command, move, legalMoves);

    protected override void ApplyMove(HaggisGameState state, HaggisAction move) => state.ApplyAction(move);

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

    private static IHaggisPlayer ResolvePlayer(HaggisGameState state, string playerId)
    {
        var player = state.Players.FirstOrDefault(p =>
            p.Name.Equals(playerId, StringComparison.OrdinalIgnoreCase));
        if (player is null)
        {
            throw new InvalidOperationException($"Player '{playerId}' is not part of this Haggis game.");
        }

        return player;
    }

    private static bool TryResolvePlayByActionDescription(
        HaggisGameState state,
        JsonElement payload,
        out HaggisAction move)
    {
        move = default!;
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("action", out var actionElement) ||
            actionElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var actionValue = actionElement.GetString();
        var matchingAction = state.Actions.FirstOrDefault(x =>
            !x.IsPass && x.Desc.Equals(actionValue, StringComparison.Ordinal));
        if (matchingAction is null)
        {
            return false;
        }

        move = matchingAction;
        return true;
    }

    private static bool TryResolvePlayByTrick(IHaggisPlayer player, JsonElement payload, out HaggisAction move)
    {
        move = default!;
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("trick", out var trickElement) ||
            trickElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        move = HaggisAction.FromTrick(trickElement.GetString()!, player);
        return true;
    }

    private static bool PayloadHasTrick(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object &&
        payload.TryGetProperty("trick", out var trickElement) &&
        trickElement.ValueKind == JsonValueKind.String;
}
