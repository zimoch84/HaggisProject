using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
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
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.TWO, Haggis.Domain.Enums.Suit.RED));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.THREE, Haggis.Domain.Enums.Suit.RED));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.FOUR, Haggis.Domain.Enums.Suit.RED));


            Cards.Add(new Card(Haggis.Domain.Enums.Rank.TWO, Haggis.Domain.Enums.Suit.GREEN));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.THREE, Haggis.Domain.Enums.Suit.GREEN));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.FOUR, Haggis.Domain.Enums.Suit.GREEN));
           

            Cards.Add(new Card(Haggis.Domain.Enums.Rank.TWO, Haggis.Domain.Enums.Suit.ORANGE));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.THREE, Haggis.Domain.Enums.Suit.ORANGE));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.FOUR, Haggis.Domain.Enums.Suit.ORANGE));
           

            Cards.Add(new Card(Haggis.Domain.Enums.Rank.TWO, Haggis.Domain.Enums.Suit.YELLOW));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.THREE, Haggis.Domain.Enums.Suit.YELLOW));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.FOUR, Haggis.Domain.Enums.Suit.YELLOW));

            Cards.Add(new Card(Haggis.Domain.Enums.Rank.TWO, Haggis.Domain.Enums.Suit.BLACK));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.THREE, Haggis.Domain.Enums.Suit.BLACK));
            Cards.Add(new Card(Haggis.Domain.Enums.Rank.FOUR, Haggis.Domain.Enums.Suit.BLACK));

        }



    }
}
