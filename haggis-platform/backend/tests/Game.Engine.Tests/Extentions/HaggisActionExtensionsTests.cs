using System;
using NUnit.Framework;
using Haggis.Domain.Model;
using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using System.Collections.Generic;

namespace HaggisTests.Extentions
{
    [TestFixture]
    public class HaggisActionExtensionsTests
    {
        [Test]
        public void IsPassAction_ReturnsTrueForPass()
        {
            var player = new HaggisPlayer("P1");
            var pass = HaggisAction.Pass(player);
            Assert.That(pass.IsPassAction(), Is.True);
        }

        [Test]
        public void IsPassAction_ReturnsFalseForPlay()
        {
            var player = new HaggisPlayer("P1");
            var card = "2Y".ToCard();
            var trick = new Trick(TrickType.SINGLE, new List<Card>{ card });
            var action = HaggisAction.FromTrick(trick, player);
            Assert.That(action.IsPassAction(), Is.False);
        }

        [Test]
        public void PlayerGuid_ReturnsGuidForAction()
        {
            var player = new HaggisPlayer("P1");
            var action = HaggisAction.Pass(player);
            Assert.That(action.PlayerGuid(), Is.EqualTo(player.GUID));
        }

        [Test]
        public void Desc_ShouldPrefixFinalActionWithoutChangingTrickText()
        {
            var player = new HaggisPlayer("P1");
            var action = HaggisAction.FromTrick("2Y_SINGLE", player, true);

            Assert.That(action.Trick.ToString(), Is.EqualTo("SINGLE[2Y]"));
            Assert.That(action.Desc, Is.EqualTo("Final SINGLE[2Y]"));
            Assert.That(action.ToString(), Is.EqualTo("Final SINGLE[2Y]"));
        }
    }
}
