namespace Haggis.Infrastructure.Services.Application;

public interface IGameWebSocketAuditLogger
{
    void Log(GameWebSocketAuditEntry entry);
}
