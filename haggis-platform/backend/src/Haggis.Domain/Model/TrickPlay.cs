using Haggis.Domain.Extentions;
using Haggis.Domain.Enums;
using System.Data;
using Haggis.Domain.Interfaces;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

namespace Haggis.Domain.Model
{
    public class TrickPlay : ICloneable
    {
        private List<HaggisAction> _actions;
        private int _numberOfPlayers;

        public List<HaggisAction> Actions => _actions;
        public HaggisAction LastAction => _actions.Count() > 0 ? _actions.Last() : null;
        public HaggisAction SecondToLastAction => _actions.GetSecondToLast();
        public int NumberOfPlayers => _numberOfPlayers;
        public bool IsEmpty => _actions.Count == 0;
        public List<HaggisAction> NotPassActions => _actions.Where(a => !a.IsPass).ToList();

        public TrickPlay(int numberOfPlayers)
        {
            _actions = new List<HaggisAction>();
            _numberOfPlayers = numberOfPlayers;
        }

        public TrickPlay(int numberOfPlayers, List<HaggisAction> actions)
        {
            _actions = actions;
            _numberOfPlayers = numberOfPlayers;
        }

        public bool IsEndingPass()
        {
            if (LastAction == null)
                return false;

            if (NumberOfPlayers == 2)
                return LastAction.IsPass;

            if (NumberOfPlayers == 3)
            {
                if (LastAction == null)
                    return false;

                var hasFinalTrick = NotPassActions.Where(a => a.IsFinal).Any();

                if (hasFinalTrick && SecondToLastAction != null) {
                    /*First pass after another player finishes does let the third player play trick
                      So we need to check if current pass is first after final trick*/
                    if (SecondToLastAction.IsFinal)
                    {
                        return false;
                    }
                    return LastAction.IsPass;
                }

                if (SecondToLastAction == null)
                    return false;
                return LastAction.IsPass && SecondToLastAction.IsPass;
            }

            return false;
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void AddAction(HaggisAction action)
        {
            _actions.Add(action);
        }
        public object Clone()
        {
            return new TrickPlay(_numberOfPlayers, new List<HaggisAction>(_actions.DeepCopy()));
        }

        public IHaggisPlayer Taking()
        {
            var lastNotPassAction = NotPassActions.Last();
            if (lastNotPassAction.Trick?.Type != TrickType.BOMB)
            {
                return lastNotPassAction.Player;
            }

            var previousBestAction = NotPassActions
                .Where(a => a.Trick?.Type != TrickType.BOMB)
                .LastOrDefault();

            /* If the trick starts with a bomb there is no earlier best trick. */
            return previousBestAction?.Player ?? lastNotPassAction.Player;
        }

        public override bool Equals(object obj)
        {
            if (obj is TrickPlay other)
            {
                 if (_numberOfPlayers != other._numberOfPlayers)
                    return false;

                  if (_actions.Count != other._actions.Count)
                    return false;

                for (int i = 0; i < _actions.Count; i++)
                {
                    if (!_actions[i].Equals(other._actions[i]))
                        return false;
                }

                return true; 
            }

            return false; 
        }
        public override int GetHashCode()
        {
             int hash = _numberOfPlayers.GetHashCode();
            foreach (var action in _actions)
            {
                hash ^= action.GetHashCode(); // XOR z haszem akcji
            }
            return hash;
        }
        override
        public string ToString() {

            StringBuilder sb = new StringBuilder();
            foreach (var action in _actions) {
                sb.Append(action.Desc);
                sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
