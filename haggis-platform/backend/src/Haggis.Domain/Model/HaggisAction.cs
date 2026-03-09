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
        public string Desc => GetDesc();
        [JsonIgnore]
        public Trick Trick => _trick;
        public bool IsFinal => IsPass ? false : _trick.IsFinal;

        public static HaggisAction FromTrick(Trick trick, IHaggisPlayer player)
        {
            var haggisAction = new HaggisAction(trick, player);
            return haggisAction;
        }

        public static HaggisAction FromTrick(string trick, IHaggisPlayer player)
        {
            var haggisAction = new HaggisAction(trick.ToTrick(), player);
            return haggisAction;
        }

        public static HaggisAction Pass(IHaggisPlayer player)
        {
            var haggisAction = new HaggisAction(null, player);
            return haggisAction;
        }

        protected HaggisAction(Trick trick, IHaggisPlayer player)
        {
            if (trick == null)
            {
                IsPass = true;
            }
            else
            {
                _trick = trick.Clone() as Trick;
                IsPass = false;
            }
            _player = player;
            _desc = GetDesc();
        }

        private string GetDesc()
        {
            return string.Format("{0}", IsPass ? "Pass" : Trick?.ToString());
        }

        public override string ToString()
        {
            return _desc;
        }
        public object Clone()
        {
            var haggisAction = new HaggisAction(_trick?.Clone() as Trick, _player.Clone() as IHaggisPlayer);
            return haggisAction;
        }
        public bool Equals(HaggisAction other)
        {
            if (IsPass && other.IsPass)
                return PlayerName == other?.PlayerName;

            if (IsPass && !other.IsPass) return false;

            if (!IsPass && other.IsPass) return false;

            return Trick.Equals(other?.Trick) &&
                PlayerName == other?.PlayerName;
        }
        public override int GetHashCode()
        {
            int hash = 17;

            hash = hash * 23 + (IsPass ? 1 : 0); 
            hash = hash * 23 + (PlayerName != null ? PlayerName.GetHashCode() : 0); 

            if (!IsPass) 
            {
                hash = hash * 23 + (Trick != null ? Trick.GetHashCode() : 0);
            }

            return hash;
        }
    }
}
