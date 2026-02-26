using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using System.Linq;

namespace Haggis.Domain.Services
{
    public sealed class RoundEndScoringService : IRoundEndScoringService
    {
        public void Apply(HaggisGameState state)
        {
            state.ActionArchive.AddLast(state.CurrentTrickPlayState);
            foreach (var player in state.Players)
            {
                player.Score += player.Discard.Sum(card => state.ScoringStrategy.GetCardPoints(card));
            }
        }
    }
}
