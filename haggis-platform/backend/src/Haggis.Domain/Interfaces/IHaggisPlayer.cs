using Haggis.Domain.Model;
using MonteCarlo;
using System;
using System.Collections.Generic;

namespace Haggis.Domain.Interfaces
{

    public interface IHaggisPlayer : ICloneable, IPlayer, IEquatable<IHaggisPlayer>
    {
        string Name { get; set; }
        List<Card> Hand { get; set; }
        List<Card> Discard { get; set; }
        int Score { get; set; }
        Guid GUID { get; }
        bool Finished { get; }
        void RemoveFromHand(List<Card> cards);
        void AddToDiscard(List<Card> cards);
    }
}
