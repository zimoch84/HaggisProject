using Haggis.Domain.Interfaces;

namespace MonteCarlo
{
    public sealed class MonteCarloHaggisPlayer : IPlayer
    {
        public MonteCarloHaggisPlayer(IHaggisPlayer player)
        {
            DomainPlayer = player;
        }

        public IHaggisPlayer DomainPlayer { get; }
    }
}
