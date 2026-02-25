using System.Collections.Concurrent;
using Haggis.Infrastructure.Dtos.Chat;

namespace Haggis.Infrastructure.Services;

public sealed class InMemoryGlobalChatHistoryStore : IGlobalChatHistoryStore
{
    private readonly ConcurrentQueue<ChatMessage> _messages = new();
    private const int MaxMessages = 500;

    public void Append(ChatMessage message)
    {
        _messages.Enqueue(message);
        while (_messages.Count > MaxMessages && _messages.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<ChatMessage> GetRecent(int limit = 100)
    {
        if (limit <= 0)
        {
            return Array.Empty<ChatMessage>();
        }

        var snapshot = _messages.ToArray();
        if (snapshot.Length <= limit)
        {
            return snapshot;
        }

        return snapshot[^limit..];
    }
}
