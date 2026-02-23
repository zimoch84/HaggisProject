namespace Haggis.Application.Engine.Loop;

public sealed record GameLoopExecutionResult<TState, TMove>(
    bool Handled,
    bool IsStart,
    TState? State,
    TMove AppliedMove,
    bool HasAppliedMove)
{
    public static GameLoopExecutionResult<TState, TMove> NotHandled() =>
        new(false, false, default, default!, false);
}
