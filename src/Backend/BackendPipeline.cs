using Chibikko.Common.Ir;
using Chibikko.Common;

namespace Chibikko.Backend;

public sealed class BackendPipeline : IBackendPipeline
{
    public string Run(Ir ir)
    {
        var bin = new CodeGenerator().Emit(ir);
        return bin;
    }
}
