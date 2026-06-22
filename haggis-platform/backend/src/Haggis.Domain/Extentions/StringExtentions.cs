using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;

namespace Haggis.Domain.Extentions
{
    public static class StringExtensions
    {
        /*
         * 2Y = Two.Yellow
         * 10O = Ten.Orange
         * */
        public static Card ToCard(this string cardString)
        {
            if (cardString == null)
                throw new ArgumentException("Null string");
            if (cardString.Length > 3)
                throw new ArgumentException("String too long for a card");

            char[] chars = cardString.ToCharArray();
            Card card;
            Rank rank;
            Suit suit;
            if (chars.Length == 1)
            {
                rank = GetRankFromChar(chars[0]);
                card = new Card(rank);
            }
            else if (chars.Length == 2)
            {
                rank = GetRankFromChar(chars[0]);
                suit = GetSuitFromChar(chars[1]);
                card = new Card(rank, suit);
            }
            /* Ten (10) takes only last 0 */
            else 
            {
                 rank = GetRankFromChar(chars[1]);
                 suit = GetSuitFromChar(chars[2]);
                 card = new Card(rank, suit);
            }
            
            return card;
        }
        /*
         * 
         4BYOG_QUAD = 4B,4Y,4O,4G
         */
        public static Trick ToTrick(this string trickString)
        {
            try
            {
                string trickTypeString = trickString.Split('_')[1]; 
                string cardsString = trickString.Split('_')[0];
                TrickType type = (TrickType)Enum.Parse(typeof(TrickType), trickTypeString);

                List<Card> cards = new List<Card>();

                switch (type)
                {
                    case TrickType.SINGLE:
                    case TrickType.PAIR:
                    case TrickType.TRIPLE:
                    case TrickType.QUAD:
                    case TrickType.FIVED:
                    case TrickType.SIXED:
                        //4BYOG_QUAD, 10RB_PAIR
                        Rank rank = ParseSameRankTrickRank(cardsString, out var suitsStartIndex);
                        int suitCount = (int)type / 10;
                        if (type == TrickType.SINGLE && cardsString.Length == suitsStartIndex)
                        {
                            cards.Add(new Card(rank));
                            break;
                        }

                        if (cardsString.Length < suitsStartIndex + suitCount)
                            throw new ArgumentException($"Invalid trick cards payload: {cardsString}");

                        char[] suits = cardsString.Substring(suitsStartIndex, suitCount).ToCharArray(); // Read suit letters
                        foreach (char suitCh in suits)
                        {
                            cards.Add(new Card(rank, GetSuitFromChar(suitCh)));
                        }
                        break;

                    case TrickType.SEQ3:
                    case TrickType.SEQ4:
                    case TrickType.SEQ5:
                    case TrickType.SEQ6:
                    case TrickType.SEQ7:
                        // Build cards for the sequence
                        // 2Y_SEQ3
                        int sequenceLength = (int)((int)type - 2) / 10;
                        char rankChar = trickString[0];
                        char suitChar = trickString[1]; // Suit letter after rank
                        cards = GenerateSequence(GetRankFromChar(rankChar), sequenceLength, GetSuitFromChar(suitChar));
                        break;

                    default:
                        throw new ArgumentException($"Unsupported TrickType: {type}");
                }

                cards.Sort();

                return new Trick(type, cards);
            }
            catch(Exception e) {
                throw new ArgumentException(e.Message);
            }
        }

        private static Rank ParseSameRankTrickRank(string cardsString, out int suitsStartIndex)
        {
            if (string.IsNullOrWhiteSpace(cardsString))
                throw new ArgumentException("Trick string cannot be empty.");

            if (cardsString.StartsWith("10", StringComparison.Ordinal))
            {
                suitsStartIndex = 2;
                return Rank.TEN;
            }

            suitsStartIndex = 1;
            return GetRankFromChar(cardsString[0]);
        }

       
        private static List<Card> GenerateSequence(Rank firstRank , int numberOfCards, Suit suit)
        {
            var cards = new List<Card>();
            for (int i = (int)firstRank; i < (int)firstRank + numberOfCards; i++)
            {
                cards.Add(new Card((Rank)(i), suit));

            }
            return cards;
        }

        private static Rank GetRankFromChar(char rankChar)
        {
            switch (rankChar)
            {
                case '0': return Rank.TEN;
                case '2': return Rank.TWO;
                case '3': return Rank.THREE;
                case '4': return Rank.FOUR;
                case '5': return Rank.FIVE;
                case '6': return Rank.SIX;
                case '7': return Rank.SEVEN;
                case '8': return Rank.EIGHT;
                case '9': return Rank.NINE;
                case 'J': return Rank.JACK;
                case 'Q': return Rank.QUEEN;
                case 'K': return Rank.KING;
                default: throw new ArgumentException($"Unknown rank letter: {rankChar}");
            }
        }

        private static Suit GetSuitFromChar(char suitChar)
        {
            switch (suitChar)
            {
                case 'R': return Suit.RED;
                case 'B': return Suit.BLACK;
                case 'G': return Suit.GREEN;
                case 'Y': return Suit.YELLOW;
                case 'O': return Suit.ORANGE;
                default: throw new ArgumentException($"Unknown suit letter: {suitChar}");
            }
        }

        /*
         * [2O|3O|4O|6B|6G|7Y|7G|7O|8G|9B|9R|10O|10Y|10B|J|Q|K]
         * */
        public static List<Card> ToCards(this string cardString)
        {
            if (string.IsNullOrEmpty(cardString))
                throw new ArgumentException("Input string cannot be null or empty.");

            // Remove square brackets and split into card tokens
            var cardParts = cardString.Trim('[', ']').Split('|');
            var cards = new List<Card>();

            foreach (var part in cardParts)
            {
                var card = part.ToCard(); // Convert each token to Card
                cards.Add(card);
            }

            return cards;
        }


    }
}
   
