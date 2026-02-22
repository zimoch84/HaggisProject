using Game.Core.Engine.Loop;
using Haggis.API.Services.Models;
using Haggis.Model;

namespace Haggis.API.Services.Engine.Haggis;

public sealed class HaggisMoveRuleValidator : IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand>
{
    public MoveValidationResult Validate(
        HaggisGameState state,
        GameCommand command,
        HaggisAction move,
        IReadOnlyList<HaggisAction> legalMoves)
    {
        var playerId = command.PlayerId?.Trim();
        if (!string.IsNullOrWhiteSpace(playerId))
        {
            var player = state.Players.FirstOrDefault(p =>
                p.Name.Equals(playerId, StringComparison.OrdinalIgnoreCase));
            if (player is null)
            {
                return MoveValidationResult.Failure($"Player '{command.PlayerId}' is not part of this Haggis game.");
            }

            if (!state.CurrentPlayer.GUID.Equals(player.GUID))
            {
                return MoveValidationResult.Failure($"It is not '{command.PlayerId}' turn.");
            }
        }
        else if (!state.CurrentPlayer.IsAI)
        {
            return MoveValidationResult.Failure("PlayerId is required for non-AI turn.");
        }

        var isLegal = legalMoves.Any(x => x.Equals(move));
        if (!isLegal)
        {
            return MoveValidationResult.Failure("Provided play action is not legal in current state.");
        }

        return MoveValidationResult.Success();
    }
}
