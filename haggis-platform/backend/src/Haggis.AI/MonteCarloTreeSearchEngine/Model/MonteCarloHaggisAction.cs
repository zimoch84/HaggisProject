using Haggis.Domain.Model;
using Haggis.Domain.Interfaces;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisAction : HaggisAction, IAction
    {
        private MonteCarloHaggisAction(Trick trick, IHaggisPlayer player)
            : base(trick, player)
        {
        }

        public new static MonteCarloHaggisAction FromTrick(Trick trick, IHaggisPlayer player)
        {
            return new MonteCarloHaggisAction(trick, player);
        }

        public new static MonteCarloHaggisAction Pass(IHaggisPlayer player)
        {
            return new MonteCarloHaggisAction(null, player);
        }

        public static MonteCarloHaggisAction FromHaggisAction(HaggisAction action)
        {
            return action.IsPass
                ? Pass(action.Player)
                : FromTrick(action.Trick, action.Player);
        }
    }
}
