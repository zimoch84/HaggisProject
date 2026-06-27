using Haggis.AI.Strategies;
using Haggis.Domain.Model;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarlo
{
    public sealed class HeuristicMonteCarloRolloutSelectionStrategy : IMonteCarloRolloutSelectionStrategy
    {
        private readonly HeuristicActionRanker _ranker;
        private readonly int _topN;

        public HeuristicMonteCarloRolloutSelectionStrategy(HeuristicOptions heuristicOptions, int topN)
        {
            _ranker = HeuristicActionRanker.CreateDefault(heuristicOptions);
            _topN = topN;
        }

        public IList<MonteCarloHaggisAction> Select(RoundState state, IList<MonteCarloHaggisAction> generatedActions)
        {
            if (generatedActions == null || generatedActions.Count == 0 || _topN <= 0)
            {
                return generatedActions ?? new List<MonteCarloHaggisAction>();
            }

            var ranked = Rank(state, generatedActions);
            if (ranked.Count == 0)
            {
                return generatedActions;
            }

            return ranked.Take(_topN)
                .Select(action => MonteCarloHaggisAction.FromHaggisAction(action.Action))
                .ToList();
        }

        private IReadOnlyList<HeuristicRankedAction> Rank(RoundState state, IList<MonteCarloHaggisAction> generatedActions)
        {
            var actions = generatedActions.Cast<HaggisAction>().ToList();
            return state?.CurrentTrickPlay?.LastAction == null
                ? _ranker.RankOpeningActions(state, actions)
                : _ranker.RankContinuationActions(state, actions);
        }
    }
}
