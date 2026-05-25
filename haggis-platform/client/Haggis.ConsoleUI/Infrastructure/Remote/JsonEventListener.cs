using System.Threading.Channels;
using System.Text.Json;

public sealed class JsonEventListener : IAsyncDisposable
{
    private readonly Channel<JsonDocument> _channel = Channel.CreateUnbounded<JsonDocument>();
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly Task _listenerTask;

    public JsonEventListener(Func<CancellationToken, Task<JsonDocument?>> receiveAsync)
    {
        _listenerTask = Task.Run(async () =>
        {
            while (!_cancellationSource.IsCancellationRequested)
            {
                var message = await receiveAsync(_cancellationSource.Token);
                if (message is null)
                {
                    break;
                }

                await _channel.Writer.WriteAsync(message, _cancellationSource.Token);
            }

            _channel.Writer.TryComplete();
        }, _cancellationSource.Token);
    }

    public bool TryRead(out JsonDocument message)
    {
        return _channel.Reader.TryRead(out message!);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationSource.Cancel();

        try
        {
            await _listenerTask;
        }
        catch
        {
        }

        _cancellationSource.Dispose();
    }
}
