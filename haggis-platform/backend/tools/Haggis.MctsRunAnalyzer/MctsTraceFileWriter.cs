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
            builder.AppendLine($"seed={options.Seed} iterations={options.Iterations} timeBudgetMs={options.TimeBudgetMs} workers={options.Workers}");
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
                        $"  select node={traceEvent.NodeId} depth={traceEvent.Depth} action={traceEvent.Action} runs={traceEvent.Runs} wins={FormatDouble(traceEvent.Wins)} uct={FormatDouble(traceEvent.Uct)}");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "expand"))
                {
                    builder.AppendLine(
                        $"  expand node={traceEvent.NodeId} parent={traceEvent.ParentNodeId} depth={traceEvent.Depth} action={traceEvent.Action} untriedRemaining={traceEvent.UntriedRemaining}");
                }

                var rolloutStart = group.FirstOrDefault(e => e.Type == "rollout_start");
                if (rolloutStart != null)
                {
                    builder.AppendLine(
                        $"  rollout node={rolloutStart.NodeId} depth={rolloutStart.Depth} startPlayer={rolloutStart.Player} seed={rolloutStart.Seed}");
                }
                else
                {
                    builder.AppendLine("  rollout");
                }

                foreach (var traceEvent in group.Where(e => e.Type == "rollout_step").OrderBy(e => e.Ply))
                {
                    builder.AppendLine(
                        $"    {traceEvent.Ply}. player={traceEvent.Player} action={traceEvent.Action}");
                    if (traceEvent.Scores != null && traceEvent.Scores.Count > 0)
                    {
                        builder.AppendLine($"      scores: {FormatScores(traceEvent.Scores)}");
                    }
                }

                var rolloutEnd = group.FirstOrDefault(e => e.Type == "rollout_end");
                if (rolloutEnd != null)
                {
                    builder.AppendLine(
                        $"  result={FormatDouble(rolloutEnd.Result)} plies={rolloutEnd.Plies}");
                    if (rolloutEnd.Scores != null && rolloutEnd.Scores.Count > 0)
                    {
                        builder.AppendLine($"  final-scores: {FormatScores(rolloutEnd.Scores)}");
                    }
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
    }
}
