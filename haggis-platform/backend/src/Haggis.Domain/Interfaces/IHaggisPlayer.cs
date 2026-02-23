using Haggis.Domain.Enums;
using Haggis.Domain.Model;
using MonteCarlo;
using System;
using System.Collections.Generic;

namespace Haggis.Domain.Interfaces
{

    public interface IHaggisPlayer : ICloneable, IPlayer, IEquatable<IHaggisPlayer>
    {
        string Name { get; set; }
        List<Card> Hand { get; set;}
        List<Card> Discard { get; set; }
        int Score { get; set; }
        Guid GUID { get; }
        bool Finished { get;}
        bool IsAI { get; set; }

        List<Trick> SuggestedTricks(Trick lastTrick);
        List<Trick> AllPossibleTricks(TrickType? lastTrickType);
        void RemoveFromHand(List<Card> cards);
        void AddToDiscard(List<Card> cards);
        void ScoreForDiscard();
     }
}
