using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using NUnit.Framework;

namespace HaggisTests
{
    [TestFixture]
    internal class TrickPlayHotPathTests
    {
        [Test]
        public void LastNotPassAction_ShouldReturnNull_ForOnlyPasses()
        {
            var piotr = new HaggisPlayer("Piotr");
            var slawek = new HaggisPlayer("Slawek");
            var trickPlay = new TrickPlay(3);

            trickPlay.AddAction(HaggisAction.Pass(piotr));
            trickPlay.AddAction(HaggisAction.Pass(slawek));

            Assert.That(trickPlay.LastNotPassAction, Is.Null);
            Assert.That(trickPlay.LastNotPassTrick, Is.Null);
        }

        [Test]
        public void LastNotPassTrick_ShouldReturnLastPlayedTrick_WhenFollowedByPasses()
        {
            var piotr = new HaggisPlayer("Piotr");
            var slawek = new HaggisPlayer("Slawek");
            var robert = new HaggisPlayer("Robert");
            var trickPlay = new TrickPlay(3);
            var trick = "4BYOG_QUAD".ToTrick();

            trickPlay.AddAction(HaggisAction.FromTrick(trick, piotr));
            trickPlay.AddAction(HaggisAction.Pass(slawek));
            trickPlay.AddAction(HaggisAction.Pass(robert));

            Assert.That(trickPlay.LastNotPassAction, Is.Not.Null);
            Assert.That(trickPlay.LastNotPassTrick, Is.EqualTo(trick));
        }

        [Test]
        public void HasFinalNonPassAction_ShouldDetectFinalAction()
        {
            var piotr = new HaggisPlayer("Piotr");
            var slawek = new HaggisPlayer("Slawek");
            var trickPlay = new TrickPlay(3);
            var finalTrick = "2G_SINGLE".ToTrick();

            trickPlay.AddAction(HaggisAction.Pass(piotr));
            trickPlay.AddAction(HaggisAction.FromTrick(finalTrick, slawek, true));

            Assert.That(trickPlay.HasFinalNonPassAction(), Is.True);
        }
    }
}

