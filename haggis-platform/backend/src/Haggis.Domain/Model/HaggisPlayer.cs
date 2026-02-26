using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haggis.Domain.Model
{
    public class HaggisPlayer : IHaggisPlayer
    {
        private bool Starts { get; set; }
        protected Guid GuidState { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public List<Card> Hand { get; set; }
        [JsonIgnore]
        public List<Card> Discard { get; set; }
        public int Score { get; set; }

        public string HandDesc => Hand.ToLetters();

        public HaggisPlayer(string name)
        {
            Name = name;
            GuidState = Guid.NewGuid();
            Hand = new List<Card>();
            Discard = new List<Card>();
        }

        public HaggisPlayer(string name, List<Card> hand, List<Card> discard)
        {
            Name = name;
            Hand = hand.DeepCopy().ToList();
            Discard = discard.DeepCopy().ToList();
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
            {
                return;
            }

            Hand.RemoveAll(card => cards.Contains(card));
        }

        public void AddToDiscard(List<Card> cards)
        {
            Discard.AddRange(cards);
        }

        public override string ToString()
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
            var clonedPlayer = new HaggisPlayer(Name, Hand, Discard)
            {
                Starts = Starts,
                GuidState = GuidState,
                Score = Score
            };

            return clonedPlayer;
        }

        public void RemoveFromHand(List<Card> cards)
        {
            Hand.RemoveAll(card => cards.Contains(card));
        }

        public bool Equals(IHaggisPlayer other)
        {
            return GUID.Equals(other.GUID);
        }
    }
}
