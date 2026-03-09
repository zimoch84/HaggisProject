namespace Haggis.Infrastructure.Services.Engine.Loop;

public interface IMoveRuleValidator<TState, TMove, in TCommand>
{
    MoveValidationResult Validate(TState state, TCommand command, TMove move, IReadOnlyList<TMove> legalMoves);
}
