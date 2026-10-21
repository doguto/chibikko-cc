using Chibikko.Common.Ir;

namespace Chibikko.Optimizer;

public interface IOptimizationPass
{
    void Run(Module module);
}

public sealed class OptimizationPipeline : IOptimizationPipeline
{
    private readonly List<IOptimizationPass> _optimizations = [];

    public void Add(IOptimizationPass optimization) => _optimizations.Add(optimization);

    public Module Run(Module module)
    {
        foreach (var optimization in _optimizations)
        {
            optimization.Run(module);
        }
        return module;
    }
}
