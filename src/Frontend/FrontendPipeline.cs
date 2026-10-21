using Chibikko.Common.Ir;

namespace Chibikko.Frontend;

public sealed class FrontendPipeline : IFrontendPipeline
{
    public Module Run(string source)
    {
        // Lexer -> Parser -> SemanticAnalyzer -> IrGenerator
        throw new NotImplementedException("frontend");
    }
}
