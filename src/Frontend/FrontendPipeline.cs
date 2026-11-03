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

        throw new NotImplementedException("IrGenerator");
    }
}
