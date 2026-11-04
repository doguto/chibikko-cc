using Chibikko.Common.Ir;
using Chibikko.Common;

namespace Chibikko;

public interface IBackendPipeline
{
    string Run(Ir ir);
}
