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
        private TrickPlay _currentTrickPlay;
        private readonly List<IHaggisPlayer> _players;
        private readonly IHaggisScoringStrategy _scoringStrategy;
        private HaggisPlayerQueue _playerQueue;
        private List<HaggisAction> NotPassActions => _currentTrickPlay.NotPassActions;
        private HaggisAction LastAction => _currentTrickPlay?.LastAction;

        public List<IHaggisPlayer> Players { get { return _players; } }
        [JsonIgnore]
        public IHaggisPlayer CurrentPlayer => FindPlayerByGUID(_playerQueue.GetCurrentPlayer());

        [JsonIgnore]
        public IHaggisPlayer NextPlayer => FindPlayerByGUID(_playerQueue.GetNextPlayer());

        [JsonIgnore]
        public TrickPlay CurrentTrickPlay => _currentTrickPlay;

        [JsonIgnore]
        public LinkedList<TrickPlay> ActionArchive;

        [JsonIgnore]
        public IList<HaggisAction> Actions => GetPossibleActionsForCurrentPlayer();

        public HaggisGameState(List<IHaggisPlayer> players, IHaggisScoringStrategy scoringStrategy = null)
        {
            ActionArchive = new LinkedList<TrickPlay>();
            _currentTrickPlay = new TrickPlay(players.Count);
            _playerQueue = new HaggisPlayerQueue(players);
            _players = new List<IHaggisPlayer>(players);
            _scoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
        }

        private HaggisGameState(
            LinkedList<TrickPlay> historyTricks,
            TrickPlay trickPlay,
            List<IHaggisPlayer> players,
            HaggisPlayerQueue playerQueue,
            IHaggisScoringStrategy scoringStrategy)
        {
            ActionArchive = new LinkedList<TrickPlay>(historyTricks);
            _currentTrickPlay = trickPlay;
            _players = new List<IHaggisPlayer>(players);
            _playerQueue = playerQueue;
            _scoringStrategy = scoringStrategy ?? new ClassicHaggisScoringStrategy();
        }

        public bool RoundOver()
        {
            return _players.Count(p => !p.Finished) == 1;
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
            _playerQueue.SetCurrentPlayer(player);
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
            _currentTrickPlay.AddAction(action);
        }

        private void MoveCardsToDiscardForPlayer(IHaggisPlayer player)
        {
            var target = FindPlayerByGUID(player.GUID);

            // Collect all cards from current TrickPlay actions (non-pass only).
            var cardsToMove = _currentTrickPlay?.Actions
                .Where(a => !a.IsPass && a.Trick != null)
                .SelectMany(a => a.Trick.Cards)
                .ToList();

            if (cardsToMove != null && cardsToMove.Count > 0)
            {
                target.AddToDiscard(new List<Card>(cardsToMove));
            }

            // Clear current TrickPlay to avoid moving same cards twice.
            _currentTrickPlay?.Clear();
        }

        private void PlayerQueueRemoveOrRotate(HaggisAction action)
        {
            if (FindPlayerByGUID(action.Player.GUID).Finished)
                _playerQueue.RemoveFromQueue(action.Player);
            else
                _playerQueue.RotatePlayersClockwise();
        }

        private void CheckEndingPassAndMoveCardsToTakingPlayer()
        {
            if (_currentTrickPlay.IsEndingPass())
            {
                var takingPlayer = _currentTrickPlay.Taking();
                ActionArchive.AddLast(_currentTrickPlay);

                MoveCardsToDiscardForPlayer(takingPlayer);
                _currentTrickPlay = new TrickPlay(_players.Where(p => !p.Finished).Count());
            }
        }

        public void ApplyAction(HaggisAction action)
        {
            ApplyActionToBoard(action);

            ScoreForRunOut(action);

            if (!RoundOver())
            {
                PlayerQueueRemoveOrRotate(action);
                CheckEndingPassAndMoveCardsToTakingPlayer();
            }
            else
            {
                ActionArchive.AddLast(_currentTrickPlay);
                ScoreForDiscardCards();
            }
        }

        private void ScoreForDiscardCards()
        {
            foreach (var player in _players)
            {
                player.Score += player.Discard.Sum(card => _scoringStrategy.GetCardPoints(card));
            }
        }

        private void ScoreForRunOut(HaggisAction action)
        {
            if (action == null || action.Player == null)
                return;

            var target = _players.FirstOrDefault(p => p.GUID == action.Player.GUID);
            if (target == null || !target.Finished)
                return;

            foreach (var player in _players.Where(p => !p.Finished && p.GUID != action.Player.GUID))
            {
                target.Score += player.Hand.Count * _scoringStrategy.RunOutMultiplier;
            }
        }

        private IHaggisPlayer FindPlayerByGUID(Guid guid)
        {
            return _players.First(p => p.GUID == guid);
        }

        public IState<IHaggisPlayer, HaggisAction> Clone()
        {
            var players = new List<IHaggisPlayer>(_players?.DeepCopy());
            var board = (TrickPlay)_currentTrickPlay?.Clone();
            var archive = new LinkedList<TrickPlay>(ActionArchive.DeepCopy());
            var playerQueue = (HaggisPlayerQueue)_playerQueue.Clone();
            return new HaggisGameState(archive, board, players, playerQueue, _scoringStrategy);
        }

        public double GetResult(IHaggisPlayer forPlayer)
        {
            var forPlayerScore = _players.Where(p => p.GUID == forPlayer.GUID).First().Score;

            var otherWinner = _players.Where(p => p.Score > forPlayerScore).Count();
            if (otherWinner > 0)
                return 0;

            return 1;
        }

        override
        public string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var player in _players)
            {
                stringBuilder.Append(player.ToString());
                stringBuilder.Append("\n\r");
            }

            return stringBuilder.ToString();
        }
    }
}
