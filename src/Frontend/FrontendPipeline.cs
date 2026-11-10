using Chibikko.Common.Ir;
using Chibikko.Common;
using Chibikko.Frontend.Type;

namespace Chibikko.Frontend;

public sealed class FrontendPipeline : IFrontendPipeline
{
    public Ir Run(string source)
    {
        var unit = Parse(source);

        var irGenerator = new IrGenerator();
        var ir = irGenerator.Generate(unit);

        return ir;
    }

    public string DumpAst(string source) => AstDumper.Dump(Parse(source));

    static TranslationUnit Parse(string source)
    {
        var lexer = new Lexer(source);
        var semanticAnalyzer = new SemanticAnalyzer();

        var parser = new Parser(lexer, semanticAnalyzer);
        return parser.Parse();
    }
}
