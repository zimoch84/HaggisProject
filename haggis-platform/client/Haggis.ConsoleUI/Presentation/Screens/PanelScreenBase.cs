public abstract class PanelScreenBase
{
    private readonly List<PanelRegionBase> _panels = new();
    private readonly PanelRegionInputBase _inputPanel;
    private readonly object _sync = new();
    private bool _isFirstRender = true;

    protected PanelScreenBase(PanelRegionInputBase inputPanel)
    {
        _inputPanel = inputPanel;
        _panels.Add(inputPanel);
    }

    protected PanelRegionInputBase InputPanel => _inputPanel;

    protected void AddPanel(PanelRegionBase panel)
    {
        _panels.Add(panel);
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        lock (_sync)
        {
            _inputPanel.HandleKey(key);
        }
    }

    public bool TryConsumeCommand(out string command)
    {
        lock (_sync)
        {
            return _inputPanel.TryConsumeSubmitted(out command);
        }
    }

    public bool TryConsumeCommand<TCommand>(out TCommand command)
        where TCommand : class, IInputAction
    {
        lock (_sync)
        {
            if (!_inputPanel.TryConsumeSubmitted(out var rawCommand))
            {
                command = default!;
                return false;
            }

            var parsed = _inputPanel.ParseCommand(rawCommand);
            if (parsed is TCommand typedCommand)
            {
                command = typedCommand;
                return true;
            }

            throw new InvalidOperationException(
                $"Input panel returned command type '{parsed?.GetType().Name ?? "null"}', expected '{typeof(TCommand).Name}'.");
        }
    }

    public void Render()
    {
        lock (_sync)
        {
            PreparePanels();
            if (_isFirstRender)
            {
                SafeClear();
                _isFirstRender = false;
            }

            SafeSetCursorVisible(false);

            foreach (var panel in _panels)
            {
                panel.DrawPanel();
            }
        }
    }

    public void RenderInputPanel()
    {
        lock (_sync)
        {
            SafeSetCursorVisible(false);
            _inputPanel.DrawPanel();
        }
    }

    protected async Task<string> ReadCommandCoreAsync(
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken,
        string cancelCommand)
    {
        Render();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var updated = await pumpMessagesAsync();
            if (updated)
            {
                Render();
            }

            if (!_inputPanel.TryReadKey(out var key))
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            HandleKey(key);
            if (TryConsumeCommand(out var command))
            {
                return command;
            }

            RenderInputPanel();
        }

        return cancelCommand;
    }

    protected async Task<TCommand> ReadCommandCoreAsync<TCommand>(
        Func<Task<bool>> pumpMessagesAsync,
        CancellationToken cancellationToken,
        TCommand cancelCommand)
        where TCommand : class, IInputAction
    {
        Render();

        while (!cancellationToken.IsCancellationRequested)
        {
            var updated = await pumpMessagesAsync();
            if (updated)
            {
                Render();
            }

            if (!_inputPanel.TryReadKey(out var key))
            {
                await Task.Delay(50, cancellationToken);
                continue;
            }

            HandleKey(key);
            if (TryConsumeCommand<TCommand>(out var command))
            {
                return command;
            }

            RenderInputPanel();
        }

        return cancelCommand;
    }

    protected abstract void PreparePanels();

    private static void SafeClear()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
        }
    }

    private static void SafeSetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch
        {
        }
    }
}
