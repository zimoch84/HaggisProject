using NUnit.Framework;
using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using Haggis.Domain.Interfaces;
using System.Collections.Generic;

namespace HaggisTests
{
    [TestFixture()]
    public class HaggisPlayerTests
    {
        IHaggisPlayer Piotr = new HaggisPlayer("Piotr");

        [SetUp]
        public void SetUp()
        {
            Piotr.Hand = new List<string> { "2Y", "3Y" }.ToCards();
        }

        [Test]
        public void CloneTest()
        {
            var piotrClone = (IHaggisPlayer)Piotr.Clone();

            Assert.That(piotrClone != Piotr, Is.True);
            Assert.That(piotrClone.Name.Equals(Piotr.Name), Is.True);
        }
    }
}
