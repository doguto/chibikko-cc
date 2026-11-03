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
        return backend.Run(optimized);
    }
}
