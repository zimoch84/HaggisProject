using System.Text.Json;

namespace Haggis.Infrastructure.Services.Models;

public sealed record GameCommand(string Type, string PlayerId, JsonElement Payload);
