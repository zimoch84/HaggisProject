using Haggis.Domain.Extentions;
using MonteCarlo;
using System.Text;
using Newtonsoft.Json;
using Haggis.Domain.Interfaces;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Haggis.Domain.Model
{
    public class HaggisGameState : IState<IHaggisPlayer, HaggisAction>
    {
        private TrickPlay CurrentTrickPlayState { get; set; }
        private List<IHaggisPlayer> PlayersState { get; }
        private HaggisPlayerQueue PlayerQueue { get; set; }
        private int RoundNumberState { get; }
        private long MoveIterationState { get; set; }
        private List<HaggisAction> NotPassActions => CurrentTrickPlayState.NotPassActions;
        private HaggisAction LastAction => CurrentTrickPlayState?.LastAction;

        public List<IHaggisPlayer> Players { get { return PlayersState; } }
        [JsonIgnore]
        public IHaggisPlayer CurrentPlayer => FindPlayerByGUID(PlayerQueue.GetCurrentPlayer());

        [JsonIgnore]
        public IHaggisPlayer NextPlayer => FindPlayerByGUID(PlayerQueue.GetNextPlayer());

        [JsonIgnore]
        public TrickPlay CurrentTrickPlay => CurrentTrickPlayState;

        [JsonIgnore]
        public LinkedList<TrickPlay> ActionArchive;

        [JsonIgnore]
        public IList<HaggisAction> Actions => GetPossibleActionsForCurrentPlayer();
        [JsonIgnore]
        public IHaggisScoringStrategy ScoringStrategy { get; private set; }
        [JsonIgnore]
        public int RoundNumber => RoundNumberState;
        [JsonIgnore]
        public long MoveIteration => MoveIterationState;

        public HaggisGameState(
            List<IHaggisPlayer> players,
            IHaggisScoringStrategy scoringStrategy = null,
            int roundNumber = 1,
            long moveIteration = 0)
        {
            ActionArchive = new LinkedList<TrickPlay>();
            CurrentTrickPlayState = new TrickPlay(players.Count);
            PlayerQueue = new HaggisPlayerQueue(players);
            PlayersState = new List<IHaggisPlayer>(players);
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            RoundNumberState = roundNumber < 1 ? 1 : roundNumber;
            MoveIterationState = moveIteration < 0 ? 0 : moveIteration;
        }

        private HaggisGameState(
            LinkedList<TrickPlay> historyTricks,
            TrickPlay trickPlay,
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy,
            int roundNumber,
            long moveIteration)
        {
            ActionArchive = new LinkedList<TrickPlay>(historyTricks);
            CurrentTrickPlayState = trickPlay;
            PlayersState = new List<IHaggisPlayer>(players);
            PlayerQueue = playerQueue;
            ScoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
            RoundNumberState = roundNumber < 1 ? 1 : roundNumber;
            MoveIterationState = moveIteration < 0 ? 0 : moveIteration;
        }

        public bool RoundOver()
        {
            return PlayersState.Count(p => !p.Finished) == 1;
        }

        private IList<HaggisAction> GetPossibleActionsForCurrentPlayer()
        {
            if (RoundOver())
                return Enumerable.Empty<HaggisAction>().ToList();

            var actions = new List<HaggisAction>();
            var lastTrick = NotPassActions?.LastOrDefault()?.Trick;
            var possibleTricks = CurrentPlayer.SuggestedTricks(lastTrick);

            bool isFinalAction = possibleTricks.Where(t => t.IsFinal).Any();

            possibleTricks.ForEach(trick => actions.Add(HaggisAction.FromTrick(trick, CurrentPlayer)));

            /*Play pass only when You cant finish trickPlay */
            if (LastAction != null && !isFinalAction)
                actions.Add(HaggisAction.Pass(CurrentPlayer));

            return actions;
        }

        public void SetCurrentPlayer(IHaggisPlayer player)
        {
            PlayerQueue.SetCurrentPlayer(player);
        }

        private void RemoveCardsFromHand(HaggisAction action)
        {
            if (action.IsPass)
                return;

            FindPlayerByGUID(action.Player.GUID).RemoveFromHand(action.Trick.Cards);
        }

        public void ApplyActionToBoard(HaggisAction action)
        {
            RemoveCardsFromHand(action);
            CurrentTrickPlayState.AddAction(action);
        }

        private void MoveCardsToDiscardForPlayer(IHaggisPlayer player)
        {
            var target = FindPlayerByGUID(player.GUID);

            // Collect all cards from current TrickPlay actions (non-pass only).
            var cardsToMove = CurrentTrickPlayState?.Actions
                .Where(a => !a.IsPass && a.Trick != null)
                .SelectMany(a => a.Trick.Cards)
                .ToList();

            if (cardsToMove != null && cardsToMove.Count > 0)
            {
                target.AddToDiscard(new List<Card>(cardsToMove));
            }

            // Clear current TrickPlay to avoid moving same cards twice.
            CurrentTrickPlayState?.Clear();
        }

        private void PlayerQueueRemoveOrRotate(HaggisAction action)
        {
            if (FindPlayerByGUID(action.Player.GUID).Finished)
                PlayerQueue.RemoveFromQueue(action.Player);
            else
                PlayerQueue.RotatePlayersClockwise();
        }

        private void CheckEndingPassAndMoveCardsToTakingPlayer()
        {
            if (CurrentTrickPlayState.IsEndingPass())
            {
                var takingPlayer = CurrentTrickPlayState.Taking();
                ActionArchive.AddLast(CurrentTrickPlayState);

                MoveCardsToDiscardForPlayer(takingPlayer);
                CurrentTrickPlayState = new TrickPlay(PlayersState.Where(p => !p.Finished).Count());
            }
        }

        public void ApplyAction(HaggisAction action)
        {
            ApplyActionToBoard(action);
            MoveIterationState++;

            ScoreForRunOut(action);

            if (!RoundOver())
            {
                PlayerQueueRemoveOrRotate(action);
                CheckEndingPassAndMoveCardsToTakingPlayer();
            }
            else
            {
                ActionArchive.AddLast(CurrentTrickPlayState);
                ScoreForDiscardCards();
            }
        }

        private void ScoreForDiscardCards()
        {
            foreach (var player in PlayersState)
            {
                player.Score += player.Discard.Sum(card => ScoringStrategy.GetCardPoints(card));
            }
        }

        private void ScoreForRunOut(HaggisAction action)
        {
            if (action == null || action.Player == null)
                return;

            var target = PlayersState.FirstOrDefault(p => p.GUID == action.Player.GUID);
            if (target == null || !target.Finished)
                return;

            foreach (var player in PlayersState.Where(p => !p.Finished && p.GUID != action.Player.GUID))
            {
                target.Score += player.Hand.Count * ScoringStrategy.RunOutMultiplier;
            }
        }

        private IHaggisPlayer FindPlayerByGUID(Guid guid)
        {
            return PlayersState.First(p => p.GUID == guid);
        }

        public IState<IHaggisPlayer, HaggisAction> Clone()
        {
            var players = new List<IHaggisPlayer>(PlayersState?.DeepCopy());
            var board = (TrickPlay)CurrentTrickPlayState?.Clone();
            var archive = new LinkedList<TrickPlay>(ActionArchive.DeepCopy());
            var playerQueue = (HaggisPlayerQueue)PlayerQueue.Clone();
            return new HaggisGameState(archive, board, players, playerQueue, ScoringStrategy, RoundNumberState, MoveIterationState);
        }

        public double GetResult(IHaggisPlayer forPlayer)
        {
            var forPlayerScore = PlayersState.Where(p => p.GUID == forPlayer.GUID).First().Score;

            var otherWinner = PlayersState.Where(p => p.Score > forPlayerScore).Count();
            if (otherWinner > 0)
                return 0;

            return 1;
        }

        override
        public string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var player in PlayersState)
            {
                stringBuilder.Append(player.ToString());
                stringBuilder.Append("\n\r");
            }

            return stringBuilder.ToString();
        }
    }
}
