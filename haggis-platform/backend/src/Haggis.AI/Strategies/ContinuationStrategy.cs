using Haggis.AI.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.AI.Strategies
{
    // Backward compatible alias. Prefer ContinuationTrickStrategy or HeuristicPlayStrategy.
    public class ContinuationStrategy : IPlayStrategy
    {
        private ContinuationTrickStrategy ContinuationTrickStrategy { get; }

        public ContinuationStrategy(bool useWildsInContinuations, bool takeLessValueTrickFirst)
        {
            ContinuationTrickStrategy = new ContinuationTrickStrategy(useWildsInContinuations, takeLessValueTrickFirst);
        }

        public HaggisAction GetPlayingAction(RoundState gameState)
        {
            return ContinuationTrickStrategy.GetPlayingAction(gameState) ?? gameState.PossibleActions[0];
        }
    }
}
