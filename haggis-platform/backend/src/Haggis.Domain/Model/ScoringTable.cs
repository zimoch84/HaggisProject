using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Interfaces;

namespace Haggis.Domain.Model
{
    public sealed class ScoringTable
    {
        private readonly List<RoundScoringResult> _roundScores = new List<RoundScoringResult>();

        public int Count => _roundScores.Count;

        public RoundScoringResult this[int index] => _roundScores[index];

        public IReadOnlyList<RoundScoringResult> RoundScores => _roundScores;

        public void AddRoundScore(RoundScoringResult roundScore)
        {
            if (roundScore is null)
            {
                return;
            }

            if (_roundScores.Any(entry => entry.RoundNumber == roundScore.RoundNumber))
            {
                return;
            }

            _roundScores.Add(roundScore);
        }

        public RoundScoringResult GetLastRoundScore()
        {
            return _roundScores.LastOrDefault();
        }

        public string GetPlayerWithLeastPoints()
        {
            var playerTotals = GetPlayersTotalPoints();
            if (playerTotals.Count == 0)
            {
                return null;
            }

            var playerWithLeastPoints = playerTotals
                .OrderBy(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return playerWithLeastPoints.Key;
        }

        public IReadOnlyDictionary<string, int> GetPlayersTotalPoints()
        {
            var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var round in _roundScores)
            {
                if (round?.PlayerScores is null)
                {
                    continue;
                }

                foreach (var score in round.PlayerScores)
                {
                    if (score is null || string.IsNullOrWhiteSpace(score.PlayerName))
                    {
                        continue;
                    }

                    var playerName = score.PlayerName.Trim();
                    var current = totals.TryGetValue(playerName, out var existing) ? existing : 0;
                    totals[playerName] = current + score.RoundPoints;
                }
            }

            return totals;
        }

        public int TotalScore(IHaggisPlayer player)
        {
            if (player is null || string.IsNullOrWhiteSpace(player.Name))
            {
                return 0;
            }

            var playerName = player.Name.Trim();
            return _roundScores
                .Where(round => round != null && round.PlayerScores != null)
                .SelectMany(round => round.PlayerScores)
                .Where(score => score != null &&
                                !string.IsNullOrWhiteSpace(score.PlayerName) &&
                                string.Equals(score.PlayerName.Trim(), playerName, StringComparison.OrdinalIgnoreCase))
                .Sum(score => score.RoundPoints);
        }
    }
}
