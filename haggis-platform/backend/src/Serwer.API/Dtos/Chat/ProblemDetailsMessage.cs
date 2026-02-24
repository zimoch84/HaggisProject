using System.Text.Json.Serialization;

namespace Serwer.API.Dtos.Chat;

public sealed record ProblemDetailsMessage(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("type")] string? Type = null,
    [property: JsonPropertyName("detail")] string? Detail = null,
    [property: JsonPropertyName("instance")] string? Instance = null,
    [property: JsonPropertyName("errors")] IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors = null);

