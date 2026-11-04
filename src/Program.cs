using Chibikko;
using Chibikko.Backend;
using Chibikko.Common;
using Chibikko.Frontend;
using Chibikko.Optimizer;


var dumpIr = args.Length == 2 && args[0] == "--dump-ir";
if (args.Length != 1 && !dumpIr)
{
    Console.Error.WriteLine("usage: chibikko [--dump-ir] <file>");
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

    Console.Write(dumpIr ? compiler.DumpIr(source) : compiler.Compile(source));
    return 0;
}
catch (CompileException e)
{
    Console.Error.WriteLine($"{path}:{e.Offset}: error: {e.Message}");
    return 1;
}
