using Chibikko;
using Chibikko.Backend;
using Chibikko.Common;
using Chibikko.Frontend;
using Chibikko.Optimizer;


if (args.Length != 1)
{
    Console.Error.WriteLine("usage: chibikko <file>");
    return 2;
}

var source = File.ReadAllText(args[0]);

try
{
    var compiler = new Compiler(
        new FrontendPipeline(),
        new OptimizationPipeline(),
        new BackendPipeline()
    );

    Console.Write(compiler.Compile(source));
    return 0;
}
catch (CompileException e)
{
    Console.Error.WriteLine($"{args[0]}:{e.Offset}: error: {e.Message}");
    return 1;
}
