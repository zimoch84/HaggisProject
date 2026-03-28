public enum AppFlowState
{
    Login,
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
    private readonly LobbyScreen _mainLobbyScreen = new();
    private readonly GameScreen _gameScreen = new();
    private readonly LobbyController _lobbyController;

    private AppFlowState _currentState;
    private string? _playerId;
    private LobbyRoom? _selectedRoom;

    public AppFlowController(GlobalLobbyWebSocketClient lobbyClient, string? defaultPlayerId = null)
    {
        _lobbyClient = lobbyClient;
        _defaultPlayerId = string.IsNullOrWhiteSpace(defaultPlayerId) ? null : defaultPlayerId.Trim();
        _lobbyController = new LobbyController(_lobbyClient, _mainLobbyScreen);
        _playerId = _defaultPlayerId;
        _currentState = _playerId is null ? AppFlowState.Login : AppFlowState.MainLobby;
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
        _currentState = AppFlowState.MainLobby;
    }

    private async Task RunMainLobbyAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_playerId))
        {
            _currentState = AppFlowState.Login;
            return;
        }

        _selectedRoom = await _lobbyController.RunAsync(_playerId, cancellationToken);
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
            new RemoteGameOptions(_playerId, _selectedRoom.GameId, _lobbyClient.ServerBaseUrl, null),
            _gameScreen);

        var result = await gameController.RunAsync(cancellationToken);
        _currentState = result == RemoteGameLoopResult.BackToLobby
            ? AppFlowState.MainLobby
            : AppFlowState.Closed;
    }

}
