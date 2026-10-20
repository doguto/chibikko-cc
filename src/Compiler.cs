using Chibikko.Backend;
using Chibikko.Common.Ir;
using Chibikko.Optimizer;

namespace Chibikko;

public sealed class Compiler
{
    public PassManager Passes { get; } = new();

    public string Compile(string source)
    {
        var module = LowerToIr(source);
        var optimized = Passes.Run(module);
        return new CodeGen().Emit(optimized);
    }

    private static Module LowerToIr(string source)
    {
        // Lexer -> Parser -> Sema -> IrGen
        throw new NotImplementedException("frontend");
    }
}
