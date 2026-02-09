using Haggis.Interfaces;
using Haggis.Model;
using System.Collections.Generic;

public class MonteCarloResult
{
    public IHaggisPlayer Player { get; set; }
    public List<MonteCarloActionInfo> Actions { get; set; }
}