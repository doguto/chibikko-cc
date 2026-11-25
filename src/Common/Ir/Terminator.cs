namespace Chibikko.Common.Ir;

public abstract record Terminator
{
    public abstract IEnumerable<BasicBlock> Successors { get; }
    public abstract IEnumerable<Variable> Uses { get; }
}

public sealed record Jump(BasicBlock Target) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [Target];
    public override IEnumerable<Variable> Uses => [];
}

public sealed record Branch(Operand Condition, BasicBlock Then, BasicBlock Else) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [Then, Else];
    public override IEnumerable<Variable> Uses => new[] { Condition }.OfType<Variable>();
}

public sealed record Return(Operand? Value) : Terminator
{
    public override IEnumerable<BasicBlock> Successors => [];
    public override IEnumerable<Variable> Uses => new[] { Value }.OfType<Variable>();
}
