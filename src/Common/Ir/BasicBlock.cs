namespace Chibikko.Common.Ir;

public sealed class BasicBlock(string name)
{
    public string Name { get; } = name;
    public List<Quadruple> Instructions { get; } = [];
    public Terminator? Terminator { get; private set; }
    public List<BasicBlock> Predecessors { get; } = [];
    public IEnumerable<BasicBlock> Successors => Terminator?.Successors ?? [];

    public void SetTerminator(Terminator? terminator)
    {
        foreach (var successor in Successors) successor.Predecessors.Remove(this);
        Terminator = terminator;
        foreach (var successor in Successors) successor.Predecessors.Add(this);
    }
}
