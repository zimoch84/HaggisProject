public sealed class GameScreen
{
    private readonly StaticConsoleUI _ui;

    public GameScreen(StaticConsoleUI ui)
    {
        _ui = ui;
    }

    public void Render(
        RemoteGameState? state,
        string playerId,
        string gameId,
        string status,
        IReadOnlyList<string> roomPlayers,
        bool autoStart,
        string inputText = "")
    {
        _ui.RenderRemote(state, playerId, gameId, status, roomPlayers, autoStart, inputText);
    }

    public async Task<RemotePossibleAction> ReadInputAsync(
        RemoteGameState state,
        string playerId,
        string gameId,
        string status,
        IReadOnlyList<string> roomPlayers,
        bool autoStart,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        var sync = new object();
        var input = string.Empty;

        using var renderCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renderTask = Task.Run(async () =>
        {
            while (!renderCancellation.IsCancellationRequested)
            {
                lock (sync)
                {
                    _ui.RenderRemote(state, playerId, gameId, status, roomPlayers, autoStart, input);
                }

                await Task.Delay(120, renderCancellation.Token);
            }
        }, renderCancellation.Token);

        while (!cancellationToken.IsCancellationRequested)
        {
            await pumpMessagesAsync();

            if (!Console.KeyAvailable)
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    if (int.TryParse(input, out var index) && index >= 0 && index < state.PossibleActions.Count)
                    {
                        renderCancellation.Cancel();
                        await AwaitRenderTaskAsync(renderTask);
                        return state.PossibleActions[index];
                    }

                    lock (sync)
                    {
                        input = string.Empty;
                    }
                    break;
                case ConsoleKey.Backspace:
                    lock (sync)
                    {
                        if (input.Length > 0)
                        {
                            input = input[..^1];
                        }
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        lock (sync)
                        {
                            input += key.KeyChar;
                        }
                    }
                    break;
            }
        }

        renderCancellation.Cancel();
        await AwaitRenderTaskAsync(renderTask);
        return state.PossibleActions[0];
    }

    public async Task<string> ReadCommandAsync(
        RemoteGameState? state,
        string playerId,
        string gameId,
        string status,
        IReadOnlyList<string> roomPlayers,
        bool autoStart,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        var sync = new object();
        var input = string.Empty;

        using var renderCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renderTask = Task.Run(async () =>
        {
            while (!renderCancellation.IsCancellationRequested)
            {
                lock (sync)
                {
                    _ui.RenderRemote(state, playerId, gameId, status, roomPlayers, autoStart, input);
                }

                await Task.Delay(120, renderCancellation.Token);
            }
        }, renderCancellation.Token);

        while (!cancellationToken.IsCancellationRequested)
        {
            await pumpMessagesAsync();

            if (!Console.KeyAvailable)
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    renderCancellation.Cancel();
                    await AwaitRenderTaskAsync(renderTask);
                    return input.Trim();
                case ConsoleKey.Backspace:
                    lock (sync)
                    {
                        if (input.Length > 0)
                        {
                            input = input[..^1];
                        }
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        lock (sync)
                        {
                            input += key.KeyChar;
                        }
                    }
                    break;
            }
        }

        renderCancellation.Cancel();
        await AwaitRenderTaskAsync(renderTask);
        return "/back";
    }

    private static async Task AwaitRenderTaskAsync(Task renderTask)
    {
        try
        {
            await renderTask;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
