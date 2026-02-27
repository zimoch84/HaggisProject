using System;
using System.Collections.Generic;
using System.Linq;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.Domain.Services
{
    public sealed class ScoringTableService
    {
        public RoundScoringResult BuildRoundScoringResult(RoundState state)
        {
            if (state is null)
            {
                return null;
            }

            var roundPointsByPlayer = BuildRoundPointsByPlayer(state);
            var playersByScore = state.Players
                .OrderByDescending(player => roundPointsByPlayer[player.GUID])
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var finishingOrderPlayerNames = BuildFinishingOrderPlayerNames(state);

            var rankedScores = new List<PlayerRoundScore>();
            foreach (var player in playersByScore)
            {
                var roundPoints = roundPointsByPlayer[player.GUID];
                player.Score = roundPoints;

                rankedScores.Add(new PlayerRoundScore
                {
                    PlayerName = player.Name,
                    RoundPoints = roundPoints
                });
            }

            return new RoundScoringResult
            {
                RoundNumber = state.RoundNumber,
                WinnerPlayerName = rankedScores.FirstOrDefault()?.PlayerName,
                PlayerScores = rankedScores,
                FinishingOrderPlayerNames = finishingOrderPlayerNames
            };
        }

        private static Dictionary<Guid, int> BuildRoundPointsByPlayer(RoundState state)
        {
            var points = state.Players.ToDictionary(player => player.GUID, _ => 0);
            var haggisPoints = state.HaggisCards?.Sum(card => state.ScoringStrategy.GetCardPoints(card)) ?? 0;
            var firstFinishedGuid = state.FinishingOrder.FirstOrDefault();

            foreach (var player in state.Players)
            {
                var discardPoints = player.Discard.Sum(card => state.ScoringStrategy.GetCardPoints(card));
                var runOutPoints = player.OpponentRemainingCardsOnFinish < 0
                    ? 0
                    : player.OpponentRemainingCardsOnFinish * state.ScoringStrategy.RunOutMultiplier;
                var bonusHaggisPoints = firstFinishedGuid == player.GUID ? haggisPoints : 0;

                points[player.GUID] = discardPoints + runOutPoints + bonusHaggisPoints;
            }

            return points;
        }

        private static IReadOnlyList<string> BuildFinishingOrderPlayerNames(RoundState state)
        {
            var namesByGuid = state.Players.ToDictionary(player => player.GUID, player => player.Name);
            var orderedNames = new List<string>();

            foreach (var playerGuid in state.FinishingOrder)
            {
                if (namesByGuid.TryGetValue(playerGuid, out var playerName) &&
                    !string.IsNullOrWhiteSpace(playerName) &&
                    !orderedNames.Contains(playerName))
                {
                    orderedNames.Add(playerName);
                }
            }

            foreach (var player in state.Players)
            {
                if (!orderedNames.Contains(player.Name))
                {
                    orderedNames.Add(player.Name);
                }
            }

            return orderedNames;
        }

        public IReadOnlyList<IHaggisPlayer> BuildRoundPlayersOrder(IReadOnlyList<IHaggisPlayer> seatingOrder, ScoringTable scoringTable)
        {
            if (seatingOrder == null || seatingOrder.Count == 0)
            {
                return new List<IHaggisPlayer>();
            }

            var leastScorePlayer = seatingOrder
                .Select((player, index) => new
                {
                    Player = player,
                    Index = index,
                    TotalScore = scoringTable == null ? 0 : scoringTable.TotalScore(player)
                })
                .OrderBy(entry => entry.TotalScore)
                .ThenBy(entry => entry.Index)
                .First()
                .Player;

            var startIndex = seatingOrder
                .Select((player, index) => new { Player = player, Index = index })
                .First(entry => entry.Player.GUID == leastScorePlayer.GUID)
                .Index;

            var orderedPlayers = new List<IHaggisPlayer>(seatingOrder.Count);
            for (var i = 0; i < seatingOrder.Count; i++)
            {
                var index = (startIndex + i) % seatingOrder.Count;
                orderedPlayers.Add(seatingOrder[index]);
            }

            return orderedPlayers;
        }
    }
}
