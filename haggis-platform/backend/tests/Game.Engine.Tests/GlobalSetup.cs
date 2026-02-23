using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace HaggisTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        IHaggisPlayer Piotr = new HaggisPlayer("Piotr");
        IHaggisPlayer Slawek = new HaggisPlayer("S³awek");
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
            // Tutaj umieœæ kod, który chcesz wykonaæ po zakoñczeniu wszystkich testów
            // Na przyk³ad zwolnienie zasobów wspó³dzielonych przez wszystkie testy
        }
    }
}
