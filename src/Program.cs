using Chibikko.Common;

namespace Chibikko;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: chibikko <file>");
            return 2;
        }

        var source = File.ReadAllText(args[0]);

        try
        {
            Console.Write(new Compiler().Compile(source));
            return 0;
        }
        catch (CompileException e)
        {
            Console.Error.WriteLine($"{args[0]}:{e.Offset}: error: {e.Message}");
            return 1;
        }
    }
}
