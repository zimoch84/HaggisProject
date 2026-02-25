using System.Text.Json;

namespace Haggis.Infrastructure.Services.Models;

public sealed record StartGameClientMessage(string Type, string PlayerId, JsonElement Payload);
