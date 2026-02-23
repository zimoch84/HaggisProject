using Haggis.Domain.Interfaces;
using System.Collections.Generic;

namespace Haggis.AI.Model
{
    public class MonteCarloResult
    {
        public IHaggisPlayer Player { get; set; }
        public List<MonteCarloActionInfo> Actions { get; set; }
    }
}

