using Haggis.Domain.Extentions;
using Haggis.Domain.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.Strategies;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace HaggisTests
{
    internal class PickOnlyXLeastValuebleStrategyTests
    {
        [Test]
        public static void ShouldFilterTenTricks() { 
        
            
            List<Trick> tricks = new List<Trick>();

            tricks.Add("2G_SINGLE".ToTrick()); 
            tricks.Add("6G_SINGLE".ToTrick());
            tricks.Add("7G_SINGLE".ToTrick());
            tricks.Add("3G_SINGLE".ToTrick());
            tricks.Add("4G_SINGLE".ToTrick());
            tricks.Add("4BYO_TRIPLE".ToTrick());
            tricks.Add("5BYOG_QUAD".ToTrick());
            tricks.Add("6BYOG_QUAD".ToTrick());
            tricks.Add("5G_SINGLE".ToTrick());
            tricks.Add("8G_SINGLE".ToTrick());

            tricks.Add("8BYOG_QUAD".ToTrick()); //<--ShouldBeRemoved

            var strategy = new FilterXLeastValuebleStrategy(10);

            var filteredTricks =strategy.FilterTricks(tricks);

            Assert.That(filteredTricks.Count(), Is.EqualTo(10));
            Assert.That(filteredTricks.ToArray()[0], Is.EqualTo("2G_SINGLE".ToTrick()));  
            Assert.That(filteredTricks.ToArray()[9], Is.EqualTo("6BYOG_QUAD".ToTrick()));  
        }
    }
}
