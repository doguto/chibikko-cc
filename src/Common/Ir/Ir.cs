namespace Chibikko.Common.Ir;

public sealed class Module
{
    public List<Function> Functions { get; } = [];
}

public sealed class Function(string name)
{
    public string Name { get; } = name;
    public List<BasicBlock> Blocks { get; } = [];
}

public sealed class BasicBlock(string name)
{
    public string Name { get; } = name;
    public List<Instruction> Instructions { get; } = [];
}

public abstract class Instruction;
