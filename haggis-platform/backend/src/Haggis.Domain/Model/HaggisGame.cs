using Haggis.Domain.Enums;
using static Haggis.Domain.Enums.Rank;
using System;
using System.Collections.Generic;
using Haggis.Domain.Interfaces;
using System.Linq;

namespace Haggis.Domain.Model
{
    public partial class HaggisGame
    {
        private IDeckDealer DeckDealer { get; }
        private List<IHaggisPlayer> Players { get; }
        private int Seed { get; set; }
        private int CurrentRoundNumber { get; set; }
        private int WinScore { get; set; }
        public IHaggisScoringStrategy ScoringStrategy { get; private set; }

        public HaggisGame(List<IHaggisPlayer> players, IHaggisScoringStrategy scoringStrategy = null, IDeckDealer deckDealer = null)
        {
            Players = players;
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            DeckDealer = deckDealer ?? new HaggisDeckDealer();
            WinScore = ScoringStrategy.GameOverScore;
            Seed = Environment.TickCount;
            CurrentRoundNumber = 0;
        }

        public void SetSeed(int seed)
        {
            Seed = seed;
        }

        public void SetWinScore(int score)
        {
            WinScore = score;
        }

        public HaggisGameState NewRound()
        {
            CurrentRoundNumber++;
            var deck = DeckDealer.CreateShuffledDeck(Seed);
            Players.ForEach(player =>
            {
                player.Discard.Clear();
                player.Hand = DeckDealer.DealSetupCards(deck);
            });
            var state = new HaggisGameState(Players, ScoringStrategy, CurrentRoundNumber, moveIteration: 0);
            if (Players.Count > 0)
            {
                var random = new Random(unchecked(Seed + CurrentRoundNumber * 7919));
                var startingPlayer = Players[random.Next(Players.Count)];
                state.SetCurrentPlayer(startingPlayer);
            }

            return state;
        }

        public bool GameOver()
        {
            return Players.Any(p => p.Score >= WinScore);
        }
    }
}
