using Haggis.Domain.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public MctsTimingCollector Timing { get; set; }
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
        public int TreeNodeCount { get; set; }
        public int TreeMaxDepth { get; set; }
        public MctsTimingResult Timing { get; set; }
    }

    public class MonteCarloTreeSearch
    {
        private class Node<TPlayer, TAction> : IMctsNode<TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public Node(
                IState<TPlayer, TAction> state,
                MctsTimingCollector timing = null,
                TAction action = default(TAction),
                Node<TPlayer, TAction> parent = null,
                int id = 0)
            {
                Parent = parent;
                Player = state.CurrentPlayer;
                State = state;
                Action = action;

                var actionsStart = Stopwatch.GetTimestamp();
                CachedActions = state.Actions.ToList();
                timing?.AddMoveGenerationTree(Stopwatch.GetTimestamp() - actionsStart);

                UntriedActions = new HashSet<TAction>(CachedActions);
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

            public IList<TAction> CachedActions { get; }

            public IList<TAction> Actions => CachedActions;

            public double ExploitationValue => NumRuns == 0 ? 0 : NumWins / NumRuns;

            public double ExplorationValue => CalculateExplorationValue();

            private double CalculateExplorationValue()
            {
                if (Parent?.NumRuns == null)
                {
                    return 0;
                }

                if (NumRuns > 0)
                {
                    return Math.Sqrt(2 * Math.Log(Parent.NumRuns) / NumRuns);
                }

                return 99999;
            }

            private double UCT => ExploitationValue + ExplorationValue;

            public Node<TPlayer, TAction> SelectChild()
            {
                return Children.MaxElementBy(e => e.UCT);
            }

            public Node<TPlayer, TAction> AddChild(TAction action, IState<TPlayer, TAction> state, int id, MctsTimingCollector timing = null)
            {
                var child = new Node<TPlayer, TAction>(state, timing, action, this, id);
                UntriedActions.Remove(action);
                Children.Add(child);
                return child;
            }

            public void BuildTree(Func<int, long, bool> shouldContinue)
            {
                BuildTree(new MctsOptions(), shouldContinue);
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
                var pendingJobs = 0;
                var completedResults = new Dictionary<int, RolloutResult<TPlayer, TAction>>();
                var cancellation = new CancellationTokenSource();
                var jobQueue = new BlockingCollection<RolloutJob<TPlayer, TAction>>();
                var resultQueue = new BlockingCollection<RolloutResult<TPlayer, TAction>>();
                var workerTasks = StartWorkers(workers, jobQueue, resultQueue, cancellation.Token);

                try
                {
                    while (scheduledRollouts == 0 || shouldContinue(scheduledRollouts, timer.ElapsedMilliseconds) || pendingJobs > 0)
                    {
                        while (pendingJobs < workers &&
                               (scheduledRollouts == 0 || shouldContinue(scheduledRollouts, timer.ElapsedMilliseconds)))
                        {
                            var scheduleStart = Stopwatch.GetTimestamp();
                            var job = PrepareRollout(
                                scheduledRollouts,
                                options.Seed,
                                scheduledRollouts % workers,
                                options.TraceContext,
                                options.Trace != null,
                                options.Timing,
                                ref nextNodeId);

                            jobQueue.Add(job);
                            options.Timing?.AddScheduler(Stopwatch.GetTimestamp() - scheduleStart);
                            scheduledRollouts++;
                            pendingJobs++;
                        }

                        if (pendingJobs == 0)
                        {
                            break;
                        }

                        var remainingBudgetMs = options.TimeBudgetMs == long.MaxValue
                            ? -1
                            : Math.Max(0, options.TimeBudgetMs - timer.ElapsedMilliseconds);

                        var waitStart = Stopwatch.GetTimestamp();
                        var waitTimeout = completedRollouts == 0
                            ? Timeout.Infinite
                            : remainingBudgetMs < 0
                                ? Timeout.Infinite
                                : (int)Math.Min(int.MaxValue, remainingBudgetMs);

                        if (!resultQueue.TryTake(out var rolloutResult, waitTimeout))
                        {
                            cancellation.Cancel();
                            break;
                        }

                        options.Timing?.AddScheduler(Stopwatch.GetTimestamp() - waitStart);

                        completedResults[rolloutResult.Iteration] = rolloutResult;
                        pendingJobs--;
                        completedRollouts++;

                        while (completedResults.TryGetValue(nextResultToApply, out var readyResult))
                        {
                            completedResults.Remove(nextResultToApply);
                            var backpropStart = Stopwatch.GetTimestamp();
                            ApplyRolloutResult(readyResult);
                            options.Timing?.AddBackpropagation(Stopwatch.GetTimestamp() - backpropStart);
                            EmitTrace(options.Trace, readyResult.TraceEvents);
                            nextResultToApply++;
                        }
                    }
                }
                finally
                {
                    cancellation.Cancel();
                    jobQueue.CompleteAdding();

                    try
                    {
                        Task.WaitAll(workerTasks);
                    }
                    catch (AggregateException)
                    {
                    }
                }

                return (scheduledRollouts, completedRollouts, workers);
            }

            private RolloutJob<TPlayer, TAction> PrepareRollout(
                int iteration,
                int seed,
                int workerIndex,
                string traceContext,
                bool traceEnabled,
                MctsTimingCollector timing,
                ref int nextNodeId)
            {
                var node = this;
                var selectionGenerationStart = Stopwatch.GetTimestamp();
                var state = State.Clone();
                timing?.AddCloneState(Stopwatch.GetTimestamp() - selectionGenerationStart);

                var selectionRandom = CreateRandom(seed, iteration, 0);
                var traceEvents = traceEnabled ? new List<MctsTraceEvent>() : null;

                while (node.UntriedActions.Count == 0)
                {
                    if (node.Actions.Count == 0)
                    {
                        break;
                    }

                    var selectionStart = Stopwatch.GetTimestamp();
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
                    timing?.AddSelection(Stopwatch.GetTimestamp() - selectionStart);
                }

                if (node.UntriedActions.Count > 0)
                {
                    var expansionStart = Stopwatch.GetTimestamp();
                    var action = node.UntriedActions.RandomChoice(selectionRandom);
                    var parent = node;
                    state.ApplyAction(action);

                    var childCloneStart = Stopwatch.GetTimestamp();
                    var childState = state.Clone();
                    timing?.AddCloneState(Stopwatch.GetTimestamp() - childCloneStart);
                    node = node.AddChild(action, childState, nextNodeId++, timing);

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
                    timing?.AddExpansion(Stopwatch.GetTimestamp() - expansionStart);
                }

                var rolloutCloneStart = Stopwatch.GetTimestamp();
                var rolloutState = state.Clone();
                timing?.AddCloneState(Stopwatch.GetTimestamp() - rolloutCloneStart);
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

                return new RolloutJob<TPlayer, TAction>
                {
                    Iteration = iteration,
                    WorkerIndex = workerIndex,
                    Node = node,
                    RootPlayer = rootPlayer,
                    RolloutState = rolloutState,
                    RolloutRandom = rolloutRandom,
                    TraceContext = traceContext,
                    TraceEvents = traceEvents,
                    Timing = timing
                };
            }

            private static Task[] StartWorkers(
                int workers,
                BlockingCollection<RolloutJob<TPlayer, TAction>> jobQueue,
                BlockingCollection<RolloutResult<TPlayer, TAction>> resultQueue,
                CancellationToken cancellationToken)
            {
                return Enumerable.Range(0, workers)
                    .Select(workerIndex => Task.Factory.StartNew(
                        () => WorkerLoop(workerIndex, jobQueue, resultQueue, cancellationToken),
                        cancellationToken,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default))
                    .ToArray();
            }

            private static void WorkerLoop(
                int workerIndex,
                BlockingCollection<RolloutJob<TPlayer, TAction>> jobQueue,
                BlockingCollection<RolloutResult<TPlayer, TAction>> resultQueue,
                CancellationToken cancellationToken)
            {
                foreach (var job in jobQueue.GetConsumingEnumerable())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var result = ExecuteRollout(job, workerIndex, cancellationToken);
                    if (result != null)
                    {
                        resultQueue.Add(result);
                    }
                }
            }

            private static RolloutResult<TPlayer, TAction> ExecuteRollout(
                RolloutJob<TPlayer, TAction> job,
                int workerIndex,
                CancellationToken cancellationToken)
            {
                var rolloutStart = Stopwatch.GetTimestamp();
                var ply = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var rolloutActionsStart = Stopwatch.GetTimestamp();
                    IList<TAction> rolloutActions;
                    if (job.RolloutState is MonteCarloHaggisState monteCarloState)
                    {
                        rolloutActions = monteCarloState.GetRolloutActions().Cast<TAction>().ToList();
                    }
                    else
                    {
                        rolloutActions = job.RolloutState.Actions;
                    }
                    job.Timing?.AddMoveGenerationRollout(Stopwatch.GetTimestamp() - rolloutActionsStart);

                    if (rolloutActions.Count == 0)
                    {
                        break;
                    }

                    var player = job.RolloutState.CurrentPlayer;
                    var action = rolloutActions.RandomChoice(job.RolloutRandom);
                    job.RolloutState.ApplyAction(action);
                    AddTrace(job.TraceEvents, new MctsTraceEvent
                    {
                        Type = "rollout_step",
                        Context = job.TraceContext,
                        Iteration = job.Iteration,
                        Worker = job.WorkerIndex,
                        NodeId = job.Node.Id,
                        Ply = ply,
                        Player = FormatPlayer(player),
                        Action = FormatAction(action),
                        Scores = BuildScoreSnapshot(job.RolloutState),
                        OpponentRemainingCardsOnFinish = BuildOpponentRemainingCardsSnapshot(job.RolloutState)
                    });
                    ply++;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var result = job.RolloutState.GetResult(job.RootPlayer);
                job.Timing?.AddRollout(Stopwatch.GetTimestamp() - rolloutStart);
                var finalScores = BuildScoreSnapshot(job.RolloutState);
                var finalOpponentRemainingCards = BuildOpponentRemainingCardsSnapshot(job.RolloutState);
                AddTrace(job.TraceEvents, new MctsTraceEvent
                {
                    Type = "rollout_end",
                    Context = job.TraceContext,
                    Iteration = job.Iteration,
                    Worker = job.WorkerIndex,
                    NodeId = job.Node.Id,
                    Plies = ply,
                    Result = result,
                    Scores = finalScores,
                    OpponentRemainingCardsOnFinish = finalOpponentRemainingCards
                });

                return new RolloutResult<TPlayer, TAction>
                {
                    Node = job.Node,
                    Iteration = job.Iteration,
                    Result = result,
                    TraceEvents = job.TraceEvents
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

        private sealed class RolloutJob<TPlayer, TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public int Iteration { get; set; }
            public int WorkerIndex { get; set; }
            public Node<TPlayer, TAction> Node { get; set; }
            public TPlayer RootPlayer { get; set; }
            public IState<TPlayer, TAction> RolloutState { get; set; }
            public Random RolloutRandom { get; set; }
            public string TraceContext { get; set; }
            public List<MctsTraceEvent> TraceEvents { get; set; }
            public MctsTimingCollector Timing { get; set; }
        }

        private sealed class RolloutResult<TPlayer, TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public int Iteration { get; set; }
            public Node<TPlayer, TAction> Node { get; set; }
            public double Result { get; set; }
            public IReadOnlyList<MctsTraceEvent> TraceEvents { get; set; }
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, int maxIterations)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            return GetTopActions(state, maxIterations, long.MaxValue);
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, long timeBudget)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            return GetTopActions(state, int.MaxValue, timeBudget);
        }

        public static IEnumerable<IMctsNode<TAction>> GetTopActions<TPlayer, TAction>(IState<TPlayer, TAction> state, int maxIterations, long timeBudget)
            where TPlayer : IPlayer
            where TAction : IAction
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
            var searchTimer = Stopwatch.StartNew();
            var root = new Node<TPlayer, TAction>(state, options.Timing);

            if (root.Actions.Count <= 1)
            {
                if (root.Actions.Count == 1)
                {
                    var singleAction = root.Actions[0];
                    var childState = state.Clone();
                    childState.ApplyAction(singleAction);
                    root.AddChild(singleAction, childState, 1, options.Timing);
                }

                return new MctsSearchResult<TAction>
                {
                    TopActions = root.Children
                        .Cast<IMctsNode<TAction>>()
                        .ToList(),
                    ScheduledRollouts = 0,
                    CompletedRollouts = 0,
                    Workers = 0,
                    TreeNodeCount = CountNodes(root),
                    TreeMaxDepth = GetMaxDepth(root),
                    Timing = options.Timing?.Snapshot(searchTimer.ElapsedTicks, 0, 0, 0)
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
                Workers = stats.Workers,
                TreeNodeCount = CountNodes(root),
                TreeMaxDepth = GetMaxDepth(root),
                Timing = options.Timing?.Snapshot(searchTimer.ElapsedTicks, stats.ScheduledRollouts, stats.CompletedRollouts, stats.Workers)
            };
        }

        private static int CountNodes<TPlayer, TAction>(Node<TPlayer, TAction> root)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            var count = 0;
            var stack = new Stack<Node<TPlayer, TAction>>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                count++;

                for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
                {
                    stack.Push(node.Children[childIndex]);
                }
            }

            return count;
        }

        private static int GetMaxDepth<TPlayer, TAction>(Node<TPlayer, TAction> root)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            var maxDepth = 0;
            var stack = new Stack<Node<TPlayer, TAction>>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Depth > maxDepth)
                {
                    maxDepth = node.Depth;
                }

                for (var childIndex = 0; childIndex < node.Children.Count; childIndex++)
                {
                    stack.Push(node.Children[childIndex]);
                }
            }

            return maxDepth;
        }
    }
}
