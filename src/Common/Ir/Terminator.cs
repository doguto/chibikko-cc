namespace Chibikko.Common.Ir;

public abstract record Terminator
{
    public abstract IEnumerable<BasicBlock> Successors { get; }
}

public sealed record Jump(BasicBlock Target) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [Target];
}

public sealed record Branch(Operand Condition, BasicBlock Then, BasicBlock Else) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [Then, Else];
}

public sealed record Return(Operand? Value) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [];
}
