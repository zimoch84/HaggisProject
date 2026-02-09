using Haggis.Interfaces;
using Haggis.Model;
using System.Collections.Generic;

namespace HaggisTests
{
    internal class SetUp
    {
        public IHaggisPlayer Ala;
        public IHaggisPlayer Bolek;
        public IHaggisPlayer Kasia;

        public List<Card> Cards;

        public void SetUpPlayers() {

            Ala = new HaggisPlayer("Ala");
            Bolek = new HaggisPlayer("Bolek");
            Kasia = new HaggisPlayer("Kasia");

        }

        public void MinimalDeck() {

            Cards = new List<Card>();
            Cards.Add(new Card(Haggis.Enums.Rank.TWO, Haggis.Enums.Suit.RED));
            Cards.Add(new Card(Haggis.Enums.Rank.THREE, Haggis.Enums.Suit.RED));
            Cards.Add(new Card(Haggis.Enums.Rank.FOUR, Haggis.Enums.Suit.RED));


            Cards.Add(new Card(Haggis.Enums.Rank.TWO, Haggis.Enums.Suit.GREEN));
            Cards.Add(new Card(Haggis.Enums.Rank.THREE, Haggis.Enums.Suit.GREEN));
            Cards.Add(new Card(Haggis.Enums.Rank.FOUR, Haggis.Enums.Suit.GREEN));
           

            Cards.Add(new Card(Haggis.Enums.Rank.TWO, Haggis.Enums.Suit.ORANGE));
            Cards.Add(new Card(Haggis.Enums.Rank.THREE, Haggis.Enums.Suit.ORANGE));
            Cards.Add(new Card(Haggis.Enums.Rank.FOUR, Haggis.Enums.Suit.ORANGE));
           

            Cards.Add(new Card(Haggis.Enums.Rank.TWO, Haggis.Enums.Suit.YELLOW));
            Cards.Add(new Card(Haggis.Enums.Rank.THREE, Haggis.Enums.Suit.YELLOW));
            Cards.Add(new Card(Haggis.Enums.Rank.FOUR, Haggis.Enums.Suit.YELLOW));

            Cards.Add(new Card(Haggis.Enums.Rank.TWO, Haggis.Enums.Suit.BLACK));
            Cards.Add(new Card(Haggis.Enums.Rank.THREE, Haggis.Enums.Suit.BLACK));
            Cards.Add(new Card(Haggis.Enums.Rank.FOUR, Haggis.Enums.Suit.BLACK));

        }



    }
}
