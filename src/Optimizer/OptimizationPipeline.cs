using Chibikko.Common;

namespace Chibikko.Optimizer;

public interface IOptimizationPass
{
    void Run(Ir ir);
}

public sealed class OptimizationPipeline : IOptimizationPipeline
{
    readonly List<IOptimizationPass> optimizations = [];

    public void Add(IOptimizationPass optimization) => optimizations.Add(optimization);

    public Ir Run(Ir ir)
    {
        foreach (var optimization in optimizations)
        {
            optimization.Run(ir);
        }
        return ir;
    }
}
