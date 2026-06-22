using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests
{
    [TestFixture]
    internal class TrickGenerationSeedSnapshotTests
    {
        private sealed class TestableTrickGenerationService : TrickGenerationServiceBase
        {
            public List<Trick> Opening(IHaggisPlayer player) => BuildPossibleOpeningTricks(player);
        }

        private static readonly Dictionary<int, string[]> HandsBySeed = new()
        {
            [1] = new[] { "9B", "5G", "8O", "2Y", "9Y", "8R", "6B", "10B", "9R", "9O", "8B", "2R", "2G", "7B", "J", "Q", "K" },
            [2] = new[] { "3G", "8O", "2G", "3Y", "6O", "8G", "6B", "7R", "2Y", "7O", "5R", "7Y", "9B", "7G", "J", "Q", "K" },
            [3] = new[] { "7Y", "5O", "9O", "9B", "9Y", "3G", "8O", "8B", "4Y", "7B", "8G", "6Y", "9G", "3Y", "J", "Q", "K" }
        };

        private TestableTrickGenerationService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new TestableTrickGenerationService();
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed1_ShouldMatchSnapshot()
        {
            AssertOpeningCharacteristics(
                1,
                "TRIPLE[2R|2G|2Y]",
                "TRIPLE[8R|8B|8O]",
                "QUAD[9R|9B|9Y|9O]",
                "SEQ3[8B|9B|10B]",
                "SINGLE[J]");
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed2_ShouldMatchSnapshot()
        {
            AssertOpeningCharacteristics(
                2,
                "PAIR[2G|2Y]",
                "PAIR[3G|3Y]",
                "QUAD[7R|7G|7Y|7O]",
                "SEQ3[6O|7O|8O]",
                "SINGLE[J]");
        }

        [Test]
        public void OpeningTricks_FirstHand_Seed3_ShouldMatchSnapshot()
        {
            AssertOpeningCharacteristics(
                3,
                "PAIR[3G|3Y]",
                "PAIR[7B|7Y]",
                "TRIPLE[8B|8G|8O]",
                "QUAD[9B|9G|9Y|9O]",
                "SEQ3[7B|8B|9B]");
        }

        private void AssertOpeningCharacteristics(int seed, params string[] expectedTricks)
        {
            var player = new HaggisPlayer($"seed-{seed}") { Hand = HandsBySeed[seed].ToCards() };
            var actual = GetSortedOpeningTrickDescriptions(player);

            Assert.That(actual, Is.Not.Empty);
            Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length), $"Seed {seed} should not generate duplicate trick descriptions.");
            Assert.That(actual.Any(description => description.StartsWith("SINGLE[")), Is.True, $"Seed {seed} should include at least one single.");
            Assert.That(actual.Any(description => description.StartsWith("PAIR[")), Is.True, $"Seed {seed} should include at least one pair.");
            Assert.That(actual.Any(description => description.StartsWith("SEQ3[")), Is.True, $"Seed {seed} should include at least one sequence.");
            Assert.That(actual.Any(description => description.Contains("[J[") || description.Contains("[Q[") || description.Contains("[K[")), Is.True, $"Seed {seed} should include at least one wildcard-based trick.");

            foreach (var expected in expectedTricks)
            {
                Assert.That(actual, Does.Contain(expected), $"Seed {seed} is missing expected representative trick '{expected}'.");
            }
        }

        private string[] GetSortedOpeningTrickDescriptions(HaggisPlayer player)
        {
            var tricks = _service.Opening(player);
            tricks.Sort((left, right) =>
            {
                var comparison = left.CompareTo(right);
                return comparison != 0 ? comparison : string.CompareOrdinal(left.ToString(), right.ToString());
            });

            return tricks.Select(trick => trick.ToString()).ToArray();
        }
    }
}
