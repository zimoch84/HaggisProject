using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Haggis.Domain.Services;

namespace MonteCarlo
{
    public sealed class MctsOptions
    {
        public int MaxIterations { get; set; } = int.MaxValue;
        public long TimeBudgetMs { get; set; } = long.MaxValue;
        public int Workers { get; set; } = 1;
        public int Seed { get; set; }
        public Action<MctsTraceEvent> Trace { get; set; }
        public string TraceContext { get; set; }
    }

    public sealed class MctsTraceEvent
    {
        public string Type { get; set; }
        public string Context { get; set; }
        public int Iteration { get; set; }
        public int Worker { get; set; }
        public int? NodeId { get; set; }
        public int? ParentNodeId { get; set; }
        public int? Depth { get; set; }
        public string Action { get; set; }
        public string Player { get; set; }
        public int? Ply { get; set; }
        public int? Plies { get; set; }
        public int? Runs { get; set; }
        public double? Wins { get; set; }
        public double? Uct { get; set; }
        public int? UntriedRemaining { get; set; }
        public int? Seed { get; set; }
        public double? Result { get; set; }
        public IDictionary<string, int> Scores { get; set; }
        public IDictionary<string, int> OpponentRemainingCardsOnFinish { get; set; }
    }

    public sealed class MctsSearchResult<TAction> where TAction : IAction
    {
        public IReadOnlyList<IMctsNode<TAction>> TopActions { get; set; } = Array.Empty<IMctsNode<TAction>>();
        public int ScheduledRollouts { get; set; }
        public int CompletedRollouts { get; set; }
        public int Workers { get; set; }
    }

    public class MonteCarloTreeSearch
    {
        private class Node<TPlayer, TAction> : IMctsNode<TAction> where TPlayer : IPlayer where TAction : IAction
        {
            public Node(
                IState<TPlayer, TAction> state,
                TAction action = default(TAction),
                Node<TPlayer, TAction> parent = null,
                int id = 0)
            {
                this.Parent = parent;
                Player = state.CurrentPlayer;
                State = state;
                Action = action;
                UntriedActions = new HashSet<TAction>(state.Actions);
                Id = id;
                Depth = parent == null ? 0 : parent.Depth + 1;
            }

            [JsonIgnore]
            public Node<TPlayer, TAction> Parent { get; }
            public int Id { get; }
            public int Depth { get; }
            public IList<Node<TPlayer, TAction>> Children { get; } = new List<Node<TPlayer, TAction>>();

            public int NumRuns { get; set; }

            public double NumWins { get; set; }

            private readonly object sync = new object();

            [JsonIgnore]
            public TPlayer Player { get; }
            [JsonIgnore]
            public IState<TPlayer, TAction> State { get; }

            public TAction Action { get; }

            public ISet<TAction> UntriedActions { get; }

            public IList<TAction> Actions => State.Actions;

            private static double c = Math.Sqrt(2);

            public double ExploitationValue => NumRuns == 0 ? 0 : NumWins / NumRuns;

            public double ExplorationValue => CalculateExplorationValue();
            private double CalculateExplorationValue()
            {
                if(Parent?.NumRuns == null) 
                    return 0;
                if (NumRuns > 0)
                {
                    return Math.Sqrt(2 * Math.Log(Parent.NumRuns) / NumRuns);
                }
                else
                {
                    return 99999;
                }
            }

            private double UCT => ExploitationValue + ExplorationValue;

            public Node<TPlayer, TAction> SelectChild()
            {
                return Children.MaxElementBy(e => e.UCT);
            }

            public Node<TPlayer, TAction> AddChild(TAction action, IState<TPlayer, TAction> state, int id)
            {
                var child = new Node<TPlayer, TAction>(state, action, this, id);
                UntriedActions.Remove(action);
                Children.Add(child);

                return child;
            }

            public void BuildTree(Func<int, long, bool> shouldContinue)
            {
                var options = new MctsOptions();
                BuildTree(options, shouldContinue);
            }

            public (int ScheduledRollouts, int CompletedRollouts, int Workers) BuildTree(
                MctsOptions options,
                Func<int, long, bool> shouldContinue)
            {
                options = options ?? new MctsOptions();
                var workers = Math.Max(1, options.Workers);
                var scheduledRollouts = 0;
                var completedRollouts = 0;
                var nextResultToApply = 0;
                var nextNodeId = 1;
                var timer = Stopwatch.StartNew();
                var pending = new List<RolloutWork<TPlayer, TAction>>();
                var cancellation = new CancellationTokenSource();

                while (shouldContinue(scheduledRollouts, timer.ElapsedMilliseconds) || pending.Count > 0)
                {
                    while (pending.Count < workers &&
                           shouldContinue(scheduledRollouts, timer.ElapsedMilliseconds))
                    {
                        pending.Add(ScheduleRollout(
                            scheduledRollouts,
                            options.Seed,
                            scheduledRollouts % workers,
                            options.TraceContext,
                            options.Trace != null,
                            ref nextNodeId,
                            cancellation.Token));
                        scheduledRollouts++;
                    }

                    if (pending.Count == 0)
                    {
                        break;
                    }

                    var remainingBudgetMs = options.TimeBudgetMs == long.MaxValue
                        ? -1
                        : Math.Max(0, options.TimeBudgetMs - timer.ElapsedMilliseconds);
                    
                    var tasks = pending.Select(w => w.Task).ToArray();
                    var completedIndex = Task.WaitAny(
                        tasks,
                        remainingBudgetMs < 0 ? -1 : (int)Math.Min(int.MaxValue, remainingBudgetMs));
                    
                    if (completedIndex == -1)
                    {
                        cancellation.Cancel();
                        break;
                    }

                    var nextWork = pending[completedIndex];
                    var rolloutResult = nextWork.Task.GetAwaiter().GetResult();
                    ApplyRolloutResult(rolloutResult);
                    EmitTrace(options.Trace, rolloutResult.TraceEvents);
                    pending.RemoveAt(completedIndex);
                    completedRollouts++;
                }

                if (pending.Count > 0)
                {
                    cancellation.Cancel();
                }

                return (scheduledRollouts, completedRollouts, workers);
            }

            private RolloutWork<TPlayer, TAction> ScheduleRollout(
                int iteration,
                int seed,
                int workerIndex,
                string traceContext,
                bool traceEnabled,
                ref int nextNodeId,
                CancellationToken cancellationToken)
            {
                var node = this;
                var state = State.Clone();
                var selectionRandom = CreateRandom(seed, iteration, 0);
                var traceEvents = traceEnabled ? new List<MctsTraceEvent>() : null;

                while (!node.UntriedActions.Any() && node.Actions.Any())
                {
                    node = node.SelectChild();
                    AddTrace(traceEvents, new MctsTraceEvent
                    {
                        Type = "select",
                        Context = traceContext,
                        Iteration = iteration,
                        Worker = workerIndex,
                        NodeId = node.Id,
                        Depth = node.Depth,
                        Action = FormatAction(node.Action),
                        Runs = node.NumRuns,
                        Wins = node.NumWins,
                        Uct = node.UCT
                    });
                    state.ApplyAction(node.Action);
                }

                if (node.UntriedActions.Any())
                {
                    var action = node.UntriedActions.RandomChoice(selectionRandom);
                    var parent = node;
                    state.ApplyAction(action);
                    node = node.AddChild(action, state.Clone(), nextNodeId++);
                    AddTrace(traceEvents, new MctsTraceEvent
                    {
                        Type = "expand",
                        Context = traceContext,
                        Iteration = iteration,
                        Worker = workerIndex,
                        ParentNodeId = parent.Id,
                        NodeId = node.Id,
                        Depth = node.Depth,
                        Action = FormatAction(action),
                        UntriedRemaining = parent.UntriedActions.Count
                    });
                }

                var rolloutState = state.Clone();
                var rolloutRandom = CreateRandom(seed, iteration, 1);
                var rootPlayer = Player;
                AddTrace(traceEvents, new MctsTraceEvent
                {
                    Type = "rollout_start",
                    Context = traceContext,
                    Iteration = iteration,
                    Worker = workerIndex,
                    NodeId = node.Id,
                    Depth = node.Depth,
                    Player = FormatPlayer(rolloutState.CurrentPlayer),
                    Seed = CreateSeed(seed, iteration, 1)
                });
                var task = Task.Run(() =>
                {
                    var ply = 0;
                    while (!cancellationToken.IsCancellationRequested && rolloutState.Actions.Any())
                    {
                        var player = rolloutState.CurrentPlayer;
                        var action = rolloutState.Actions.RandomChoice(rolloutRandom);
                        rolloutState.ApplyAction(action);
                        AddTrace(traceEvents, new MctsTraceEvent
                        {
                            Type = "rollout_step",
                            Context = traceContext,
                            Iteration = iteration,
                            Worker = workerIndex,
                            NodeId = node.Id,
                            Ply = ply,
                            Player = FormatPlayer(player),
                            Action = FormatAction(action),
                            Scores = BuildScoreSnapshot(rolloutState),
                            OpponentRemainingCardsOnFinish = BuildOpponentRemainingCardsSnapshot(rolloutState)
                        });
                        ply++;
                    }

                    var result = rolloutState.GetResult(rootPlayer);
                    var finalScores = BuildScoreSnapshot(rolloutState);
                    var finalOpponentRemainingCards = BuildOpponentRemainingCardsSnapshot(rolloutState);
                    AddTrace(traceEvents, new MctsTraceEvent
                    {
                        Type = "rollout_end",
                        Context = traceContext,
                        Iteration = iteration,
                        Worker = workerIndex,
                        NodeId = node.Id,
                        Plies = ply,
                        Result = result,
                        Scores = finalScores,
                        OpponentRemainingCardsOnFinish = finalOpponentRemainingCards
                    });

                    return new RolloutResult<TPlayer, TAction>
                    {
                        Node = node,
                        Result = result,
                        TraceEvents = traceEvents
                    };
                });

                return new RolloutWork<TPlayer, TAction>
                {
                    Iteration = iteration,
                    Task = task
                };
            }

            private static Random CreateRandom(int seed, int iteration, int stream)
            {
                return new Random(CreateSeed(seed, iteration, stream));
            }

            private static int CreateSeed(int seed, int iteration, int stream)
            {
                unchecked
                {
                    var value = seed;
                    value = (value * 397) ^ iteration;
                    value = (value * 397) ^ stream;
                    return value;
                }
            }

            private static void AddTrace(ICollection<MctsTraceEvent> traceEvents, MctsTraceEvent traceEvent)
            {
                if (traceEvents == null)
                {
                    return;
                }

                traceEvents.Add(traceEvent);
            }

            private static void EmitTrace(Action<MctsTraceEvent> trace, IEnumerable<MctsTraceEvent> traceEvents)
            {
                if (trace == null || traceEvents == null)
                {
                    return;
                }

                foreach (var traceEvent in traceEvents)
                {
                    trace(traceEvent);
                }
            }

            private static string FormatAction(TAction action)
            {
                return action == null ? null : action.ToString();
            }

            private static string FormatPlayer(TPlayer player)
            {
                return player == null ? null : player.ToString();
            }

            private static IDictionary<string, int> BuildScoreSnapshot(IState<TPlayer, TAction> rolloutState)
            {
                if (!(rolloutState is MonteCarloHaggisState monteCarloState))
                {
                    return new Dictionary<string, int>();
                }

                return BuildRoundPointsByPlayer(monteCarloState.DomainState);
            }

            private static IDictionary<string, int> BuildOpponentRemainingCardsSnapshot(IState<TPlayer, TAction> rolloutState)
            {
                if (!(rolloutState is MonteCarloHaggisState monteCarloState) || monteCarloState.DomainState == null)
                {
                    return new Dictionary<string, int>();
                }

                return monteCarloState.DomainState.Players.ToDictionary(
                    player => player.Name,
                    player => player.OpponentRemainingCardsOnFinish,
                    StringComparer.OrdinalIgnoreCase);
            }

            private static IDictionary<string, int> BuildRoundPointsByPlayer(Haggis.Domain.Model.RoundState state)
            {
                if (state == null)
                {
                    return new Dictionary<string, int>();
                }

                var points = state.Players.ToDictionary(
                    player => player.Name,
                    _ => 0,
                    StringComparer.OrdinalIgnoreCase);

                var haggisPoints = state.HaggisCards?.Sum(card => state.ScoringStrategy.GetCardPoints(card)) ?? 0;
                var firstFinishedGuid = state.FinishingOrder.FirstOrDefault();

                foreach (var player in state.Players)
                {
                    var tricksPoints = player.Discard.Sum(card => state.ScoringStrategy.GetCardPoints(card));
                    var runOutPoints = player.OpponentRemainingCardsOnFinish < 0
                        ? 0
                        : player.OpponentRemainingCardsOnFinish * state.ScoringStrategy.RunOutMultiplier;
                    var bonusHaggisPoints = firstFinishedGuid == player.GUID ? haggisPoints : 0;

                    points[player.Name] = tricksPoints + runOutPoints + bonusHaggisPoints;
                }

                return points;
            }

            private static void ApplyRolloutResult(RolloutResult<TPlayer, TAction> result)
            {
                var node = result.Node;
                while (node != null)
                {
                    lock (node.sync)
                    {
                        node.NumRuns++;
                        node.NumWins += result.Result;
                    }
                    node = node.Parent;
                }
            }

            public override string ToString()
            {
                return $"{NumWins}/{NumRuns}: ({ExploitationValue}/{ExplorationValue}={UCT}), {Action}";
            }
        }

        private sealed class RolloutWork<TPlayer, TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public int Iteration { get; set; }
            public Task<RolloutResult<TPlayer, TAction>> Task { get; set; }
        }

        private sealed class RolloutResult<TPlayer, TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public Node<TPlayer, TAction> Node { get; set; }
            public double Result { get; set; }
            public IReadOnlyList<MctsTraceEvent> TraceEvents { get; set; }
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, int maxIterations) where TPlayer : IPlayer where TAction : IAction
        {
            return GetTopActions(state, maxIterations, long.MaxValue);
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, long timeBudget) where TPlayer : IPlayer where TAction : IAction
        {
            return GetTopActions(state, int.MaxValue, timeBudget);
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, int maxIterations, long timeBudget) where TPlayer : IPlayer where TAction : IAction
        {
            return Search(state, new MctsOptions
            {
                MaxIterations = maxIterations,
                TimeBudgetMs = timeBudget,
                Workers = 1
            }).TopActions;
        }

        public static MctsSearchResult<TAction> Search<TPlayer, TAction>(
            IState<TPlayer, TAction> state,
            MctsOptions options)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            options = options ?? new MctsOptions();
            var root = new Node<TPlayer, TAction>(state);

            if (root.Actions.Count <= 1)
            {
                if (root.Actions.Count == 1)
                {
                    var singleAction = root.Actions[0];
                    var childState = state.Clone();
                    childState.ApplyAction(singleAction);
                    root.AddChild(singleAction, childState, 1);
                }

                return new MctsSearchResult<TAction>
                {
                    TopActions = root.Children
                        .Cast<IMctsNode<TAction>>()
                        .ToList(),
                    ScheduledRollouts = 0,
                    CompletedRollouts = 0,
                    Workers = 0
                };
            }

            var stats = root.BuildTree(
                options,
                (numIterations, elapsedMs) =>
                    numIterations < options.MaxIterations &&
                    elapsedMs < options.TimeBudgetMs);

            return new MctsSearchResult<TAction>
            {
                TopActions = root.Children
                    .OrderByDescending(n => n.NumRuns)
                    .Cast<IMctsNode<TAction>>()
                    .ToList(),
                ScheduledRollouts = stats.ScheduledRollouts,
                CompletedRollouts = stats.CompletedRollouts,
                Workers = stats.Workers
            };
        }
    }
} 
