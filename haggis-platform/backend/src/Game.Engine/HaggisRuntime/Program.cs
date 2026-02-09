using Haggis.Interfaces;
using Haggis.Model;
using Haggis.StartingTrickFilterStrategies;
using Haggis.Strategies;

InitConsole.Apply();  // ✔ powiększenie konsoli

var piotr = new HaggisPlayer("Piotr");

var slawekStrategy = new MonteCarloStrategy(1000, 2000);


var slawek = new AIPlayer("Sławek")
{
    StartingTrickFilterStrategy = new FilterContinuations(10, false),
    PlayStrategy = slawekStrategy
};

var robertStrategy = new MonteCarloStrategy(1000, 2000);


var robert = new AIPlayer("Robert")
{
    StartingTrickFilterStrategy = new FilterContinuations(10, false),
    PlayStrategy = robertStrategy
};


var players = new List<IHaggisPlayer>() { piotr, slawek, robert };



new GameLoop(players).Run();
