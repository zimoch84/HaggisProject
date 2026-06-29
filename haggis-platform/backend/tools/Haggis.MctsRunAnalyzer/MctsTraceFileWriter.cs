using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Haggis.AI.Model;
using MonteCarlo;

namespace Haggis.MctsRunAnalyzer
{
    public static class MctsTraceFileWriter
    {
        public static void Write(
            string path,
            MctsRunAnalyzerOptions options,
            string context,
            string targetPlayer,
            string targetStrategy,
            string chosenAction,
            IEnumerable<string> setupLines,
            HeuristicRankingSnapshot heuristicRanking,
            IReadOnlyList<MctsTraceEvent> events,
            MonteCarloResult result)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder();
            builder.AppendLine("MCTS ANALYZER TRACE");
            builder.AppendLine($"seed={options.Seed} players={options.Players} round={options.RoundNumber} move={options.MoveNumber} iterations={options.Iterations} timeBudgetMs={options.TimeBudgetMs} workers={options.Workers}");
            builder.AppendLine($"strategies ai1={options.Ai1Strategy} ai2={options.Ai2Strategy} ai3={options.Ai3Strategy}");
            builder.AppendLine($"context={context}");
            builder.AppendLine($"target player={targetPlayer} strategy={targetStrategy} action={chosenAction}");

            if (result != null)
            {
                builder.AppendLine(
                    $"metrics iterations={result.Iterations} scheduledRollouts={result.ScheduledRollouts} completedRollouts={result.CompletedRollouts} elapsedMs={result.ElapsedMs} workers={result.Workers}");
                builder.AppendLine($"metrics rootChildren={result.RootChildrenCount} legalActions={result.LegalActionsCount} budgetMs={result.BudgetMs}");

                var actions = result.Actions ?? new List<MonteCarloActionInfo>();
                for (var index = 0; index < actions.Count; index++)
                {
                    var action = actions[index];
                    builder.AppendLine(
                        $"  top {index + 1}: action={action.Action?.Desc} runs={action.NumRuns} wins={FormatDouble(action.NumWins)} winRate={FormatDouble(action.WinRate * 100)}%");
                }
            }

            if (setupLines != null)
            {
                builder.AppendLine();
                builder.AppendLine("SETUP");
                foreach (var line in setupLines)
                {
                    builder.AppendLine($"  {line}");
                }
            }

            if (heuristicRanking?.RankedActions != null && heuristicRanking.RankedActions.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"HEURISTIC TREE RANKING topN={heuristicRanking.TreeTopN}");

                for (var index = 0; index < heuristicRanking.RankedActions.Count; index++)
                {
                    var rankedAction = heuristicRanking.RankedActions[index];
                    var selectedMarker = index < heuristicRanking.TreeTopN ? " [selected-for-tree]" : string.Empty;
                    builder.AppendLine(
                        $"  rank {index + 1}: action={rankedAction.Action?.Desc} weight={rankedAction.Weight}{selectedMarker}");

                    foreach (var breakdown in rankedAction.Breakdown ?? Enumerable.Empty<Haggis.AI.Strategies.HeuristicWeightBreakdown>())
                    {
                        builder.AppendLine($"    {breakdown.StrategyName}={breakdown.Weight}");
                    }
                }
            }

            builder.AppendLine();
            builder.AppendLine("TRACE");

            foreach (var group in events
                .GroupBy(traceEvent => traceEvent.Iteration)
                .OrderBy(group => group.Key))
            {
                var worker = group.FirstOrDefault()?.Worker ?? 0;
                builder.AppendLine($"iteration {group.Key} worker={worker}");

                foreach (var traceEvent in group.Where(e => e.Type == "select"))
                {
                    builder.AppendLine(
                        $"  select node={traceEvent.NodeId} depth={traceEvent.Depth} action={traceEvent.Action} runs={traceEvent.Runs} pending={traceEvent.PendingRuns} effective={traceEvent.EffectiveRuns} wins={FormatDouble(traceEvent.Wins)} perspective={traceEvent.PerspectivePlayer} avgRoot={FormatDouble(traceEvent.AverageForRootPlayer)} avgNode={FormatDouble(traceEvent.AverageForNodePlayer)} avgSelection={FormatDouble(traceEvent.AverageForSelectionPlayer)} exploration={FormatDouble(traceEvent.Exploration)} uct={FormatDouble(traceEvent.UctForSelectionPlayer)} rootAction={traceEvent.RootAction} mode={traceEvent.RootSelectionMode}");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "expand"))
                {
                    builder.AppendLine(
                        $"  expand node={traceEvent.NodeId} parent={traceEvent.ParentNodeId} depth={traceEvent.Depth} action={traceEvent.Action} untriedBefore={traceEvent.UntriedBefore} untriedAfter={traceEvent.UntriedAfter} untriedRemaining={traceEvent.UntriedRemaining} children={traceEvent.ChildCount} actions={traceEvent.ActionCount} seed={traceEvent.Seed} rootAction={traceEvent.RootAction} mode={traceEvent.RootSelectionMode}");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "root_expand"))
                {
                    builder.AppendLine(
                        $"  root-expand action={traceEvent.Action} untriedBefore={traceEvent.UntriedBefore} untriedAfter={traceEvent.UntriedAfter} children={traceEvent.ChildCount} actions={traceEvent.ActionCount} seed={traceEvent.Seed}");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "root_selection_candidate"))
                {
                    builder.AppendLine(
                        $"  root-candidate selected={traceEvent.Selected} action={traceEvent.Action} runs={traceEvent.Runs} pending={traceEvent.PendingRuns} effective={traceEvent.EffectiveRuns} totalRoot={FormatDouble(traceEvent.Wins)} avgRoot={FormatDouble(traceEvent.AverageForRootPlayer)} avgNode={FormatDouble(traceEvent.AverageForNodePlayer)} avgSelection={FormatDouble(traceEvent.AverageForSelectionPlayer)} exploration={FormatDouble(traceEvent.Exploration)} uct={FormatDouble(traceEvent.UctForSelectionPlayer)} perspective={traceEvent.PerspectivePlayer} parentRuns={traceEvent.ParentRuns} parentPending={traceEvent.ParentPendingRuns} parentEffective={traceEvent.ParentEffectiveRuns} children={traceEvent.ChildCount} untried={traceEvent.UntriedRemaining} actions={traceEvent.ActionCount}");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "root_snapshot"))
                {
                    builder.AppendLine(
                        $"  root-snapshot action={traceEvent.Action} runs={traceEvent.Runs} pending={traceEvent.PendingRuns} effective={traceEvent.EffectiveRuns} totalRoot={FormatDouble(traceEvent.Wins)} avgRoot={FormatDouble(traceEvent.AverageForRootPlayer)} avgNode={FormatDouble(traceEvent.AverageForNodePlayer)} exploration={FormatDouble(traceEvent.Exploration)} uctRoot={FormatDouble(traceEvent.UctForSelectionPlayer)} parentPending={traceEvent.ParentPendingRuns} parentEffective={traceEvent.ParentEffectiveRuns} untried={traceEvent.UntriedRemaining} children={traceEvent.ChildCount} actions={traceEvent.ActionCount}");
                }

                var rolloutStart = group.FirstOrDefault(e => e.Type == "rollout_start");
                if (rolloutStart != null)
                {
                    builder.AppendLine(
                        $"  rollout node={rolloutStart.NodeId} depth={rolloutStart.Depth} startPlayer={rolloutStart.Player} seed={rolloutStart.Seed} rootAction={rolloutStart.RootAction} mode={rolloutStart.RootSelectionMode}");
                }
                else
                {
                    builder.AppendLine("  rollout");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "rollout_step").OrderBy(e => e.Ply))
                {
                    builder.AppendLine(
                        $"    {traceEvent.Ply}. player={traceEvent.Player} action={traceEvent.Action}");
                    if (ShouldWriteOpponentRemaining(traceEvent))
                    {
                        builder.AppendLine(
                            $"      opponent-remaining: {FormatScores(traceEvent.OpponentRemainingCardsOnFinish)}");
                    }
                    if (traceEvent.Scores != null && traceEvent.Scores.Count > 0)
                    {
                        builder.AppendLine($"      scores: {FormatScores(traceEvent.Scores)}");
                    }
                }

                var rolloutEnd = group.FirstOrDefault(e => e.Type == "rollout_end");
                if (rolloutEnd != null)
                {
                    builder.AppendLine(
                        $"  result={FormatDouble(rolloutEnd.Result)} plies={rolloutEnd.Plies} rootAction={rolloutEnd.RootAction} mode={rolloutEnd.RootSelectionMode}");
                    if (rolloutEnd.Scores != null && rolloutEnd.Scores.Count > 0)
                    {
                        builder.AppendLine($"  final-scores: {FormatScores(rolloutEnd.Scores)}");
                    }
                }

                foreach (var traceEvent in group.Where(e => e.Type == "backprop").OrderByDescending(e => e.Depth))
                {
                    builder.AppendLine(
                        $"  backprop node={traceEvent.NodeId} parent={traceEvent.ParentNodeId} depth={traceEvent.Depth} player={traceEvent.Player} action={traceEvent.Action} result={FormatDouble(traceEvent.Result)} runs={traceEvent.Runs} pending={traceEvent.PendingRuns} effective={traceEvent.EffectiveRuns} wins={FormatDouble(traceEvent.Wins)} avgNode={FormatDouble(traceEvent.AverageForNodePlayer)} avgRoot={FormatDouble(traceEvent.AverageForRootPlayer)} rootAction={traceEvent.RootAction} mode={traceEvent.RootSelectionMode}");
                }

                builder.AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        private static string FormatDouble(double? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatScores(IDictionary<string, int> scores)
        {
            return string.Join(", ", scores.OrderBy(item => item.Key).Select(item => $"{item.Key}={item.Value}"));
        }

        private static bool ShouldWriteOpponentRemaining(MctsTraceEvent traceEvent)
        {
            return traceEvent != null &&
                   !string.IsNullOrWhiteSpace(traceEvent.Action) &&
                   traceEvent.Action.StartsWith("Final ", StringComparison.Ordinal) &&
                   traceEvent.OpponentRemainingCardsOnFinish != null &&
                   traceEvent.OpponentRemainingCardsOnFinish.Count > 0;
        }
    }
}
