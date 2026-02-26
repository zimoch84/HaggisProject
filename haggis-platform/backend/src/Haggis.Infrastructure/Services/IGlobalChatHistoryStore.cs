using Haggis.Infrastructure.Dtos.Chat;

namespace Haggis.Infrastructure.Services;

public interface IGlobalChatHistoryStore
{
    void Append(ChatMessage message);
    IReadOnlyList<ChatMessage> GetRecent(int limit = 100);
}
