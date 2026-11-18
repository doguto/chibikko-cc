using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public readonly record struct NextUse(bool IsLive, int? Position);

public sealed class NextUseInfo(IReadOnlyDictionary<Variable, NextUse> operands)
{
    public NextUse Of(Variable variable) =>
        operands.TryGetValue(variable, out var nextUse)
            ? nextUse
            : throw new InvalidOperationException($"variable is not an operand: {variable.Name ?? "<temp>"}");
}

public sealed class BlockNextUse(Dictionary<Quadruple, NextUseInfo> instructions, NextUseInfo? terminator)
{
    public NextUseInfo Of(Quadruple quadruple) => instructions[quadruple];

    public NextUseInfo OfTerminator() =>
        terminator ?? throw new InvalidOperationException("block has no terminator");
}

public static class NextUseAnalysis
{
    public static BlockNextUse Analyze(BasicBlock block)
    {
        Dictionary<Variable, NextUse> table = new(ReferenceEqualityComparer.Instance);
        Dictionary<Quadruple, NextUseInfo> instructions = new(ReferenceEqualityComparer.Instance);
        NextUseInfo? terminator = null;

        var position = block.Instructions.Count;
        if (block.Terminator is { } blockTerminator)
        {
            var uses = blockTerminator.Uses().ToList();
            terminator = Record(table, uses);
            MarkUsed(table, uses, position);
        }

        for (position--; position >= 0; position--)
        {
            var quadruple = block.Instructions[position];
            var uses = quadruple.Uses().ToList();
            var defs = quadruple.Defs().ToList();

            instructions[quadruple] = Record(table, defs.Concat(uses));
            foreach (var def in defs) table[def] = new NextUse(false, null);
            MarkUsed(table, uses, position);
        }

        var crossing = table.Where(x => x.Key.IsTemporary && x.Value.IsLive).Select(x => x.Key).FirstOrDefault();
        if (crossing is not null)
        {
            throw new InvalidOperationException($"temporary variable is live on entry to block: {block.Name}");
        }

        return new BlockNextUse(instructions, terminator);
    }

    static NextUseInfo Record(Dictionary<Variable, NextUse> table, IEnumerable<Variable> operands)
    {
        Dictionary<Variable, NextUse> snapshot = new(ReferenceEqualityComparer.Instance);
        foreach (var operand in operands) snapshot[operand] = Lookup(table, operand);
        return new NextUseInfo(snapshot);
    }

    static void MarkUsed(Dictionary<Variable, NextUse> table, List<Variable> uses, int position)
    {
        foreach (var use in uses) table[use] = new NextUse(true, position);
    }

    static NextUse Lookup(Dictionary<Variable, NextUse> table, Variable variable) =>
        table.TryGetValue(variable, out var nextUse) ? nextUse : new NextUse(!variable.IsTemporary, null);
}
