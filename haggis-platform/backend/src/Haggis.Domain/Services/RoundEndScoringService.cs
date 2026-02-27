using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.Domain.Services
{
    public sealed class RoundEndScoringService : IRoundEndScoringService
    {
        public void Apply(RoundState state)
        {
            state.ActionArchive.AddLast(state.CurrentTrickPlay);
        }
    }
}
