namespace Game.Core.Engine.Loop;

public interface IAiMoveStrategy<TState, TMove>
{
    TMove ChooseMove(TState state, IReadOnlyList<TMove> legalMoves);
}
