using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.AI.Model;

public class GameLoop
{
    private HaggisGameState _state;
    private readonly HaggisGame _game;
    private StaticConsoleUI StaticConsoleUI;
    private List<IHaggisPlayer> _players;

    public GameLoop(List<IHaggisPlayer> players)
    {
        _players = players;
        _state = new HaggisGameState(players);
        _game = new HaggisGame(players);
        _game.SetSeed(1234567890);
        _game.SetWinScore(250);
        StaticConsoleUI = new StaticConsoleUI(_state);
    }   

    public void Run()
    {
        _game.NewRound();
        while (!_game.GameOver())
        {
            while (!_state.RoundOver())
            {
                StaticConsoleUI.Render(_state);
                if (_state.CurrentPlayer is AIPlayer ai)
                {
                    var action = ai.GetPlayingAction(_state);
                    _state.ApplyAction(action);
                }
                else
                {
                    int index = StaticConsoleUI.ReadHumanInput(_state);
                    _state.ApplyAction(_state.PossibleActions[index]);
                }
            }

            StaticConsoleUI.Render(_state);
            Console.ReadKey();

            _game.NewRound();
            _state = new HaggisGameState(_players);
            _state.SetCurrentPlayer(_players.MinBy(p => p.Score));

        }
    }
}




