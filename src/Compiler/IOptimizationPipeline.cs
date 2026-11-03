using Chibikko.Common;

namespace Chibikko;

public interface IOptimizationPipeline
{
    Ir Run(Ir ir);
}
