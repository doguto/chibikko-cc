using Chibikko.Common.Ir;

namespace Chibikko;

public sealed class Compiler(
    IFrontendPipeline frontend,
    IOptimizationPipeline optimization,
    IBackendPipeline backend
)
{
    public string Compile(string source)
    {
        var ir = frontend.Run(source);
        var optimized = optimization.Run(ir);
        var result = backend.Run(optimized);
        return result;
    }

    public string DumpIr(string source) => IrDumper.Dump(frontend.Run(source));
}
