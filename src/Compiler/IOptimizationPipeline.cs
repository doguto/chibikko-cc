using Chibikko.Common.Ir;
using Chibikko.Common;

namespace Chibikko;

public interface IOptimizationPipeline
{
    Ir Run(Ir ir);
}
