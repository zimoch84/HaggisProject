using Game.Core.Engine.Loop;
using Haggis.Model;

namespace Haggis.API.Services.Engine.Haggis;

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
