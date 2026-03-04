using Haggis.Infrastructure.Dtos.Chat;

namespace Haggis.Infrastructure.Services.Interfaces;

public interface IGlobalChatHistoryStore
{
    void Append(ChatMessage message);
    IReadOnlyList<ChatMessage> GetRecent(int limit = 100);
}
