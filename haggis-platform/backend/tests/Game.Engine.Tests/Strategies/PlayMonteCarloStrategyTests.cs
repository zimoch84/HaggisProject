using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static Haggis.Domain.Extentions.CardsExtensions;
using static Haggis.Domain.Model.HaggisAction;
using Haggis.AI.Model;
using MonteCarlo;

namespace HaggisTests.Strategies
{
    public class PlayMonteCarloStrategyTests
    {

        public MonteCarloStrategy strategy;
        RoundState GameState;
        AIPlayer Piotr;
        AIPlayer Slawek;
        AIPlayer Robert;

        List<IHaggisPlayer> _players;

        [SetUp]

        public void Setup() {

            strategy = new MonteCarloStrategy(1,1L);

            Piotr = new AIPlayer("Piotr");
            Slawek = new AIPlayer("Sławek");
            Robert = new AIPlayer("Robert");

            Piotr.Hand = Cards("2Y", "3Y");
            Slawek.Hand = Cards("2G", "4G");
            Robert.Hand = Cards("2O", "2B");

            _players = new List<IHaggisPlayer> { Piotr, Slawek, Robert };

            GameState = new RoundState(_players);

        }

        [Test]
        public  void ShouldNotChanteGameStateAfterGettinggAction()
        {
            var currentPlayerName = GameState.CurrentPlayer.Name;
            var nextPlayerName = GameState.NextPlayer.Name;
            var players = GameState.Players.DeepCopy();

            var actions = MonteCarloStrategy.GetTopActions(GameState, 2, 1);

            Assert.That(GameState.CurrentPlayer.Name, Is.EqualTo(currentPlayerName));
            Assert.That(GameState.NextPlayer.Name, Is.EqualTo(nextPlayerName));

            Assert.That(GameState.Players, Is.EqualTo(players));
        }
        [Repeat(10)]
        [TestCase(1000, 1000L)]
        public  void ShouldNotGetExceptionAfterXRuns(int runs, long timeBudget)
        {
            var game = new HaggisGame(_players);

            var gameState = new RoundState(_players);

            var currentPlayerName = gameState.CurrentPlayer.Name;
            var nextPlayerName = gameState.NextPlayer.Name;
            var players = gameState.Players.DeepCopy();

            var actions = MonteCarloStrategy.GetTopActions(gameState, runs, timeBudget);

            Assert.That(gameState.CurrentPlayer.Name, Is.EqualTo(currentPlayerName));
            Assert.That(gameState.NextPlayer.Name, Is.EqualTo(nextPlayerName));

            Assert.That(gameState.Players, Is.EqualTo(players));
        }

        [TestCase(100, 100L)]
        public void ShouldRunTillGameOver(int maxIteration, long timeBudget)
        {
            var game = new HaggisGame(_players);
            var gameState = new RoundState(_players);

            while (!gameState.RoundOver())
            {
                var actions = MonteCarloStrategy.GetTopActions(gameState, maxIteration, timeBudget);
                HaggisAction action = actions.First().Action;
                gameState.ApplyAction(action);
                Trace.WriteLine(action.ToString());
            }
        }

        [Test]
        public void ShouldPublishComputationMetrics()
        {
            var result = default(MonteCarloResult);
            var strategyWithMetrics = new MonteCarloStrategy(10, 100L);
            strategyWithMetrics.OnComputed += computed => result = computed;

            var action = strategyWithMetrics.GetPlayingAction(GameState);

            Assert.That(action, Is.Not.Null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Player.Name, Is.EqualTo(GameState.CurrentPlayer.Name));
            Assert.That(result.BudgetMs, Is.EqualTo(100L));
            Assert.That(result.ElapsedMs, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.LegalActionsCount, Is.GreaterThan(0));
            Assert.That(result.RootChildrenCount, Is.GreaterThan(0));
            Assert.That(result.Workers, Is.GreaterThan(0));
            Assert.That(result.ScheduledRollouts, Is.GreaterThan(0));
            Assert.That(result.CompletedRollouts, Is.GreaterThan(0));
            Assert.That(result.TreeNodeCount, Is.GreaterThan(0));
            Assert.That(result.TreeMaxDepth, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Actions, Is.Not.Empty);
            Assert.That(result.Iterations, Is.EqualTo(result.Actions.Sum(info => info.NumRuns)));
            Assert.That(result.Actions.All(info => info.WinRate >= 0 && info.WinRate <= 1), Is.True);
        }

        [Test]
        public void ShouldPublishRolloutTraceEvents()
        {
            var traceEvents = new List<MctsTraceEvent>();
            var strategyWithTrace = new MonteCarloStrategy(3, 1000L, workers: 2)
            {
                TraceContext = "test-context"
            };
            strategyWithTrace.OnTrace += traceEvent => traceEvents.Add(traceEvent);

            var action = strategyWithTrace.GetPlayingAction(GameState);

            Assert.That(action, Is.Not.Null);
            Assert.That(traceEvents, Has.Some.Matches<MctsTraceEvent>(traceEvent => traceEvent.Type == "expand"));
            Assert.That(traceEvents, Has.Some.Matches<MctsTraceEvent>(traceEvent => traceEvent.Type == "rollout_start"));
            Assert.That(traceEvents, Has.Some.Matches<MctsTraceEvent>(traceEvent => traceEvent.Type == "rollout_step"));
            Assert.That(traceEvents, Has.Some.Matches<MctsTraceEvent>(traceEvent => traceEvent.Type == "rollout_end" && traceEvent.Result.HasValue));
            Assert.That(traceEvents.All(traceEvent => traceEvent.Context == "test-context"), Is.True);
        }

        [Test]
        public void AIPlayerCloneShouldPreserveOpponentRemainingCardsOnFinish()
        {
            var player = new AIPlayer("CloneTarget");
            player.OpponentRemainingCardsOnFinish = -1;

            var cloned = (AIPlayer)player.Clone();

            Assert.That(cloned.OpponentRemainingCardsOnFinish, Is.EqualTo(-1));
            Assert.That(cloned.GUID, Is.EqualTo(player.GUID));
            Assert.That(cloned.Name, Is.EqualTo(player.Name));
        }

        [Test]
        public void ShouldIncludeRunOutPointsInMonteCarloRolloutScoring()
        {
            var scoring = new EveryCardOnePointScoringStrategy(runOutMultiplier: 5);
            var alice = new HaggisPlayer("Alice")
            {
                Hand = new List<Card>(),
                Discard = new List<Card>(),
                OpponentRemainingCardsOnFinish = 1
            };
            var bob = new HaggisPlayer("Bob")
            {
                Hand = Cards("2Y"),
                Discard = Cards("3Y")
            };
            var carol = new HaggisPlayer("Carol")
            {
                Hand = new List<Card>(),
                Discard = new List<Card>()
            };

            var state = new RoundState(new List<IHaggisPlayer> { alice, bob, carol }, scoring);
            var roundResult = new Haggis.Domain.Services.ScoringTableService().BuildRoundScoringResult(state);
            var aliceScore = roundResult.PlayerScores.First(score => score.PlayerName == "Alice");
            var bobScore = roundResult.PlayerScores.First(score => score.PlayerName == "Bob");
            var mctsState = new MonteCarloHaggisState(state);

            Assert.That(state.RoundOver(), Is.True);
            Assert.That(aliceScore.OpponentsRemainingCardsPoints, Is.EqualTo(5));
            Assert.That(aliceScore.RoundPoints, Is.EqualTo(5));
            Assert.That(bobScore.RoundPoints, Is.EqualTo(1));
            Assert.That(mctsState.GetResult(new MonteCarloHaggisPlayer(alice)), Is.EqualTo(1));
        }

        [Test]
        public void ShouldWriteReadableTraceForOneMonteCarloDecision()
        {
            var tracePath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "mcts-one-decision-trace.txt");
            var strategyWithTrace = new MonteCarloStrategy(5, 10000L, workers: 1)
            {
                TraceContext = "one-decision-debug"
            };

            using (var subscriber = new ReadableMctsTraceSubscriber(tracePath))
            {
                strategyWithTrace.OnTrace += subscriber.Handle;

                var action = strategyWithTrace.GetPlayingAction(GameState);

                Assert.That(action, Is.Not.Null);
            }

            var traceText = File.ReadAllText(tracePath);
            TestContext.WriteLine($"MCTS trace written to: {tracePath}");

            Assert.That(File.Exists(tracePath), Is.True);
            Assert.That(traceText, Does.Contain("MCTS TRACE context=one-decision-debug"));
            Assert.That(traceText, Does.Contain("iteration 0"));
            Assert.That(traceText, Does.Contain("rollout:"));
            Assert.That(traceText, Does.Contain("result="));
            Assert.That(traceText, Does.Contain("action="));
        }

        [Test]
        public void ShouldProduceRepeatableRootStats_WithSameStateAndSingleWorker()
        {
            var first = ComputeMetrics(CreateInitialState(), workers: 1);
            var second = ComputeMetrics(CreateInitialState(), workers: 1);

            Assert.That(
                second.Actions.Select(action => $"{action.Action.Desc}:{action.NumRuns}:{action.NumWins}").ToArray(),
                Is.EqualTo(first.Actions.Select(action => $"{action.Action.Desc}:{action.NumRuns}:{action.NumWins}").ToArray()));
            Assert.That(second.CompletedRollouts, Is.EqualTo(first.CompletedRollouts));
            Assert.That(second.ScheduledRollouts, Is.EqualTo(first.ScheduledRollouts));
            Assert.That(second.TreeNodeCount, Is.EqualTo(first.TreeNodeCount));
            Assert.That(second.TreeMaxDepth, Is.EqualTo(first.TreeMaxDepth));
        }

        private static MonteCarloResult ComputeMetrics(RoundState state, int workers)
        {
            var result = default(MonteCarloResult);
            var strategy = new MonteCarloStrategy(10, 10000L, workers);
            strategy.OnComputed += computed => result = computed;
            strategy.GetPlayingAction(state);
            return result;
        }

        private static RoundState CreateInitialState()
        {
            var piotr = new AIPlayer("Piotr") { Hand = Cards("2Y", "3Y") };
            var slawek = new AIPlayer("Sławek") { Hand = Cards("2G", "4G") };
            var robert = new AIPlayer("Robert") { Hand = Cards("2O", "2B") };
            return new RoundState(new List<IHaggisPlayer> { piotr, slawek, robert });
        }

        private sealed class ReadableMctsTraceSubscriber : IDisposable
        {
            private readonly string _path;
            private readonly SortedDictionary<int, List<MctsTraceEvent>> _eventsByIteration =
                new SortedDictionary<int, List<MctsTraceEvent>>();
            private string _context;

            public ReadableMctsTraceSubscriber(string path)
            {
                _path = path;
            }

            public void Handle(MctsTraceEvent traceEvent)
            {
                if (traceEvent == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(_context))
                {
                    _context = traceEvent.Context;
                }

                if (!_eventsByIteration.TryGetValue(traceEvent.Iteration, out var events))
                {
                    events = new List<MctsTraceEvent>();
                    _eventsByIteration.Add(traceEvent.Iteration, events);
                }

                events.Add(traceEvent);
            }

            public void Dispose()
            {
                Flush();
            }

            public void Flush()
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var builder = new StringBuilder();
                builder.AppendLine($"MCTS TRACE context={_context ?? "(none)"}");
                builder.AppendLine();

                foreach (var item in _eventsByIteration)
                {
                    WriteIteration(builder, item.Key, item.Value);
                }

                File.WriteAllText(_path, builder.ToString(), Encoding.UTF8);
            }

            private static void WriteIteration(StringBuilder builder, int iteration, IReadOnlyCollection<MctsTraceEvent> events)
            {
                var worker = events.FirstOrDefault()?.Worker ?? 0;
                builder.AppendLine($"iteration {iteration} worker={worker}");

                foreach (var traceEvent in events.Where(e => e.Type == "select"))
                {
                    builder.AppendLine(
                        $"  select node={traceEvent.NodeId} depth={traceEvent.Depth} action={traceEvent.Action} runs={traceEvent.Runs} wins={FormatDouble(traceEvent.Wins)} uct={FormatDouble(traceEvent.Uct)}");
                }

                foreach (var traceEvent in events.Where(e => e.Type == "expand"))
                {
                    builder.AppendLine(
                        $"  expand node={traceEvent.NodeId} parent={traceEvent.ParentNodeId} depth={traceEvent.Depth} action={traceEvent.Action}");
                }

                var rolloutStart = events.FirstOrDefault(e => e.Type == "rollout_start");
                if (rolloutStart != null)
                {
                    builder.AppendLine(
                        $"  rollout: node={rolloutStart.NodeId} depth={rolloutStart.Depth} startPlayer={rolloutStart.Player} seed={rolloutStart.Seed}");
                }
                else
                {
                    builder.AppendLine("  rollout:");
                }

                foreach (var traceEvent in events.Where(e => e.Type == "rollout_step").OrderBy(e => e.Ply))
                {
                    builder.AppendLine(
                        $"    {traceEvent.Ply}. player={traceEvent.Player} action={traceEvent.Action}");
                }

                var rolloutEnd = events.FirstOrDefault(e => e.Type == "rollout_end");
                if (rolloutEnd != null)
                {
                    builder.AppendLine(
                        $"  result={FormatDouble(rolloutEnd.Result)} plies={rolloutEnd.Plies}");
                }

                builder.AppendLine();
            }

            private static string FormatDouble(double? value)
            {
                return value.HasValue
                    ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
        }

    }
}

