using Chibikko.Common.Ir;

namespace Chibikko.Frontend;

public sealed class FrontendPipeline : IFrontendPipeline
{
    public Module Run(string source)
    {
        var syntax = new Parser(new Lexer(source)).Parse();
        var analyzed = new SemanticAnalyzer(source).Analyze(syntax);

        // analyzed -> IrGenerator
        throw new NotImplementedException("IrGenerator");
    }
}
