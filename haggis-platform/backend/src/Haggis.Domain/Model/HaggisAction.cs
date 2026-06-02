using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Newtonsoft.Json;
using System;

namespace Haggis.Domain.Model
{
    public class HaggisAction : ICloneable, IEquatable<HaggisAction>
    {
        private readonly Trick _trick;
        private readonly IHaggisPlayer _player;
        private string _desc;

        [JsonIgnore]
        public readonly bool IsPass;

        [JsonIgnore]
        public string PlayerName => Player.Name;

        [JsonIgnore]
        public IHaggisPlayer Player => _player;

        public string Desc => _desc ?? (_desc = BuildDesc());

        [JsonIgnore]
        public Trick Trick => _trick;

        [JsonIgnore]
        public bool IsFinal { get; }

        public static HaggisAction FromTrick(Trick trick, IHaggisPlayer player, bool isFinal = false)
        {
            return new HaggisAction(trick, player, isFinal);
        }

        public static HaggisAction FromTrick(string trick, IHaggisPlayer player, bool isFinal = false)
        {
            return new HaggisAction(trick.ToTrick(), player, isFinal);
        }

        public static HaggisAction Pass(IHaggisPlayer player)
        {
            return new HaggisAction(null, player);
        }

        protected HaggisAction(Trick trick, IHaggisPlayer player, bool isFinal = false)
        {
            if (trick == null)
            {
                IsPass = true;
            }
            else
            {
                _trick = trick;
                IsPass = false;
            }

            IsFinal = IsPass ? false : isFinal;
            _player = player;
        }

        private string BuildDesc()
        {
            if (IsPass)
            {
                return "Pass";
            }

            return IsFinal
                ? $"Final {Trick}"
                : Trick?.ToString();
        }

        public override string ToString()
        {
            return Desc;
        }

        public object Clone()
        {
            return new HaggisAction(_trick, _player.Clone() as IHaggisPlayer, IsFinal);
        }

        public bool Equals(HaggisAction other)
        {
            if (other is null)
            {
                return false;
            }

            if (IsPass && other.IsPass)
            {
                return PlayerName == other.PlayerName && IsFinal == other.IsFinal;
            }

            if (IsPass || other.IsPass)
            {
                return false;
            }

            return Trick.Equals(other.Trick) &&
                PlayerName == other.PlayerName &&
                IsFinal == other.IsFinal;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HaggisAction);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (IsPass ? 1 : 0);
                hash = hash * 23 + (IsFinal ? 1 : 0);
                hash = hash * 23 + (PlayerName != null ? PlayerName.GetHashCode() : 0);

                if (!IsPass)
                {
                    hash = hash * 23 + (Trick != null ? Trick.GetHashCode() : 0);
                }

                return hash;
            }
        }
    }
}
