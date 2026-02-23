using System.Text.Json;

namespace Game.API.Services.Models;

public sealed record GameCommand(string Type, string PlayerId, JsonElement Payload);
