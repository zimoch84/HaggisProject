using System.Collections.Generic;

namespace MonteCarlo
{
    public interface IAction { }

    public interface IPlayer { }

    public interface IState<TPlayer, TAction>
    {
        IState<TPlayer, TAction> Clone();

        TPlayer CurrentPlayer { get; }

        IList<TAction> Actions { get; }

        void ApplyAction(TAction action);

        double GetResult(TPlayer forPlayer);
    }
    public interface IMctsNode<TAction> where TAction : IAction
    {
        TAction Action { get; }

        int NumRuns { get; }

        double NumWins { get; }
    }
}
