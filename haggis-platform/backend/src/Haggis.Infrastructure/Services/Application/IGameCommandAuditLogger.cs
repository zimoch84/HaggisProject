namespace Haggis.Infrastructure.Services.Application;

public interface IGameCommandAuditLogger
{
    void Log(GameCommandAuditEntry entry);
}
