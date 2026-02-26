using Haggis.Domain.Extentions;
using System.Text;
using Newtonsoft.Json;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Services;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Haggis.Domain.Model
{
    public class HaggisGameState
    {
        private static IActionApplicationService ActionApplicationService { get; } = new ActionApplicationService();
        private static MoveGenerationService MoveGenerationService { get; } = new MoveGenerationService();
        internal HaggisPlayerQueue PlayerQueue { get; set; }

        public List<IHaggisPlayer> Players { get; }
        [JsonIgnore]
        public IHaggisPlayer CurrentPlayer => Players.First(p => p.GUID == PlayerQueue.GetCurrentPlayer());

        [JsonIgnore]
        public IHaggisPlayer NextPlayer => Players.First(p => p.GUID == PlayerQueue.GetNextPlayer());

        [JsonIgnore]
        public TrickPlay CurrentTrickPlay  { get; set; }
        [JsonIgnore]
        internal TrickPlay CurrentTrickPlayState
        {
            get => CurrentTrickPlay;
            set => CurrentTrickPlay = value;
        }
        [JsonIgnore]
        public LinkedList<TrickPlay> ActionArchive;

        [JsonIgnore]
        public IList<HaggisAction> PossibleActions => MoveGenerationService.GetPossibleActionsForCurrentPlayer(this);
        [JsonIgnore]
        public IHaggisScoringStrategy ScoringStrategy { get; private set; }
        [JsonIgnore]
        public int RoundNumber { get; set; }
        [JsonIgnore]
        public long MoveIteration { get; set; }

        private HaggisGameState(
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy,
            int roundNumber,
            long moveIteration)
        {
            Players = new List<IHaggisPlayer>(players);
            PlayerQueue = playerQueue;
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            RoundNumber = roundNumber < 1 ? 1 : roundNumber;
            MoveIteration = moveIteration < 0 ? 0 : moveIteration;
        }

        public HaggisGameState(
            List<IHaggisPlayer> players,
            IHaggisScoringStrategy scoringStrategy = null,
            int roundNumber = 1,
            long moveIteration = 0)
            : this(
                players,
                new HaggisPlayerQueue(players),
                scoringStrategy,
                roundNumber,
                moveIteration)
        {
            ActionArchive = new LinkedList<TrickPlay>();
            CurrentTrickPlay = new TrickPlay(players.Count);
        }

        private HaggisGameState(
            LinkedList<TrickPlay> historyTricks,
            TrickPlay trickPlay,
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy,
            int roundNumber,
            long moveIteration)
            : this(players, playerQueue, scoringStrategy, roundNumber, moveIteration)
        {
            ActionArchive = new LinkedList<TrickPlay>(historyTricks);
            CurrentTrickPlay = trickPlay;
        }

        public bool RoundOver()
        {
            return Players.Count(p => !p.Finished) == 1;
        }

        public void SetCurrentPlayer(IHaggisPlayer player)
        {
            PlayerQueue.SetCurrentPlayer(player);
        }

        public void ApplyAction(HaggisAction action)
        {
            ActionApplicationService.Apply(this, action);
        }

        public HaggisGameState Clone()
        {
            var players = new List<IHaggisPlayer>(Players?.DeepCopy());
            var board = (TrickPlay)CurrentTrickPlay?.Clone();
            var archive = new LinkedList<TrickPlay>(ActionArchive.DeepCopy());
            var playerQueue = (HaggisPlayerQueue)PlayerQueue.Clone();
            return new HaggisGameState(archive, board, players, playerQueue, ScoringStrategy, RoundNumber, MoveIteration);
        }

        override
        public string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var player in Players)
            {
                stringBuilder.Append(player.ToString());
                stringBuilder.Append("\n\r");
            }

            return stringBuilder.ToString();
        }
    }
}
