using Chibikko.Common.Ir;

namespace Chibikko.Frontend;

public sealed class FrontendPipeline : IFrontendPipeline
{
    public Module Run(string source)
    {
        var lexer = new Lexer(source);
        var semanticAnalyzer = new SemanticAnalyzer(source);
        var parser = new Parser(lexer, semanticAnalyzer);

        var unit = parser.Parse();

        // unit -> IrGenerator
        throw new NotImplementedException("IrGenerator");
    }
}
