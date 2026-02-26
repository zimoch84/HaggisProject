using Haggis.Domain.Enums;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using MonteCarlo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Haggis.Domain.Enums.TrickType;

namespace Haggis.Domain.Model
{
    public class HaggisPlayer : IHaggisPlayer
    {
        private bool Starts { get; set; }
        private List<Card> HandState { get; set; }
        private List<Card> DiscardState { get; set; }
        protected Guid GuidState { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public List<Card> Hand { get => HandState; set { HandState = value; HandState.Sort(); } }
        [JsonIgnore]
        public List<Card> Discard { get => DiscardState; set => DiscardState = value; }
        public int Score { get; set; }

        public string HandDesc => Hand.ToLetters();
        public bool IsAI { get; set; }

        public HaggisPlayer(string name)
        {
            Name = name;
            GuidState = Guid.NewGuid();
            HandState = new List<Card>();
            DiscardState = new List<Card>();
            this.IsAI = false;
        }

        public HaggisPlayer(string name, List<Card> hand, List<Card> discard)
        {
            Name = name;
            HandState = hand.DeepCopy().ToList();
            DiscardState = discard.DeepCopy().ToList();
            this.IsAI = false;
         }

        public virtual List<Trick> SuggestedTricks(Trick lastTrick)
        {
            List<Trick> allPossibbleTricks = AllPossibleTricks(lastTrick?.Type);

            /*Filter fricks base on trick value */
            if (lastTrick != null)
            {
                allPossibbleTricks = allPossibbleTricks
                    .Where(trick => trick.CompareTo(lastTrick) > 0)
                    .ToList();
            }
            /*Search if there is final trick and return only him if possible */
            foreach (var t in allPossibbleTricks)
            {
                if (t.Cards.Count() == HandState.Count())
                {
                    t.IsFinal = true;
                    return new List<Trick> { t };
                }
            }

            return allPossibbleTricks;
        }
        public List<Trick> AllPossibleTricks(TrickType? lastTrickType)
        {
            var sameCardTypes = new List<TrickType>() { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
            var sequenceTypes = new List<TrickType>() { SEQ3, SEQ4, SEQ5, SEQ6, SEQ6, SEQ7 };
            var pairedSequenceType  = new List<TrickType>() { PAIRSEQ2 };
            var tricks = new List<Trick>();
           
            if (!lastTrickType.HasValue)
            {
                foreach (var trickType in sameCardTypes)
                {
                    tricks.AddRange(HandState.FindTheSameCards(trickType));
                    tricks.AddRange(HandState.FindTheSameCardsWithWildCards(trickType));
                }

                foreach (var trickType in sequenceTypes)
                {
                    tricks.AddRange(HandState.FindCardSequences(trickType));
                }

                foreach (var trickType in pairedSequenceType)
                {
                    tricks.AddRange(HandState.FindPairedSequences(trickType));
                }

            }
            else
            {
                if (sameCardTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(HandState.FindTheSameCards(lastTrickType.Value));
                    tricks.AddRange(HandState.FindTheSameCardsWithWildCards(lastTrickType.Value));
                }
                if (sequenceTypes.Contains(lastTrickType.Value))
                    tricks.AddRange(HandState.FindCardSequences(lastTrickType.Value));

                if (pairedSequenceType.Contains(lastTrickType.Value))
                    tricks.AddRange(HandState.FindPairedSequences(lastTrickType.Value));
            }
            var bombs = HandState.FindAllPossibleBombs();
            tricks.AddRange(bombs);

            return tricks;
        }
        public Guid GUID => GuidState;
        public bool Finished => !Hand.Any();
        public int CardCount()
        {
            return Hand.Count;
        }

        public void RemoveFromHand(IEnumerable<Card> cards)
        {
            if (cards == null)
                return;
            Hand.RemoveAll(card => cards.Contains(card));
        }
        public void AddToDiscard(List<Card> cards)
        {
            DiscardState.AddRange(cards);
        }

        override
        public string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(Name);
            stringBuilder.Append("\r\n");
            stringBuilder.Append("Hand:");
            stringBuilder.Append(Hand.ToLetters());
            stringBuilder.Append("\r\n");
            stringBuilder.Append("Discard:");
            stringBuilder.Append(Discard.ToLetters());
            return stringBuilder.ToString();
        }

        public object Clone()
        {
            var clonedPlayer = new HaggisPlayer(Name, HandState, DiscardState)
            {
                Starts = this.Starts,
                GuidState = this.GuidState,
                Score = this.Score,
                IsAI = this.IsAI
            };

            return clonedPlayer;
        }

        public void RemoveFromHand(List<Card> cards)
        {
            HandState.RemoveAll(card => cards.Contains(card));
        }

        public bool Equals(IHaggisPlayer other)
        {
            return this.GUID.Equals(other.GUID);
        }
    }
}
