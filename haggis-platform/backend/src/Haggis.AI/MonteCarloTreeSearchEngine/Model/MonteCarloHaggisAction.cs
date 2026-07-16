using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisAction : HaggisAction, IAction
    {
        private MonteCarloHaggisAction(Trick trick, IHaggisPlayer player, bool isFinal = false)
            : base(trick, player, isFinal)
        {
        }

        public new static MonteCarloHaggisAction FromTrick(Trick trick, IHaggisPlayer player, bool isFinal = false)
        {
            return new MonteCarloHaggisAction(trick, player, isFinal);
        }

        public new static MonteCarloHaggisAction Pass(IHaggisPlayer player)
        {
            return new MonteCarloHaggisAction(null, player);
        }

        public static MonteCarloHaggisAction FromHaggisAction(HaggisAction action)
        {
            return action.IsPass
                ? Pass(action.Player)
                : FromTrick(action.Trick, action.Player, action.IsFinal);
        }

        public MonteCarloHaggisAction AsFinal()
        {
            return IsPass ? this : FromTrick(Trick, Player, true);
        }
    }
}
