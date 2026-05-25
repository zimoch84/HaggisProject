using System.Text.Json;

namespace Haggis.Infrastructure.Services.Application;

public sealed class FileGameCommandAuditLogger : IGameCommandAuditLogger
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly object _sync = new();
    private readonly string _logFilePath;

    public FileGameCommandAuditLogger(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        var configuredPath = configuration["GameCommandAudit:Path"];
        _logFilePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(
                hostEnvironment.ContentRootPath,
                string.IsNullOrWhiteSpace(configuredPath) ? "logs\\game-commands.log" : configuredPath);

        if (configuration.GetValue("GameCommandAudit:ClearOnStartup", true))
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_logFilePath, string.Empty);
        }
    }

    public void Log(GameCommandAuditEntry entry)
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
