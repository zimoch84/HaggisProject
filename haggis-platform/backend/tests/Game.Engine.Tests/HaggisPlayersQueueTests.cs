using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System;
using System.Collections.Generic;
using NUnit.Framework;


namespace HaggisTests
{
    [TestFixture]
    public class HaggisPlayerQueueTests
    {
        HaggisPlayerQueue queue;
        HaggisPlayer Piotr;
        HaggisPlayer Slawek;
        HaggisPlayer Robert;

        [SetUp]
        public void SetUp()
        {

            Piotr = new HaggisPlayer("Piotr");
            Slawek = new HaggisPlayer("Slawek");
            Robert = new HaggisPlayer("Robert");
            queue = new HaggisPlayerQueue(new List<IHaggisPlayer> { Piotr, Slawek, Robert});

            Assert.That(queue.Count, Is.EqualTo(3));
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Piotr.GUID));
            Assert.That(queue.GetNextPlayer(), Is.EqualTo(Slawek.GUID));
        }

        [Test]
        public void RotatePlayersClockwise_WithEmptyQueue_ThrowsException()
        {
            queue = new HaggisPlayerQueue(new List<IHaggisPlayer>());
            Assert.Throws<InvalidOperationException>(() => queue.RotatePlayersClockwise());
        }

        [Test]
        public void RotatePlayersClockwise_WithPlayers_RotatesCorrectly()
        {
           queue.RotatePlayersClockwise();
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Slawek.GUID));

            queue.RotatePlayersClockwise();
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Robert.GUID));

            queue.RotatePlayersClockwise();
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Piotr.GUID));
        }

        [Test]
        public void GetCurrentPlayer_WithEmptyQueue_ThrowsException()
        {
            var queue = new HaggisPlayerQueue(new List<IHaggisPlayer>());
            Assert.Throws<InvalidOperationException>(() => queue.GetCurrentPlayer());
        }

        [Test]
        public void GetCurrentPlayer_WithPlayers_ReturnsCurrentPlayer()
        {
            var currentPlayer = queue.GetCurrentPlayer();
            Assert.That(currentPlayer, Is.EqualTo(Piotr.GUID));
        }

        [Test]
        public void AddToQueue_AddsPlayerToQueue()
        {
            var player1 = new HaggisPlayer("Player1");
            var player2 = new HaggisPlayer("Player2");
            var queue = new HaggisPlayerQueue(new List<IHaggisPlayer> { player1 });
            queue.AddToQueue(player2);
            Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(player1.GUID));
        }

        [Test]
        public void RemoveFromQueue_RemovesPlayerFromQueueWhenCurrent()
        {
            queue.RemoveFromQueue(Piotr);
            Assert.That(queue.Count, Is.EqualTo(2));
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Slawek.GUID));
            Assert.That(queue.GetNextPlayer(), Is.EqualTo(Robert.GUID));
        }
        
        [Test]
        public void RemoveFromQueue_RemovesPlayerFromQueue()
        {
            queue.RemoveFromQueue(Slawek);
            Assert.That(queue.GetCurrentPlayer(), Is.EqualTo(Piotr.GUID));
            Assert.That(queue.GetNextPlayer(), Is.EqualTo(Robert.GUID));
        }

        [Test]
        public void Clone_ShouldNotChangeAfterClone() {

            var piotrGuid = Piotr.GUID;
            var plawekGuid = Slawek.GUID;
            var pobertGuid = Robert.GUID;

            var currentPlayer = queue.GetCurrentPlayer();
            var nextPlayer = queue.GetNextPlayer();

            var newQueue = (HaggisPlayerQueue)queue.Clone();
            
            Assert.That(newQueue, Is.Not.EqualTo(queue));
            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(currentPlayer));    
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(nextPlayer));

            newQueue.RemoveFromQueue(Piotr);
            /*Old queue doesnt change */
            Assert.That(currentPlayer, Is.EqualTo(queue.GetCurrentPlayer()));
            Assert.That(nextPlayer, Is.EqualTo(queue.GetNextPlayer()));

            newQueue = null;

            Assert.That(currentPlayer, Is.EqualTo(queue.GetCurrentPlayer()));
            Assert.That(nextPlayer, Is.EqualTo(queue.GetNextPlayer()));
        }
        
        [Test]
        public void Clone_ShouldNewQueueRotate() {

            var newQueue = (HaggisPlayerQueue)queue.Clone();
            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Piotr.GUID));    
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(Slawek.GUID));

            newQueue.RotatePlayersClockwise();

            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Slawek.GUID));
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(Robert.GUID));
            
            newQueue.RotatePlayersClockwise();
            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Robert.GUID));
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(Piotr.GUID));
        }
        
        [Test]
        public void Clone_ShouldNewQueueBeAbleToRemoveFrom() {

            var newQueue = (HaggisPlayerQueue)queue.Clone();
            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Piotr.GUID));    
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(Slawek.GUID));

            newQueue.RemoveFromQueue(Piotr);

            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Slawek.GUID));
            Assert.That(newQueue.GetNextPlayer(), Is.EqualTo(Robert.GUID));

            newQueue.RemoveFromQueue(Slawek);

            Assert.That(newQueue.GetCurrentPlayer(), Is.EqualTo(Robert.GUID));

        }
     }
}