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
        public int? UntriedBefore { get; set; }
        public int? UntriedAfter { get; set; }
        public int? ActionCount { get; set; }
        public int? ChildCount { get; set; }
        public int? ParentRuns { get; set; }
        public int? PendingRuns { get; set; }
        public int? EffectiveRuns { get; set; }
        public int? ParentPendingRuns { get; set; }
        public int? ParentEffectiveRuns { get; set; }
        public int? Seed { get; set; }
        public double? Result { get; set; }
        public double? AverageForRootPlayer { get; set; }
        public double? AverageForNodePlayer { get; set; }
        public double? AverageForSelectionPlayer { get; set; }
        public double? UctForSelectionPlayer { get; set; }
        public double? Exploration { get; set; }
        public string PerspectivePlayer { get; set; }
        public string RootAction { get; set; }
        public string RootSelectionMode { get; set; }
        public bool? Selected { get; set; }
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
            private int pendingRuns;

            private readonly Dictionary<string, double> totalResultByPlayer = new Dictionary<string, double>(StringComparer.Ordinal);

            public double NumWins => GetTotalResult(Player);

            private readonly object sync = new object();

            [JsonIgnore]
            public TPlayer Player { get; }

            [JsonIgnore]
            public IState<TPlayer, TAction> State { get; }

            public TAction Action { get; }

            public ISet<TAction> UntriedActions { get; }

            public IList<TAction> CachedActions { get; }

            public IList<TAction> Actions => CachedActions;

            public double ExploitationValue => GetAverageResult(Player);

            public double ExplorationValue => CalculateExplorationValue();

            private double CalculateExplorationValue()
            {
                var snapshot = GetPendingSnapshot();
                if (Parent == null)
                {
                    return 0;
                }

                var parentSnapshot = Parent.GetPendingSnapshot();
                if (snapshot.EffectiveRuns > 0 && parentSnapshot.EffectiveRuns > 0)
                {
                    return Math.Sqrt(2 * Math.Log(parentSnapshot.EffectiveRuns) / snapshot.EffectiveRuns);
                }

                return 99999;
            }

            public int PendingRuns
            {
                get
                {
                    lock (sync)
                    {
                        return pendingRuns;
                    }
                }
            }

            public double GetAverageResult(TPlayer player)
            {
                if (NumRuns == 0)
                {
                    return 0;
                }

                return GetTotalResult(player) / NumRuns;
            }

            public double GetUCT(TPlayer perspectivePlayer)
            {
                return GetAverageResult(perspectivePlayer) + ExplorationValue;
            }

            public Node<TPlayer, TAction> SelectChild()
            {
                var perspectivePlayer = State.CurrentPlayer;
                return Children.MaxElementBy(child => child.GetUCT(perspectivePlayer));
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
                        if (!rolloutResult.Cancelled)
                        {
                            completedRollouts++;
                        }

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

                    while (resultQueue.TryTake(out var pendingResult))
                    {
                        completedResults[pendingResult.Iteration] = pendingResult;
                        if (!pendingResult.Cancelled)
                        {
                            completedRollouts++;
                        }
                    }

                    foreach (var remainingResult in completedResults
                                 .OrderBy(item => item.Key)
                                 .Select(item => item.Value)
                                 .ToList())
                    {
                        var backpropStart = Stopwatch.GetTimestamp();
                        ApplyRolloutResult(remainingResult);
                        options.Timing?.AddBackpropagation(Stopwatch.GetTimestamp() - backpropStart);
                        EmitTrace(options.Trace, remainingResult.TraceEvents);
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
                var rootPlayer = Player;
                string rootAction = null;
                var rootSelectionMode = "Unknown";

                while (node.UntriedActions.Count == 0)
                {
                    if (node.Actions.Count == 0)
                    {
                        break;
                    }

                    var selectionStart = Stopwatch.GetTimestamp();
                    var selectingFromRoot = node.Parent == null;
                    if (selectingFromRoot)
                    {
                        AddRootSelectionCandidateTrace(
                            traceEvents,
                            traceContext,
                            iteration,
                            workerIndex,
                            node,
                            state.CurrentPlayer,
                            rootPlayer);
                    }

                    var selectedNode = node.SelectChild();
                    node = selectedNode;
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
                        Uct = node.GetUCT(state.CurrentPlayer),
                        PerspectivePlayer = FormatPlayer(state.CurrentPlayer),
                        AverageForRootPlayer = node.GetAverageResult(rootPlayer),
                        AverageForNodePlayer = node.GetAverageResult(node.Player),
                        AverageForSelectionPlayer = node.GetAverageResult(state.CurrentPlayer),
                        UctForSelectionPlayer = node.GetUCT(state.CurrentPlayer),
                        Exploration = node.ExplorationValue,
                        ParentRuns = node.Parent?.NumRuns,
                        PendingRuns = node.PendingRuns,
                        EffectiveRuns = node.GetEffectiveRuns(),
                        ParentPendingRuns = node.Parent?.PendingRuns,
                        ParentEffectiveRuns = node.Parent?.GetEffectiveRuns(),
                        ChildCount = node.Parent?.Children.Count,
                        UntriedRemaining = node.Parent?.UntriedActions.Count,
                        ActionCount = node.Parent?.Actions.Count,
                        RootAction = selectingFromRoot ? FormatAction(node.Action) : rootAction,
                        RootSelectionMode = selectingFromRoot ? "SelectUCT" : rootSelectionMode
                    });
                    if (selectingFromRoot)
                    {
                        rootAction = FormatAction(node.Action);
                        rootSelectionMode = "SelectUCT";
                    }
                    state.ApplyAction(node.Action);
                    timing?.AddSelection(Stopwatch.GetTimestamp() - selectionStart);
                }

                if (node.UntriedActions.Count > 0)
                {
                    var expansionStart = Stopwatch.GetTimestamp();
                    var expandingFromRoot = node.Parent == null;
                    var untriedBefore = node.UntriedActions.Count;
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
                        UntriedRemaining = parent.UntriedActions.Count,
                        UntriedBefore = untriedBefore,
                        UntriedAfter = parent.UntriedActions.Count,
                        ChildCount = parent.Children.Count,
                        ActionCount = parent.Actions.Count,
                        Seed = CreateSeed(seed, iteration, 0),
                        RootAction = expandingFromRoot ? FormatAction(action) : rootAction,
                        RootSelectionMode = expandingFromRoot ? "ExpandUntried" : rootSelectionMode
                    });
                    if (expandingFromRoot)
                    {
                        AddTrace(traceEvents, new MctsTraceEvent
                        {
                            Type = "root_expand",
                            Context = traceContext,
                            Iteration = iteration,
                            Worker = workerIndex,
                            NodeId = parent.Id,
                            ParentNodeId = parent.Parent?.Id,
                            Depth = parent.Depth,
                            Action = FormatAction(action),
                            UntriedBefore = untriedBefore,
                            UntriedAfter = parent.UntriedActions.Count,
                            ChildCount = parent.Children.Count,
                            ActionCount = parent.Actions.Count,
                            Seed = CreateSeed(seed, iteration, 0),
                            RootAction = FormatAction(action),
                            RootSelectionMode = "ExpandUntried"
                        });
                        rootAction = FormatAction(action);
                        rootSelectionMode = "ExpandUntried";
                    }
                    timing?.AddExpansion(Stopwatch.GetTimestamp() - expansionStart);
                }

                var rolloutCloneStart = Stopwatch.GetTimestamp();
                var rolloutState = state.Clone();
                timing?.AddCloneState(Stopwatch.GetTimestamp() - rolloutCloneStart);
                var rolloutRandom = CreateRandom(seed, iteration, 1);
                if (rootAction == null && node.Parent == null && node.Action != null)
                {
                    rootAction = FormatAction(node.Action);
                    rootSelectionMode = "SingleChild";
                }

                AddTrace(traceEvents, new MctsTraceEvent
                {
                    Type = "rollout_start",
                    Context = traceContext,
                    Iteration = iteration,
                    Worker = workerIndex,
                    NodeId = node.Id,
                    Depth = node.Depth,
                    Player = FormatPlayer(rolloutState.CurrentPlayer),
                    Seed = CreateSeed(seed, iteration, 1),
                    RootAction = rootAction,
                    RootSelectionMode = rootSelectionMode
                });

                AddPendingRunToPath(node);

                return new RolloutJob<TPlayer, TAction>
                {
                    Iteration = iteration,
                    WorkerIndex = workerIndex,
                    Node = node,
                    RootPlayer = rootPlayer,
                    Players = GetPlayersFromState(State),
                    RolloutState = rolloutState,
                    RolloutRandom = rolloutRandom,
                    TraceContext = traceContext,
                    TraceEvents = traceEvents,
                    Timing = timing,
                    RootAction = rootAction,
                    RootSelectionMode = rootSelectionMode,
                    HasPendingReservation = true
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
                    var result = cancellationToken.IsCancellationRequested
                        ? CreateCancelledResult(job, workerIndex)
                        : ExecuteRollout(job, workerIndex, cancellationToken);
                    if (result != null)
                    {
                        resultQueue.Add(result);
                    }
                }
            }

            private static RolloutResult<TPlayer, TAction> CreateCancelledResult(
                RolloutJob<TPlayer, TAction> job,
                int workerIndex)
            {
                return new RolloutResult<TPlayer, TAction>
                {
                    Node = job.Node,
                    Iteration = job.Iteration,
                    WorkerIndex = workerIndex,
                    ResultsByPlayer = new Dictionary<string, double>(StringComparer.Ordinal),
                    TraceContext = job.TraceContext,
                    TraceEvents = job.TraceEvents,
                    RootPlayer = job.RootPlayer,
                    RootAction = job.RootAction,
                    RootSelectionMode = job.RootSelectionMode,
                    HasPendingReservation = job.HasPendingReservation,
                    Cancelled = true
                };
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
                    return CreateCancelledResult(job, workerIndex);
                }

                var resultsByPlayer = job.Players
                    .GroupBy(GetPlayerKey)
                    .ToDictionary(
                        group => group.Key,
                        group => job.RolloutState.GetResult(group.First()),
                        StringComparer.Ordinal);
                double rootResult;
                if (!resultsByPlayer.TryGetValue(GetPlayerKey(job.RootPlayer), out rootResult))
                {
                    rootResult = 0;
                }
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
                    Result = rootResult,
                    RootAction = job.RootAction,
                    RootSelectionMode = job.RootSelectionMode,
                    Scores = finalScores,
                    OpponentRemainingCardsOnFinish = finalOpponentRemainingCards
                });

                return new RolloutResult<TPlayer, TAction>
                {
                    Node = job.Node,
                    Iteration = job.Iteration,
                    WorkerIndex = job.WorkerIndex,
                    ResultsByPlayer = resultsByPlayer,
                    TraceContext = job.TraceContext,
                    TraceEvents = job.TraceEvents,
                    RootPlayer = job.RootPlayer,
                    RootAction = job.RootAction,
                    RootSelectionMode = job.RootSelectionMode,
                    HasPendingReservation = job.HasPendingReservation
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
                if (result.HasPendingReservation)
                {
                    RemovePendingRunFromPath(result.Node);
                }

                if (result.Cancelled)
                {
                    return;
                }

                var node = result.Node;
                while (node != null)
                {
                    var nodePlayerKey = GetPlayerKey(node.Player);
                    double nodePlayerTotal;
                    double nodePlayerResult;

                    lock (node.sync)
                    {
                        node.NumRuns++;

                        foreach (var item in result.ResultsByPlayer)
                        {
                            double currentTotal;
                            node.totalResultByPlayer.TryGetValue(item.Key, out currentTotal);
                            node.totalResultByPlayer[item.Key] = currentTotal + item.Value;
                        }

                        node.totalResultByPlayer.TryGetValue(nodePlayerKey, out nodePlayerTotal);
                        result.ResultsByPlayer.TryGetValue(nodePlayerKey, out nodePlayerResult);
                    }
                    AddTrace(result.TraceEvents as ICollection<MctsTraceEvent>, new MctsTraceEvent
                    {
                        Type = "backprop",
                        Context = result.TraceContext,
                        Iteration = result.Iteration,
                        Worker = result.WorkerIndex,
                        NodeId = node.Id,
                        ParentNodeId = node.Parent?.Id,
                        Depth = node.Depth,
                        Action = FormatAction(node.Action),
                        Player = FormatPlayer(node.Player),
                        Runs = node.NumRuns,
                        Wins = nodePlayerTotal,
                        Result = nodePlayerResult,
                        AverageForNodePlayer = node.GetAverageResult(node.Player),
                        AverageForRootPlayer = node.GetAverageResult(result.RootPlayer),
                        PendingRuns = node.PendingRuns,
                        EffectiveRuns = node.GetEffectiveRuns(),
                        ParentPendingRuns = node.Parent?.PendingRuns,
                        ParentEffectiveRuns = node.Parent?.GetEffectiveRuns(),
                        RootAction = result.RootAction,
                        RootSelectionMode = result.RootSelectionMode
                    });
                    if (node.Parent == null && result.Iteration > 0 && (result.Iteration + 1) % 100 == 0)
                    {
                        AddRootSnapshotTrace(
                            result.TraceEvents as ICollection<MctsTraceEvent>,
                            result.TraceContext,
                            result.Iteration,
                            result.WorkerIndex,
                            node,
                            result.RootPlayer);
                    }
                    node = node.Parent;
                }
            }

            private static void AddPendingRunToPath(Node<TPlayer, TAction> node)
            {
                while (node != null)
                {
                    lock (node.sync)
                    {
                        node.pendingRuns++;
                    }

                    node = node.Parent;
                }
            }

            private static void RemovePendingRunFromPath(Node<TPlayer, TAction> node)
            {
                while (node != null)
                {
                    lock (node.sync)
                    {
                        if (node.pendingRuns > 0)
                        {
                            node.pendingRuns--;
                        }
                    }

                    node = node.Parent;
                }
            }

            private static void AddRootSelectionCandidateTrace(
                ICollection<MctsTraceEvent> traceEvents,
                string traceContext,
                int iteration,
                int workerIndex,
                Node<TPlayer, TAction> root,
                TPlayer selectionPlayer,
                TPlayer rootPlayer)
            {
                if (traceEvents == null || root == null)
                {
                    return;
                }

                var selectedChild = root.Children.Count == 0
                    ? null
                    : root.Children.MaxElementBy(child => child.GetUCT(selectionPlayer));

                foreach (var child in root.Children.OrderByDescending(child => child.GetUCT(selectionPlayer)))
                {
                    AddTrace(traceEvents, new MctsTraceEvent
                    {
                        Type = "root_selection_candidate",
                        Context = traceContext,
                        Iteration = iteration,
                        Worker = workerIndex,
                        NodeId = root.Id,
                        ParentNodeId = root.Parent?.Id,
                        Depth = root.Depth,
                        Action = FormatAction(child.Action),
                        Player = FormatPlayer(child.Player),
                        PerspectivePlayer = FormatPlayer(selectionPlayer),
                        Runs = child.NumRuns,
                        Wins = child.GetTotalResult(rootPlayer),
                        AverageForRootPlayer = child.GetAverageResult(rootPlayer),
                        AverageForNodePlayer = child.GetAverageResult(child.Player),
                        AverageForSelectionPlayer = child.GetAverageResult(selectionPlayer),
                        Uct = child.GetUCT(selectionPlayer),
                        UctForSelectionPlayer = child.GetUCT(selectionPlayer),
                        Exploration = child.ExplorationValue,
                        ParentRuns = root.NumRuns,
                        PendingRuns = child.PendingRuns,
                        EffectiveRuns = child.GetEffectiveRuns(),
                        ParentPendingRuns = root.PendingRuns,
                        ParentEffectiveRuns = root.GetEffectiveRuns(),
                        ChildCount = root.Children.Count,
                        UntriedRemaining = root.UntriedActions.Count,
                        ActionCount = root.Actions.Count,
                        Selected = ReferenceEquals(child, selectedChild),
                        RootAction = FormatAction(child.Action),
                        RootSelectionMode = "SelectUCT"
                    });
                }
            }

            private static void AddRootSnapshotTrace(
                ICollection<MctsTraceEvent> traceEvents,
                string traceContext,
                int iteration,
                int workerIndex,
                Node<TPlayer, TAction> root,
                TPlayer rootPlayer)
            {
                if (traceEvents == null || root == null)
                {
                    return;
                }

                foreach (var child in root.Children.OrderByDescending(child => child.NumRuns))
                {
                    AddTrace(traceEvents, new MctsTraceEvent
                    {
                        Type = "root_snapshot",
                        Context = traceContext,
                        Iteration = iteration,
                        Worker = workerIndex,
                        NodeId = root.Id,
                        ParentNodeId = root.Parent?.Id,
                        Depth = root.Depth,
                        Action = FormatAction(child.Action),
                        Player = FormatPlayer(child.Player),
                        PerspectivePlayer = FormatPlayer(rootPlayer),
                        Runs = child.NumRuns,
                        Wins = child.GetTotalResult(rootPlayer),
                        AverageForRootPlayer = child.GetAverageResult(rootPlayer),
                        AverageForNodePlayer = child.GetAverageResult(child.Player),
                        AverageForSelectionPlayer = child.GetAverageResult(rootPlayer),
                        Uct = child.GetUCT(rootPlayer),
                        UctForSelectionPlayer = child.GetUCT(rootPlayer),
                        Exploration = child.ExplorationValue,
                        ParentRuns = root.NumRuns,
                        PendingRuns = child.PendingRuns,
                        EffectiveRuns = child.GetEffectiveRuns(),
                        ParentPendingRuns = root.PendingRuns,
                        ParentEffectiveRuns = root.GetEffectiveRuns(),
                        ChildCount = root.Children.Count,
                        UntriedRemaining = root.UntriedActions.Count,
                        ActionCount = root.Actions.Count,
                        RootAction = FormatAction(child.Action),
                        RootSelectionMode = "Snapshot"
                    });
                }
            }

            public override string ToString()
            {
                return $"{NumWins}/{NumRuns}: ({ExploitationValue}/{ExplorationValue}), Player={Player}, Action={Action}";
            }

            private double GetTotalResult(TPlayer player)
            {
                var playerKey = GetPlayerKey(player);
                double total;
                return totalResultByPlayer.TryGetValue(playerKey, out total) ? total : 0;
            }

            public int GetEffectiveRuns()
            {
                var snapshot = GetPendingSnapshot();
                return snapshot.EffectiveRuns;
            }

            private PendingSnapshot GetPendingSnapshot()
            {
                lock (sync)
                {
                    return new PendingSnapshot
                    {
                        PendingRuns = pendingRuns,
                        EffectiveRuns = NumRuns + pendingRuns
                    };
                }
            }

            private struct PendingSnapshot
            {
                public int PendingRuns { get; set; }
                public int EffectiveRuns { get; set; }
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
            public IReadOnlyList<TPlayer> Players { get; set; }
            public IState<TPlayer, TAction> RolloutState { get; set; }
            public Random RolloutRandom { get; set; }
            public string TraceContext { get; set; }
            public List<MctsTraceEvent> TraceEvents { get; set; }
            public MctsTimingCollector Timing { get; set; }
            public string RootAction { get; set; }
            public string RootSelectionMode { get; set; }
            public bool HasPendingReservation { get; set; }
        }

        private sealed class RolloutResult<TPlayer, TAction>
            where TPlayer : IPlayer
            where TAction : IAction
        {
            public int Iteration { get; set; }
            public int WorkerIndex { get; set; }
            public Node<TPlayer, TAction> Node { get; set; }
            public IDictionary<string, double> ResultsByPlayer { get; set; }
            public IReadOnlyList<MctsTraceEvent> TraceEvents { get; set; }
            public string TraceContext { get; set; }
            public TPlayer RootPlayer { get; set; }
            public string RootAction { get; set; }
            public string RootSelectionMode { get; set; }
            public bool HasPendingReservation { get; set; }
            public bool Cancelled { get; set; }
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
                    .ThenByDescending(n => n.GetAverageResult(root.Player))
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

        private static IReadOnlyList<TPlayer> GetPlayersFromState<TPlayer, TAction>(IState<TPlayer, TAction> state)
            where TPlayer : IPlayer
            where TAction : IAction
        {
            var playerSetState = state as IPlayerSetState<TPlayer>;
            if (playerSetState?.Players != null)
            {
                return playerSetState.Players.ToList();
            }

            return new List<TPlayer> { state.CurrentPlayer };
        }

        private static string GetPlayerKey<TPlayer>(TPlayer player) where TPlayer : IPlayer
        {
            return player == null ? string.Empty : player.ToString() ?? string.Empty;
        }
    }
}
