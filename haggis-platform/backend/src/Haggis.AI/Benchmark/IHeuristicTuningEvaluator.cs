using Haggis.AI.Strategies;

namespace Haggis.AI.Benchmark
{
    public interface IHeuristicTuningEvaluator
    {
        HeuristicTuningRunResult Evaluate(HeuristicTuningBatchOptions batchOptions, HeuristicOptions heuristicOptions);
    }
}
