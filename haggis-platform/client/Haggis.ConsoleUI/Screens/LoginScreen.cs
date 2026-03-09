public sealed class LoginScreen
{
    private readonly StaticConsoleUI _ui;

    public LoginScreen(StaticConsoleUI ui)
    {
        _ui = ui;
    }

    public string Show()
    {
        while (true)
        {
            _ui.RenderLogin();
            var playerId = _ui.ReadLoginInput();
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                return playerId.Trim();
            }
        }
    }
}
