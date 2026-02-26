using Haggis.AI.Interfaces;
using Haggis.AI.Strategies;
using Haggis.Domain.Model;
using Haggis.Domain.Services;
using System;
using System.Collections.Generic;

namespace Haggis.AI.Model
{
    public class AIPlayer : HaggisPlayer, ICloneable
    {
        private static MoveGenerationService MoveGenerationService { get; } = new MoveGenerationService();
        public IPlayStrategy PlayStrategy { get; set; }

        public AIPlayer(string name, List<Card> hand, List<Card> discard, IPlayStrategy playStrategy = null)
            : base(name, hand, discard)
        {
            InitializeStrategy(playStrategy);
        }

        public AIPlayer(string name, IPlayStrategy playStrategy = null)
            : base(name)
        {
            InitializeStrategy(playStrategy);
        }

        public HaggisAction GetPlayingAction(HaggisGameState gameState)
        {
            return PlayStrategy.GetPlayingAction(gameState);
        }

        public List<Trick> SuggestedTricks(Trick lastTrick)
        {
            return MoveGenerationService.GetPossibleContinuationTricks(this, lastTrick);
        }

        public object Clone()
        {
            var clonedBase = (HaggisPlayer)base.Clone();

            var aiPlayer = new AIPlayer(clonedBase.Name, clonedBase.Hand, clonedBase.Discard, PlayStrategy)
            {
                Score = clonedBase.Score,
                GuidState = clonedBase.GUID
            };
            return aiPlayer;
        }

        private void InitializeStrategy(IPlayStrategy playStrategy)
        {
            PlayStrategy = playStrategy ?? new MonteCarloStrategy(1000, 1000);
        }
    }
}

