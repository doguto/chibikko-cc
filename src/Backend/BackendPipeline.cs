using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public sealed class BackendPipeline : IBackendPipeline
{
    public string Run(Module module) => new CodeGenerator().Emit(module);
}
