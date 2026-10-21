using Chibikko.Common.Ir;

namespace Chibikko;

public interface IBackendPipeline
{
    string Run(Module module);
}
