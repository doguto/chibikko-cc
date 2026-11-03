using Chibikko.Common;

namespace Chibikko.Backend;

public sealed class BackendPipeline : IBackendPipeline
{
    public string Run(Ir ir) => new CodeGenerator().Emit(ir);
}
