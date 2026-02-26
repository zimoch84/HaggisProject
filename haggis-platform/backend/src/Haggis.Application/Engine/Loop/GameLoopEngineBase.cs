using System.Collections.Concurrent;

namespace Haggis.Application.Engine.Loop;

public abstract class GameLoopEngineBase<TState, TMove, TCommand>
    where TState : class
{
    private readonly ConcurrentDictionary<string, TState> _states = new();

    protected abstract bool IsStartCommand(TCommand command);
    protected abstract bool IsNextMoveCommand(TCommand command);
    protected abstract TState CreateInitialState(string gameId, TCommand command);
    protected abstract IReadOnlyList<TMove> GetLegalMoves(TState state);
    protected abstract bool TryResolveMoveFromCommand(TState state, TCommand command, out TMove move);
    protected abstract bool ShouldUseAiMove(TState state, TCommand command);
    protected abstract TMove ResolveAiMove(TState state, IReadOnlyList<TMove> legalMoves);
    protected abstract MoveValidationResult ValidateMove(TState state, TCommand command, TMove move, IReadOnlyList<TMove> legalMoves);
    protected abstract void ApplyMove(TState state, TMove move);

    protected bool TryGetState(string gameId, out TState? state) => _states.TryGetValue(gameId, out state);

    protected void SetState(string gameId, TState state) => _states[gameId] = state;

    protected GameLoopExecutionResult<TState, TMove> Execute(string gameId, TCommand command)
    {
        if (IsStartCommand(command))
        {
            var initialized = CreateInitialState(gameId, command);
            SetState(gameId, initialized);
            return new GameLoopExecutionResult<TState, TMove>(
                Handled: true,
                IsStart: true,
                State: initialized,
                AppliedMove: default!,
                HasAppliedMove: false);
        }

        if (!IsNextMoveCommand(command))
        {
            return GameLoopExecutionResult<TState, TMove>.NotHandled();
        }

        if (!TryGetState(gameId, out var state) || state is null)
        {
            throw new InvalidOperationException($"Game state for '{gameId}' was not initialized.");
        }

        var legalMoves = GetLegalMoves(state);
        if (legalMoves.Count == 0)
        {
            throw new InvalidOperationException("No legal moves are available in current state.");
        }

        var hasCommandMove = TryResolveMoveFromCommand(state, command, out var move);
        if (!hasCommandMove)
        {
            if (!ShouldUseAiMove(state, command))
            {
                throw new InvalidOperationException("Move was not provided and AI move resolution is disabled.");
            }

            move = ResolveAiMove(state, legalMoves);
        }

        var validation = ValidateMove(state, command, move, legalMoves);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Error ?? "Move validation failed.");
        }

        ApplyMove(state, move);
        return new GameLoopExecutionResult<TState, TMove>(
            Handled: true,
            IsStart: false,
            State: state,
            AppliedMove: move,
            HasAppliedMove: true);
    }
}
