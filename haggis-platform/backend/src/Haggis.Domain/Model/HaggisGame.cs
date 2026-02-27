using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Services;

namespace Haggis.Domain.Model
{
    public partial class HaggisGame
    {
        private ScoringTableService ScoringTableService { get; }
        private HaggisDeckDealer DeckDealer { get; set; }
        private List<IHaggisPlayer> Players { get; }
        private int Seed { get; set; }
        public int CurrentRoundNumber { get; private set; }

        public ScoringTable ScoringTable { get; }
        public IHaggisScoringStrategy ScoringStrategy { get; private set; }
        public RoundScoringResult PreviousRoundResult => ScoringTable.GetLastRoundScore();

        public HaggisGame(
            List<IHaggisPlayer> players,
            IHaggisScoringStrategy scoringStrategy = null,
            ScoringTableService scoringTableService = null)
        {
            Players = (players ?? new List<IHaggisPlayer>()).ToList();
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            ScoringTableService = scoringTableService ?? new ScoringTableService();
            Seed = Environment.TickCount;
            CurrentRoundNumber = 0;
            ScoringTable = new ScoringTable();
        }

        public void SetSeed(int seed)
        {
            Seed = seed;
        }

        public string GetPreviousRoundWinnerName()
        {
            return PreviousRoundResult?.WinnerPlayerName;
        }

        public void RegisterRoundScoringResult(RoundState state)
        {
            if (state is null || !state.RoundOver())
            {
                return;
            }

            ScoringTable.AddRoundScore(ScoringTableService.BuildRoundScoringResult(state));
        }

        public bool GameOver()
        {
            return Players.Any(player => ScoringTable.TotalScore(player) >= ScoringStrategy.GameOverScore);
        }

        public RoundState NewRound()
        {
            CurrentRoundNumber++;
            var roundSeed = unchecked(Seed + CurrentRoundNumber * 7919);
            DeckDealer = new HaggisDeckDealer(roundSeed);
            Players.ForEach(player =>
            {
                player.Discard.Clear();
                player.Score = 0;
                player.OpponentRemainingCardsOnFinish = -1;
                player.Hand = DeckDealer.DealSetupCards();
            });

            var orderedPlayers = ScoringTableService.BuildRoundPlayersOrder(Players, ScoringTable).ToList();
            var haggisCards = DeckDealer.GetHaggisCards().ToList();
            return new RoundState(orderedPlayers, ScoringStrategy, CurrentRoundNumber, moveIteration: 0, haggisCards: haggisCards);
        }
    }
}
