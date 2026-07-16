using Haggis.ConsoleUI.Application.Game;
using Haggis.ConsoleUI.Application.Lobby;
using Haggis.ConsoleUI.Presentation.Screens;
using Haggis.ConsoleUI.Presentation.ViewModels.Lobby;
using Haggis.ConsoleUI.Presentation.ViewModels.Start;

namespace Haggis.ConsoleUI.Application.AppFlow;

public enum AppFlowState
{
    Login,
    ModeSelect,
    MainLobby,
    Lobby,
    Game,
    Closed
}

public sealed class AppFlowController
{
    private readonly GlobalLobbyWebSocketClient _lobbyClient;
    private readonly string? _defaultPlayerId;
    private readonly LoginScreen _loginScreen = new();
    private readonly StartModeScreen _startModeScreen = new();
    private readonly LobbyScreen _mainLobbyScreen = new();
    private readonly GameScreen _gameScreen = new();
    private readonly LobbyController _lobbyController;

    private AppFlowState _currentState;
    private string? _playerId;
    private LobbyRoom? _selectedRoom;
    private bool _singlePlayerGame;

    public AppFlowController(GlobalLobbyWebSocketClient lobbyClient, string? defaultPlayerId = null)
    {
        _lobbyClient = lobbyClient;
        _defaultPlayerId = string.IsNullOrWhiteSpace(defaultPlayerId) ? null : defaultPlayerId.Trim();
        _lobbyController = new LobbyController(_lobbyClient, _mainLobbyScreen);
        _playerId = _defaultPlayerId;
        _currentState = _playerId is null ? AppFlowState.Login : AppFlowState.ModeSelect;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested &&
               _currentState != AppFlowState.Closed)
        {
            switch (_currentState)
            {
                case AppFlowState.Login:
                    await RunLoginAsync(cancellationToken);
                    break;

                case AppFlowState.ModeSelect:
                    await RunModeSelectAsync(cancellationToken);
                    break;

                case AppFlowState.MainLobby:
                    await RunMainLobbyAsync(cancellationToken);
                    break;

                case AppFlowState.Lobby:
                    _currentState = AppFlowState.Game;
                    break;

                case AppFlowState.Game:
                    await RunGameAsync(cancellationToken);
                    break;

                default:
                    _currentState = AppFlowState.Closed;
                    break;
            }
        }
    }

    private async Task RunLoginAsync(CancellationToken cancellationToken)
    {
        var playerId = _defaultPlayerId ?? await _loginScreen.ShowAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(playerId))
        {
            _currentState = AppFlowState.Closed;
            return;
        }

        _playerId = playerId.Trim();
        _currentState = AppFlowState.ModeSelect;
    }

    private async Task RunModeSelectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_playerId))
        {
            _currentState = AppFlowState.Login;
            return;
        }

        var action = await _startModeScreen.ShowAsync(_playerId, cancellationToken);
        switch (action)
        {
            case StartModeAction.SinglePlayer:
                _selectedRoom = CreateSinglePlayerRoom(_playerId);
                _singlePlayerGame = true;
                _currentState = AppFlowState.Game;
                return;

            case StartModeAction.MultiPlayer:
                _singlePlayerGame = false;
                _currentState = AppFlowState.MainLobby;
                return;

            case StartModeAction.Quit:
            default:
                _currentState = AppFlowState.Closed;
                return;
        }
    }

    private async Task RunMainLobbyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_playerId))
        {
            _currentState = AppFlowState.Login;
            return;
        }

        _selectedRoom = await _lobbyController.RunAsync(_playerId, cancellationToken);
        _singlePlayerGame = false;
        _currentState = _selectedRoom is null
            ? AppFlowState.Closed
            : AppFlowState.Lobby;
    }

    private async Task RunGameAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_playerId) || _selectedRoom is null)
        {
            _currentState = AppFlowState.MainLobby;
            return;
        }

        var gameController = new GameController(
            new RemoteGameOptions(_playerId, _selectedRoom.GameId, _lobbyClient.ServerBaseUrl, null, _singlePlayerGame),
            _gameScreen);

        var result = await gameController.RunAsync(cancellationToken);
        _currentState = result == RemoteGameLoopResult.BackToLobby
            ? AppFlowState.ModeSelect
            : AppFlowState.Closed;
    }

    private static LobbyRoom CreateSinglePlayerRoom(string playerId)
    {
        var gameId = $"single-{Slugify(playerId)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        return new LobbyRoom
        {
            RoomId = gameId,
            GameId = gameId,
            RoomName = "Single Player",
            GameEndpoint = $"/ws/games/{gameId}",
            Players = new List<string> { playerId }
        };
    }

    private static string Slugify(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug.Trim('-')) ? "player" : slug.Trim('-');
    }
}
