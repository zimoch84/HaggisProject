using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using NUnit.Framework;
using System.Linq;

namespace HaggisTests
{
    [TestFixture]
    public sealed class HandIndexTests
    {
        [Test]
        public void Build_ShouldPopulateRanksWithAtLeastNNonWild_AndWildCardCount()
        {
            var handIndex = HandIndex.Build(new[] { "3Y", "3R", "3G", "4B", "4O", "J" }.ToCards());

            Assert.That(handIndex.WildCardCount, Is.EqualTo(1));
            Assert.That(handIndex.GetRanksWithAtLeastNNonWild(1), Is.EqualTo(new[] { Rank.THREE, Rank.FOUR }));
            Assert.That(handIndex.GetRanksWithAtLeastNNonWild(2), Is.EqualTo(new[] { Rank.THREE, Rank.FOUR }));
            Assert.That(handIndex.GetRanksWithAtLeastNNonWild(3), Is.EqualTo(new[] { Rank.THREE }));
            Assert.That(handIndex.GetRanksWithAtLeastNNonWild(4), Is.Empty);
        }

        [Test]
        public void Build_ShouldCreateSameRankCombinationsForRequestedSize()
        {
            var handIndex = HandIndex.Build(new[] { "3Y", "3R", "3G" }.ToCards());

            var combinations = handIndex
                .GetSameRankCombinations(Rank.THREE, 2)
                .Select(cards => cards.Select(card => card.ToString()).ToArray())
                .ToArray();

            Assert.That(combinations, Has.Length.EqualTo(3));
            Assert.That(combinations, Has.Some.EqualTo(new[] { "3R", "3Y" }));
            Assert.That(combinations, Has.Some.EqualTo(new[] { "3G", "3Y" }));
            Assert.That(combinations, Has.Some.EqualTo(new[] { "3R", "3G" }));
        }

        [Test]
        public void Build_ShouldNotCreateSameRankCombinationsLargerThanRankCount()
        {
            var handIndex = HandIndex.Build(new[] { "5Y", "5R" }.ToCards());

            Assert.That(handIndex.GetSameRankCombinations(Rank.FIVE, 1), Has.Count.EqualTo(2));
            Assert.That(handIndex.GetSameRankCombinations(Rank.FIVE, 2), Has.Count.EqualTo(1));
            Assert.That(handIndex.GetSameRankCombinations(Rank.FIVE, 3), Is.Empty);
        }
    }
}
