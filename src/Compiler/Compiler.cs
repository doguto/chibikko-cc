namespace Chibikko;

public sealed class Compiler(
    IFrontendPipeline frontend,
    IOptimizationPipeline optimization,
    IBackendPipeline backend
)
{
    public string Compile(string source)
    {
        var module = frontend.Run(source);
        var optimized = optimization.Run(module);
        return backend.Run(optimized);
    }
}
