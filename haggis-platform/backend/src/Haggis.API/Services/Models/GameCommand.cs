using System.Text.Json;

namespace Haggis.API.Services.Models;

public sealed record GameCommand(string Type, string PlayerId, JsonElement Payload);
