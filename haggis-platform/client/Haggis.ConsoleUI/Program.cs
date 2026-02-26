using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.AI.Strategies;
using Haggis.AI.Model;

InitConsole.Apply();  // ? powiększenie konsoli

var piotr = new HaggisPlayer("Piotr");

var slawekStrategy = new MonteCarloStrategy(1000, 2000);


var slawek = new AIPlayer("Sławek")
{
    PlayStrategy = slawekStrategy
};

var robertStrategy = new MonteCarloStrategy(1000, 2000);


var robert = new AIPlayer("Robert")
{
    PlayStrategy = robertStrategy
};


var players = new List<IHaggisPlayer>() { piotr, slawek, robert };



new GameLoop(players).Run();

