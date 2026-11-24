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

public sealed class BlockNextUse(
    Dictionary<Quadruple, NextUseInfo> instructions,
    NextUseInfo? terminator,
    Dictionary<Variable, List<int>> usePositions
)
{
    public NextUseInfo Of(Quadruple quadruple) => instructions[quadruple];

    public NextUseInfo OfTerminator() =>
        terminator ?? throw new InvalidOperationException("block has no terminator");

    public int? NextUseAfter(Variable variable, int position)
    {
        if (!usePositions.TryGetValue(variable, out var positions)) return null;

        var next = positions.FirstOrDefault(p => p > position, -1);
        return next >= 0 ? next : null;
    }
}

// TODO: 現状はレジスタ割り当て専用のため Backend に置いている
// ブロック出口で名前付き変数を生存とみなす仮定はコード生成の書き戻し方針に依存する
// 最適化パスから利用する場合や CFG 全体の生存変数解析を導入する場合は Common への移動を検討する
public static class NextUseAnalysis
{
    public static BlockNextUse Analyze(BasicBlock block)
    {
        Dictionary<Variable, NextUse> table = new(ReferenceEqualityComparer.Instance);
        Dictionary<Quadruple, NextUseInfo> instructions = new(ReferenceEqualityComparer.Instance);
        NextUseInfo? terminator = null;
        Dictionary<Variable, List<int>> usePositions = new(ReferenceEqualityComparer.Instance);

        var operands = block.Instructions.SelectMany(q => q.Uses().Concat(q.Defs())).Concat(block.Terminator?.Uses() ?? []);
        foreach (var operand in operands) table[operand] = new NextUse(!operand.IsTemporary, null);

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

        for (var i = 0; i < block.Instructions.Count; i++)
        {
            foreach (var use in block.Instructions[i].Uses()) AddUsePosition(usePositions, use, i);
        }
        foreach (var use in block.Terminator?.Uses() ?? [])
        {
            AddUsePosition(usePositions, use, block.Instructions.Count);
        }

        return new BlockNextUse(instructions, terminator, usePositions);
    }

    static NextUseInfo Record(Dictionary<Variable, NextUse> table, IEnumerable<Variable> operands)
    {
        Dictionary<Variable, NextUse> snapshot = new(ReferenceEqualityComparer.Instance);
        foreach (var operand in operands) snapshot[operand] = table[operand];
        return new NextUseInfo(snapshot);
    }

    static void AddUsePosition(Dictionary<Variable, List<int>> usePositions, Variable variable, int position)
    {
        if (!usePositions.TryGetValue(variable, out var positions))
        {
            positions = [];
            usePositions[variable] = positions;
        }
        if (positions.LastOrDefault(-1) != position) positions.Add(position);
    }

    static void MarkUsed(Dictionary<Variable, NextUse> table, List<Variable> uses, int position)
    {
        foreach (var use in uses) table[use] = new NextUse(true, position);
    }
}
