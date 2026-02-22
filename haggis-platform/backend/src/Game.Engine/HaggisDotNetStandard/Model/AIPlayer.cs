using Haggis.Interfaces;
using Haggis.StartingTrickFilterStrategies;
using Haggis.Strategies;
using System;
using System.Collections.Generic;

namespace Haggis.Model
{
    public class AIPlayer : HaggisPlayer, ICloneable
    {
        public IPlayStrategy PlayStrategy { get; set; }
        public IStartingTrickFilterStrategy StartingTrickFilterStrategy { get; set; }

        public AIPlayer(string name, List<Card> hand, List<Card> discard) : base(name, hand, discard)
        {
            StartingTrickFilterStrategy = new FilterNoneStrategy();
            PlayStrategy = new MonteCarloStrategy(1000, 1000);
            this.IsAI = true;
        }

        public AIPlayer(string name) : base(name)
        {
            StartingTrickFilterStrategy = new FilterNoneStrategy();
            PlayStrategy = new MonteCarloStrategy(1000, 1000);
            this.IsAI = true;
        }

        public override List<Trick> SuggestedTricks(Trick lastTrick) { 
            
            var  tricks  = base.SuggestedTricks(lastTrick);
            if (lastTrick != null)
                return tricks;  
                
            var filteredTricks = StartingTrickFilterStrategy.FilterTricks(tricks);
            return filteredTricks;
        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState) { 
        
              return PlayStrategy.GetPlayingAction(gameState);
        }
        public object Clone()
        {
            var clonedBase = (HaggisPlayer)base.Clone();

            var aiPlayer =  new AIPlayer(clonedBase.Name, clonedBase.Hand, clonedBase.Discard)
            {
                Score = clonedBase.Score,
                _guid = clonedBase.GUID,
                PlayStrategy = this.PlayStrategy,
                StartingTrickFilterStrategy = this.StartingTrickFilterStrategy
            };
            return aiPlayer;
        }

    }
}
