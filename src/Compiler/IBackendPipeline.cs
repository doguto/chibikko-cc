using Chibikko.Common;

namespace Chibikko;

public interface IBackendPipeline
{
    string Run(Ir ir);
}
