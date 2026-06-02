using Haggis.Domain.Model;
using Haggis.Domain.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonteCarlo
{
    public sealed class MonteCarloMoveGenerationService : TrickGenerationServiceBase
    {
        private IMonteCarloTrickSelectionStrategy TrickSelectionStrategy { get; }
        private IMonteCarloActionSelectionStrategy ActionSelectionStrategy { get; }
        private MctsTimingCollector Timing { get; }

        public MonteCarloMoveGenerationService(
            IMonteCarloActionSelectionStrategy actionSelectionStrategy = null,
            IMonteCarloTrickSelectionStrategy trickSelectionStrategy = null,
            MctsTimingCollector timing = null)
            : base((phase, ticks) => RecordTiming(timing, phase, ticks))
        {
            ActionSelectionStrategy = actionSelectionStrategy ?? new PreferFinalTrickMonteCarloActionsStrategy();
            TrickSelectionStrategy = trickSelectionStrategy ?? new SelectAllMonteCarloTricksStrategy();
            Timing = timing;
        }

        public IList<MonteCarloHaggisAction> GetPossibleActionsForCurrentPlayer(RoundState state)
        {
            if (state.RoundOver())
            {
                return new List<MonteCarloHaggisAction>();
            }

            var lastTrick = state.CurrentTrickPlay.LastNotPassTrick;
            var generatedTricks = lastTrick == null
                ? BuildPossibleOpeningTricks(state.CurrentPlayer)
                : BuildPossibleContinuationTricks(state.CurrentPlayer, lastTrick);

            var trickSelectionStart = Stopwatch.GetTimestamp();
            var selectedTricks = TrickSelectionStrategy.Select(state, generatedTricks, lastTrick == null);
            Timing?.AddMoveGenerationTrickSelection(Stopwatch.GetTimestamp() - trickSelectionStart);

            var actionWrappingStart = Stopwatch.GetTimestamp();
            var generatedActions = selectedTricks
                .Select(trick => MonteCarloHaggisAction.FromTrick(trick, state.CurrentPlayer))
                .ToList();
            Timing?.AddMoveGenerationActionWrapping(Stopwatch.GetTimestamp() - actionWrappingStart);

            var actionSelectionStart = Stopwatch.GetTimestamp();
            var selectedActions = ActionSelectionStrategy.Select(state, generatedActions).ToList();
            Timing?.AddMoveGenerationActionSelection(Stopwatch.GetTimestamp() - actionSelectionStart);

            var hasFinalAction = selectedActions.Any(action => !action.IsPass && action.Trick != null && action.Trick.IsFinal);
            if (state.CurrentTrickPlay.LastAction != null && !hasFinalAction)
            {
                var passAppendStart = Stopwatch.GetTimestamp();
                selectedActions.Add(MonteCarloHaggisAction.Pass(state.CurrentPlayer));
                Timing?.AddMoveGenerationPassAppend(Stopwatch.GetTimestamp() - passAppendStart);
            }

            return selectedActions;
        }

        private static void RecordTiming(MctsTimingCollector timing, TrickGenerationPhase phase, long ticks)
        {
            if (timing == null)
            {
                return;
            }

            switch (phase)
            {
                case TrickGenerationPhase.HandIndex:
                    timing.AddMoveGenerationHandIndex(ticks);
                    break;
                case TrickGenerationPhase.SameCards:
                    timing.AddMoveGenerationSameCards(ticks);
                    break;
                case TrickGenerationPhase.SameCardsWithWilds:
                    timing.AddMoveGenerationSameCardsWithWilds(ticks);
                    break;
                case TrickGenerationPhase.Sequences:
                    timing.AddMoveGenerationSequences(ticks);
                    break;
                case TrickGenerationPhase.Stairs:
                    timing.AddMoveGenerationStairs(ticks);
                    break;
                case TrickGenerationPhase.Bombs:
                    timing.AddMoveGenerationBombs(ticks);
                    break;
                case TrickGenerationPhase.ContinuationFilter:
                    timing.AddMoveGenerationContinuationFilter(ticks);
                    break;
                case TrickGenerationPhase.TrickSelection:
                    timing.AddMoveGenerationTrickSelection(ticks);
                    break;
                case TrickGenerationPhase.ActionWrapping:
                    timing.AddMoveGenerationActionWrapping(ticks);
                    break;
                case TrickGenerationPhase.ActionSelection:
                    timing.AddMoveGenerationActionSelection(ticks);
                    break;
                case TrickGenerationPhase.PassAppend:
                    timing.AddMoveGenerationPassAppend(ticks);
                    break;
            }
        }
    }
}
