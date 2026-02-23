using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using static Haggis.Domain.Enums.Rank;
using static Haggis.Domain.Enums.Suit;
using System;
using System.Collections.Generic;
using Haggis.Domain.Interfaces;
using System.Linq;

namespace Haggis.Domain.Model
{
    public partial class HaggisGame
    {
        public static int HAND_RUNS_OUT_MULTIPLAYER = 5;
        static Random r;
        private List<Card> _cards { get; }
        private readonly List<IHaggisPlayer> _players;
        private readonly IHaggisScoringStrategy _scoringStrategy;

        private int _winScore = 250;
        public IHaggisScoringStrategy ScoringStrategy => _scoringStrategy;

        public HaggisGame(List<IHaggisPlayer> players, IHaggisScoringStrategy scoringStrategy = null)
        {
            _players = players;
            _scoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            _cards = new List<Card>();
            _cards = AllCards();
            SetSeed(Environment.TickCount);
        }

        public void SetSeed(int seed)
        {
            r = new Random(seed);
            _cards.Shuffle();
        }

        public void SetWinScore(int score)
        {
            _winScore = score;
        }

        public List<Card> AllCards()
        {

            List<Card> allCards = new List<Card>();

            allCards.Add(new Card(TWO, RED));
            allCards.Add(new Card(THREE, RED));
            allCards.Add(new Card(FOUR, RED));
            allCards.Add(new Card(FIVE, RED));
            allCards.Add(new Card(SIX, RED));
            allCards.Add(new Card(SEVEN, RED));
            allCards.Add(new Card(EIGHT, RED));
            allCards.Add(new Card(NINE, RED));
            allCards.Add(new Card(TEN, RED));

            allCards.Add(new Card(TWO, GREEN));
            allCards.Add(new Card(THREE, GREEN));
            allCards.Add(new Card(FOUR, GREEN));
            allCards.Add(new Card(FIVE, GREEN));
            allCards.Add(new Card(SIX, GREEN));
            allCards.Add(new Card(SEVEN, GREEN));
            allCards.Add(new Card(EIGHT, GREEN));
            allCards.Add(new Card(NINE, GREEN));
            allCards.Add(new Card(TEN, GREEN));

            allCards.Add(new Card(TWO, ORANGE));
            allCards.Add(new Card(THREE, ORANGE));
            allCards.Add(new Card(FOUR, ORANGE));
            allCards.Add(new Card(FIVE, ORANGE));
            allCards.Add(new Card(SIX, ORANGE));
            allCards.Add(new Card(SEVEN, ORANGE));
            allCards.Add(new Card(EIGHT, ORANGE));
            allCards.Add(new Card(NINE, ORANGE));
            allCards.Add(new Card(TEN, ORANGE));

            allCards.Add(new Card(TWO, YELLOW));
            allCards.Add(new Card(THREE, YELLOW));
            allCards.Add(new Card(FOUR, YELLOW));
            allCards.Add(new Card(FIVE, YELLOW));
            allCards.Add(new Card(SIX, YELLOW));
            allCards.Add(new Card(SEVEN, YELLOW));
            allCards.Add(new Card(EIGHT, YELLOW));
            allCards.Add(new Card(NINE, YELLOW));
            allCards.Add(new Card(TEN, YELLOW));

            allCards.Add(new Card(TWO, BLACK));
            allCards.Add(new Card(THREE, BLACK));
            allCards.Add(new Card(FOUR, BLACK));
            allCards.Add(new Card(FIVE, BLACK));
            allCards.Add(new Card(SIX, BLACK));
            allCards.Add(new Card(SEVEN, BLACK));
            allCards.Add(new Card(EIGHT, BLACK));
            allCards.Add(new Card(NINE, BLACK));
            allCards.Add(new Card(TEN, BLACK));

            return allCards;
        }

        public List<Card> DealTopXCards(int x)
        {
            var dealCards = _cards.GetRange(0, x);
            _cards.RemoveRange(0, x);
            dealCards.Add(Rank.JACK.ToCard());
            dealCards.Add(Rank.QUEEN.ToCard());
            dealCards.Add(Rank.KING.ToCard());

            return dealCards;
        }

        private List<Card> DealSetupCards()
        {
            var dealCards = _cards.GetRange(0, 14);
            _cards.RemoveRange(0, 14);
            dealCards.Add(Rank.JACK.ToCard());
            dealCards.Add(Rank.QUEEN.ToCard());
            dealCards.Add(Rank.KING.ToCard());
            return dealCards;
        
        }

        public void NewRound()
        {
          _players.ToList().ForEach(p => p.Hand = DealSetupCards());
        }

        public bool GameOver()
        {
            return _players.Any(p => p.Score >= _winScore);
        }
    }
}
