using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static Haggis.Domain.Extentions.CardsExtensions;

namespace HaggisTests
{
    internal class TrickGenerationServiceBaseTests
    {
        private sealed class TestableTrickGenerationService : TrickGenerationServiceBase
        {
            public List<Trick> Opening(IHaggisPlayer player) => BuildPossibleOpeningTricks(player);
            public List<Trick> Continuation(IHaggisPlayer player, Trick lastTrick) => BuildPossibleContinuationTricks(player, lastTrick);
            public List<Trick> All(IHaggisPlayer player, TrickType? lastTrickType) => BuildAllPossibleTricks(player, lastTrickType);
        }

        private TestableTrickGenerationService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new TestableTrickGenerationService();
        }

        [Test]
        public void BuildPossibleOpeningTricks_ShouldIncludeBombs_WhenHandCanFormBomb()
        {
            var player = new HaggisPlayer("P1") { Hand = Cards("3Y", "5O", "7R", "9B") };

            var tricks = _service.Opening(player);

            Assert.That(tricks.Any(t => t.Type == TrickType.BOMB), Is.True);
        }

        [Test]
        public void BuildPossibleContinuationTricks_ShouldReturnOnlyHigherTricks()
        {
            var player = new HaggisPlayer("P1") { Hand = Cards("2G", "4G") };
            var lastTrick = "3Y_SINGLE".ToTrick();

            var tricks = _service.Continuation(player, lastTrick);

            Assert.That(tricks, Is.Not.Empty);
            Assert.That(tricks.All(t => t.CompareTo(lastTrick) > 0), Is.True);
            Assert.That(tricks.Any(t => t.FirstCard().Rank == Rank.FOUR), Is.True);
        }

        [Test]
        public void BuildPossibleContinuationTricks_WithNullLastTrick_ShouldMatchOpening()
        {
            var player = new HaggisPlayer("P1") { Hand = Cards("2G", "4G") };

            var opening = _service.Opening(player);
            var continuationFromNull = _service.Continuation(player, null);

            Assert.That(continuationFromNull, Is.EquivalentTo(opening));
        }

        [Test]
        public void BuildAllPossibleTricks_WithSingleLastType_ShouldReturnSinglesOnly()
        {
            var player = new HaggisPlayer("P1") { Hand = Cards("2G", "2B", "4G") };

            var tricks = _service.All(player, TrickType.SINGLE);

            Assert.That(tricks.All(t => t.Type == TrickType.SINGLE || t.Type == TrickType.BOMB), Is.True);
            Assert.That(tricks.Any(t => t.Type == TrickType.PAIR), Is.False);
        }

        [Test]
        public void PossibleTricksTest_Is_Final()
        {
            var robert = new HaggisPlayer("Robert") { Hand = new List<string> { "3O", "3B" }.ToCards() };

            var tricks = _service.Continuation(robert, "2BO_PAIR".ToTrick());

            Assert.That(tricks.Count, Is.EqualTo(1));
            Assert.That(tricks.Last().Equals("3BO_PAIR".ToTrick()), Is.True);
            Assert.That(tricks.Last().IsFinal, Is.False);

            tricks = _service.Continuation(robert, null);

            Assert.That(tricks.Count, Is.EqualTo(3));
            Assert.That(tricks.Any(t => t.Equals("3BO_PAIR".ToTrick())), Is.True);
        }

        [TestCase(new string[] { "3Y", "5O", "7Y", "9B" }, 4)]
        [TestCase(new string[] { "3Y", "5O", "7G", "9B" }, 4)]
        [TestCase(new string[] { "3Y", "5O", "7G", "9B", "10O" }, 6)]
        public void CheckIfAllPossibleTricksAreCorrect(string[] cards, int expectedTricks)
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = cards.ToCards() };

            var possibleTricks = _service.Continuation(piotr, null);
            Assert.That(possibleTricks.Count, Is.EqualTo(expectedTricks));
        }

        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7Y", "9B" }, 4)]
        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7G", "9B" }, 4)]
        [TestCase("2O_SINGLE", new string[] { "3Y", "5O", "7G", "9B", "10O" }, 6)]
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7Y", "9B" }, 2)]
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7G", "9B" }, 2)]
        [TestCase("5O_SINGLE", new string[] { "3Y", "5O", "7G", "9B", "10O" }, 4)]
        public void CheckIfAllPossibleTricksAreCorrectIfLastTrickWas(string lastTrick, string[] cards, int expectedTricks)
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = cards.ToCards() };

            var possibleTricks = _service.Continuation(piotr, lastTrick.ToTrick());
            Assert.That(possibleTricks.Count, Is.EqualTo(expectedTricks));
        }

        [Test]
        public void CheckIfSEQ3IsNotPossibleWhenOnlyWilds()
        {
            var piotr = new HaggisPlayer("Piotr") { Hand = new List<string> { "J", "Q", "K" }.ToCards() };

            var tricks = _service.Opening(piotr);

            foreach (var trick in tricks)
            {
                Assert.That(trick.Type, Is.Not.EqualTo(TrickType.SEQ3));
            }
        }

        [Test]
        public void ShouldSuggestWildedTrickWhenHaveOneCardLessToPlayingTrick()
        {
            var slawek = new HaggisPlayer("Sławek") { Hand = new List<string> { "2G", "4G", "J" }.ToCards() };

            var suggestedTricks = _service.Continuation(slawek, "3GO_PAIR".ToTrick());

            Assert.That(suggestedTricks.Count, Is.EqualTo(1));
        }
    }
}
