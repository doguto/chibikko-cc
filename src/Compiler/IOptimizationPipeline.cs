using Chibikko.Common.Ir;

namespace Chibikko;

public interface IOptimizationPipeline
{
    Module Run(Module module);
}
