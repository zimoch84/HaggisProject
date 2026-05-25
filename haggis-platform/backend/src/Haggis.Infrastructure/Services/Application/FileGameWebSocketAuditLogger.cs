using System.Text.Json;

namespace Haggis.Infrastructure.Services.Application;

public sealed class FileGameWebSocketAuditLogger : IGameWebSocketAuditLogger
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly object _sync = new();
    private readonly string _logFilePath;

    public FileGameWebSocketAuditLogger(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        var configuredPath = configuration["GameWebSocketAudit:Path"];
        _logFilePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(
                hostEnvironment.ContentRootPath,
                string.IsNullOrWhiteSpace(configuredPath) ? "logs\\game-websocket-outbound.log" : configuredPath);

        if (configuration.GetValue("GameWebSocketAudit:ClearOnStartup", true))
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_logFilePath, string.Empty);
        }
    }

    public void Log(GameWebSocketAuditEntry entry)
    {
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine;
        lock (_sync)
        {
            File.AppendAllText(_logFilePath, line);
        }
    }
}
