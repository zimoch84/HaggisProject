using Haggis.Application.Engine.Loop;
using Haggis.Domain.Model;
using Haggis.AI.Model;

namespace Haggis.Infrastructure.Services.Engine.Haggis;

public sealed class HaggisAiMoveStrategy : IAiMoveStrategy<HaggisGameState, HaggisAction>
{
    public HaggisAction ChooseMove(HaggisGameState state, IReadOnlyList<HaggisAction> legalMoves)
    {
        if (state.CurrentPlayer is AIPlayer aiPlayer)
        {
            return aiPlayer.GetPlayingAction(state);
        }

        return legalMoves[0];
    }
}

