using System;
using System.Collections.Generic;
using Haggis.Domain.Extentions;
using static Haggis.Domain.Enums.Rank;
using static Haggis.Domain.Enums.Suit;

namespace Haggis.Domain.Model
{
    public sealed class HaggisDeckDealer
    {
        private readonly List<Card> _haggisCards;

        public HaggisDeckDealer(int seed)
        {
            _haggisCards = CreateAllCards();
            var random = new Random(seed);

            for (var n = _haggisCards.Count - 1; n > 0; --n)
            {
                var k = random.Next(n + 1);
                (_haggisCards[n], _haggisCards[k]) = (_haggisCards[k], _haggisCards[n]);
            }
        }

        public List<Card> DealSetupCards()
        {
            if (_haggisCards.Count < 14)
            {
                throw new InvalidOperationException("Not enough cards in deck to deal setup cards.");
            }

            var dealt = _haggisCards.GetRange(0, 14);
            _haggisCards.RemoveRange(0, 14);
            dealt.Add(JACK.ToCard());
            dealt.Add(QUEEN.ToCard());
            dealt.Add(KING.ToCard());
            return dealt;
        }

        public IReadOnlyList<Card> GetHaggisCards()
        {
            return _haggisCards.AsReadOnly();
        }

        private static List<Card> CreateAllCards()
        {
            return new List<Card>
            {
                new Card(TWO, RED), new Card(THREE, RED), new Card(FOUR, RED), new Card(FIVE, RED), new Card(SIX, RED), new Card(SEVEN, RED), new Card(EIGHT, RED), new Card(NINE, RED), new Card(TEN, RED),
                new Card(TWO, GREEN), new Card(THREE, GREEN), new Card(FOUR, GREEN), new Card(FIVE, GREEN), new Card(SIX, GREEN), new Card(SEVEN, GREEN), new Card(EIGHT, GREEN), new Card(NINE, GREEN), new Card(TEN, GREEN),
                new Card(TWO, ORANGE), new Card(THREE, ORANGE), new Card(FOUR, ORANGE), new Card(FIVE, ORANGE), new Card(SIX, ORANGE), new Card(SEVEN, ORANGE), new Card(EIGHT, ORANGE), new Card(NINE, ORANGE), new Card(TEN, ORANGE),
                new Card(TWO, YELLOW), new Card(THREE, YELLOW), new Card(FOUR, YELLOW), new Card(FIVE, YELLOW), new Card(SIX, YELLOW), new Card(SEVEN, YELLOW), new Card(EIGHT, YELLOW), new Card(NINE, YELLOW), new Card(TEN, YELLOW),
                new Card(TWO, BLACK), new Card(THREE, BLACK), new Card(FOUR, BLACK), new Card(FIVE, BLACK), new Card(SIX, BLACK), new Card(SEVEN, BLACK), new Card(EIGHT, BLACK), new Card(NINE, BLACK), new Card(TEN, BLACK)
            };
        }
    }
}
