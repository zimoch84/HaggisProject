using Haggis.Enums;
using Haggis.Extentions;
using Haggis.Interfaces;
using MonteCarlo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Haggis.Enums.TrickType;

namespace Haggis.Model
{
    public class HaggisPlayer : IHaggisPlayer
    {

        private bool _starts;
        private string _name;
        private List<Card> _hand;
        private List<Card> _discard;
        protected Guid _guid;

        public string Name { get { return _name; } set { _name = value; } }

        [JsonIgnore]
        public List<Card> Hand { get => _hand; set { _hand = value; _hand.Sort(); } }
        [JsonIgnore]
        public List<Card> Discard { get => _discard; set => _discard = value; }
        public int Score { get; set; }

        public string HandDesc => Hand.ToLetters();
        public bool IsAI { get; set; }

        public HaggisPlayer(string name)
        {
            _name = name;
            _guid = Guid.NewGuid();
            _hand = new List<Card>();
            _discard = new List<Card>();
            this.IsAI = false;
        }

        public HaggisPlayer(string name, List<Card> hand, List<Card> discard)
        {
            _name = name;
            _hand = hand.DeepCopy().ToList();
            _discard = discard.DeepCopy().ToList();
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
                if (t.Cards.Count() == _hand.Count())
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
                    tricks.AddRange(_hand.FindTheSameCards(trickType));
                    tricks.AddRange(_hand.FindTheSameCardsWithWildCards(trickType));
                }

                foreach (var trickType in sequenceTypes)
                {
                    tricks.AddRange(_hand.FindCardSequences(trickType));
                }

                foreach (var trickType in pairedSequenceType)
                {
                    tricks.AddRange(_hand.FindPairedSequences(trickType));
                }

            }
            else
            {
                if (sameCardTypes.Contains(lastTrickType.Value))
                {
                    tricks.AddRange(_hand.FindTheSameCards(lastTrickType.Value));
                    tricks.AddRange(_hand.FindTheSameCardsWithWildCards(lastTrickType.Value));
                }
                if (sequenceTypes.Contains(lastTrickType.Value))
                    tricks.AddRange(_hand.FindCardSequences(lastTrickType.Value));

                if (pairedSequenceType.Contains(lastTrickType.Value))
                    tricks.AddRange(_hand.FindPairedSequences(lastTrickType.Value));
            }
            var bombs = _hand.FindAllPossibleBombs();
            tricks.AddRange(bombs);

            return tricks;
        }
        public Guid GUID => _guid;
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
            _discard.AddRange(cards);
        }
        public void ScoreForDiscard()
        {
            _discard.ToList().ForEach(
                card => Score+= card.Point);
         }

        override
        public string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(_name);
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
            var clonedPlayer = new HaggisPlayer(_name, _hand, _discard)
            {
                _starts = _starts,
                _guid = this._guid,
                Score = this.Score,
                IsAI = this.IsAI
            };

            return clonedPlayer;
        }

        public void RemoveFromHand(List<Card> cards)
        {
            _hand.RemoveAll(card => cards.Contains(card));
        }

        public bool Equals(IHaggisPlayer other)
        {
            return this.GUID.Equals(other.GUID);
        }
    }
}