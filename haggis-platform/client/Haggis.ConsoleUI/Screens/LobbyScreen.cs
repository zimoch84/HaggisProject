public sealed class LobbyScreen
{
    private readonly StaticConsoleUI _ui;

    public LobbyScreen(StaticConsoleUI ui)
    {
        _ui = ui;
    }

    public async Task<string> ShowAsync(
        string playerId,
        LobbyState state,
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken)
    {
        var sync = new object();
        var input = string.Empty;
        var result = "/quit";

        using var renderCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renderTask = Task.Run(async () =>
        {
            while (!renderCancellation.IsCancellationRequested)
            {
                lock (sync)
                {
                    _ui.RenderLobby(playerId, state, input);
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
                    lock (sync)
                    {
                        result = input;
                    }
                    renderCancellation.Cancel();
                    await AwaitRenderTaskAsync(renderTask);
                    return result;
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
        return result;
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
