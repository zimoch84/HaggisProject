using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.Domain.Services
{
    public sealed class ActionApplicationService : IActionApplicationService
    {
        private IBoardMutationService BoardMutationService { get; }
        private IRunOutScoringService RunOutScoringService { get; }
        private ITurnOrderService TurnOrderService { get; }
        private ITrickResolutionService TrickResolutionService { get; }
        private IRoundEndScoringService RoundEndScoringService { get; }

        public ActionApplicationService(
            IBoardMutationService boardMutationService = null,
            IRunOutScoringService runOutScoringService = null,
            ITurnOrderService turnOrderService = null,
            ITrickResolutionService trickResolutionService = null,
            IRoundEndScoringService roundEndScoringService = null)
        {
            BoardMutationService = boardMutationService ?? new BoardMutationService();
            RunOutScoringService = runOutScoringService ?? new RunOutScoringService();
            TurnOrderService = turnOrderService ?? new TurnOrderService();
            TrickResolutionService = trickResolutionService ?? new TrickResolutionService();
            RoundEndScoringService = roundEndScoringService ?? new RoundEndScoringService();
        }

        public void Apply(HaggisGameState state, HaggisAction action)
        {
            BoardMutationService.Apply(state, action);
            state.MoveIteration++;
            RunOutScoringService.Apply(state, action);

            if (!state.RoundOver())
            {
                TurnOrderService.Update(state, action);
                TrickResolutionService.Resolve(state);
                return;
            }

            RoundEndScoringService.Apply(state);
        }
    }
}
