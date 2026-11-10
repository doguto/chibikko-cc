using Chibikko;
using Chibikko.Backend;
using Chibikko.Common;
using Chibikko.Frontend;
using Chibikko.Optimizer;


var option = args.Length == 2 ? args[0] : null;
if (args.Length is not (1 or 2) || option is not (null or "--dump-ir" or "--dump-ast"))
{
    Console.Error.WriteLine("usage: chibikko [--dump-ast | --dump-ir] <file>");
    return 2;
}

var path = args[^1];
var source = File.ReadAllText(path);

try
{
    var compiler = new Compiler(
        new FrontendPipeline(),
        new OptimizationPipeline(),
        new BackendPipeline()
    );

    Console.Write(option switch
    {
        "--dump-ast" => compiler.DumpAst(source),
        "--dump-ir" => compiler.DumpIr(source),
        _ => compiler.Compile(source),
    });
    return 0;
}
catch (CompileException e)
{
    Console.Error.WriteLine($"{path}:{e.Offset}: error: {e.Message}");
    return 1;
}
