namespace Haggis.Application.Engine.Loop;

public sealed record MoveValidationResult(bool IsValid, string? Error = null)
{
    public static MoveValidationResult Success() => new(true);

    public static MoveValidationResult Failure(string error) => new(false, error);
}
