using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Haggis.Domain.Model
{
    public class RoundState
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
        public TrickPlay CurrentTrickPlay { get; set; }

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

        [JsonIgnore]
        public IReadOnlyList<Card> HaggisCards { get; private set; }

        [JsonIgnore]
        public IReadOnlyList<Guid> FinishingOrder { get; private set; }

        private RoundState(
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy,
            int roundNumber,
            long moveIteration,
            List<Card> haggisCards,
            List<Guid> finishingOrder)
        {
            Players = new List<IHaggisPlayer>(players);
            PlayerQueue = playerQueue;
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            RoundNumber = roundNumber < 1 ? 1 : roundNumber;
            MoveIteration = moveIteration < 0 ? 0 : moveIteration;
            HaggisCards = haggisCards ?? new List<Card>();
            FinishingOrder = finishingOrder ?? new List<Guid>();
        }

        public RoundState(
            List<IHaggisPlayer> players,
            IHaggisScoringStrategy scoringStrategy = null,
            int roundNumber = 1,
            long moveIteration = 0,
            List<Card> haggisCards = null)
            : this(
                players,
                new HaggisPlayerQueue(players),
                scoringStrategy,
                roundNumber,
                moveIteration,
                haggisCards,
                new List<Guid>())
        {
            ActionArchive = new LinkedList<TrickPlay>();
            CurrentTrickPlay = new TrickPlay(players.Count);
        }

        private RoundState(
            LinkedList<TrickPlay> historyTricks,
            TrickPlay trickPlay,
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy,
            int roundNumber,
            long moveIteration,
            List<Card> haggisCards,
            List<Guid> finishingOrder)
            : this(players, playerQueue, scoringStrategy, roundNumber, moveIteration, haggisCards, finishingOrder)
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

        public void RegisterPlayerFinished(IHaggisPlayer player)
        {
            if (player == null || !player.Finished || FinishingOrder.Contains(player.GUID))
            {
                return;
            }

            FinishingOrder.Add(player.GUID);
        }

        public RoundState Clone()
        {
            var players = new List<IHaggisPlayer>(Players?.DeepCopy());
            var board = (TrickPlay)CurrentTrickPlay?.Clone();
            var archive = new LinkedList<TrickPlay>(ActionArchive.DeepCopy());
            var playerQueue = (HaggisPlayerQueue)PlayerQueue.Clone();
            var haggisCards = HaggisCards.ToList().DeepCopy().ToList();
            var finishingOrder = new List<Guid>(FinishingOrder);
            return new RoundState(
                archive,
                board,
                players,
                playerQueue,
                ScoringStrategy,
                RoundNumber,
                MoveIteration,
                haggisCards,
                finishingOrder);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var player in Players)
            {
                stringBuilder.Append(player);
                stringBuilder.Append("\n\r");
            }

            return stringBuilder.ToString();
        }
    }
}
