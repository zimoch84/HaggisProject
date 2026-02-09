using System;
using NUnit.Framework;
using Haggis.Model;
using Haggis.Extentions;
using Haggis.Enums;
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
    }
}
