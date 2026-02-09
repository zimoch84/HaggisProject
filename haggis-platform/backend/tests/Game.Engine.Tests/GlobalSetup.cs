using Haggis.Extentions;
using Haggis.Interfaces;
using Haggis.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace HaggisTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        IHaggisPlayer Piotr = new HaggisPlayer("Piotr");
        IHaggisPlayer Slawek = new HaggisPlayer("Sławek");
        IHaggisPlayer Robert = new HaggisPlayer("Robert");

        HaggisGameState GameState;

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {

            Piotr.Hand = new List<string> { "2Y", "3Y" }.ToCards();
            Slawek.Hand = new List<string> { "2G", "4G" }.ToCards();
            Robert.Hand = new List<string> { "3O", "3B" }.ToCards();
            GameState = new HaggisGameState(new List<IHaggisPlayer> { Piotr, Slawek, Robert });
        }

        [OneTimeTearDown]
        public void RunAfterAllTests()
        {
            // Tutaj umieść kod, który chcesz wykonać po zakończeniu wszystkich testów
            // Na przykład zwolnienie zasobów współdzielonych przez wszystkie testy
        }
    }
}
