using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.AI.Interfaces;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests;

[TestFixture]
public class RoundLifecycleUnitMatrixTests
{
    [TestCaseSource(nameof(UnitCases))]
    public void RoundSimulation_ShouldFinishAndKeepConsistentState(int seed, bool useAiPlayer, bool useEveryCardScoring, bool useMonteCarloAi)
    {
        var scoring = useEveryCardScoring
            ? (IHaggisScoringStrategy)new EveryCardOnePointScoringStrategy(runOutMultiplier: 2, gameOverScore: 120)
            : new ClassicHaggisScoringStrategy(runOutMultiplier: 2, gameOverScore: 120);

        var players = CreatePlayers(useAiPlayer, useMonteCarloAi);
        var game = new HaggisGame(players, scoring);
        game.SetSeed(seed);

        var state = game.NewRound();
        var guard = 0;
        while (!state.RoundOver() && guard++ < 500)
        {
            var action = state.PossibleActions.First();
            state.ApplyAction(action);
        }

        Assert.That(state.RoundOver(), Is.True);
        Assert.That(guard, Is.LessThan(500));
        Assert.That(state.Players.Count(player => !player.Finished), Is.EqualTo(1));
        Assert.That(state.Players.All(player => player.Score >= 0), Is.True);
        Assert.That(state.RoundNumber, Is.EqualTo(1));
    }

    public static IEnumerable<TestCaseData> UnitCases()
    {
        for (var seed = 1; seed <= 80; seed++)
        {
            yield return new TestCaseData(seed, false, false, false).SetName($"Unit_Seed{seed}_Humans_Classic");
            yield return new TestCaseData(seed, true, false, false).SetName($"Unit_Seed{seed}_HeuristicAi_Classic");
            yield return new TestCaseData(seed, true, true, false).SetName($"Unit_Seed{seed}_HeuristicAi_EveryCard");
            yield return new TestCaseData(seed, true, true, true).SetName($"Unit_Seed{seed}_MonteCarloAi_EveryCard");
        }
    }

    private static List<IHaggisPlayer> CreatePlayers(bool useAiPlayer, bool useMonteCarloAi)
    {
        if (!useAiPlayer)
        {
            return new List<IHaggisPlayer>
            {
                new HaggisPlayer("p1"),
                new HaggisPlayer("p2"),
                new HaggisPlayer("p3")
            };
        }

        var aiStrategy = useMonteCarloAi
            ? (IPlayStrategy)new MonteCarloStrategy(20, 1)
            : new HeuristicPlayStrategy();

        return new List<IHaggisPlayer>
        {
            new HaggisPlayer("p1"),
            new AIPlayer("p2", aiStrategy),
            new HaggisPlayer("p3")
        };
    }
}
