using Chibikko.Common.Ir;
using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class FrontendPipeline : IFrontendPipeline
{
    public Ir Run(string source)
    {
        var lexer = new Lexer(source);
        var semanticAnalyzer = new SemanticAnalyzer();

        var parser = new Parser(lexer, semanticAnalyzer);
        var unit = parser.Parse();

        var irGenerator = new IrGenerator();
        var ir = irGenerator.Generate(unit);

        return ir;
    }
}
