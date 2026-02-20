using System.Text.Json;

namespace Game.API.Services.Models;

public sealed record GameStateSnapshot(long Version, JsonElement Data, DateTimeOffset UpdatedAt)
{
    public static GameStateSnapshot Initial => new(0, EmptyObject(), DateTimeOffset.UtcNow);

    private static JsonElement EmptyObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }
}
