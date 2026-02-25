using System.Collections.Generic;
using Haggis.Domain.Model;

namespace Haggis.Domain.Interfaces
{
    public interface IDeckDealer
    {
        List<Card> CreateShuffledDeck(int seed);
        List<Card> DealSetupCards(List<Card> deck);
    }
}
