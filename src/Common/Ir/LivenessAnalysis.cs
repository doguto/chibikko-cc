namespace Chibikko.Common.Ir;

public sealed class Liveness(
    Dictionary<BasicBlock, HashSet<Variable>> liveIn,
    Dictionary<BasicBlock, HashSet<Variable>> liveOut,
    Dictionary<Quadruple, HashSet<Variable>> instructionLiveOut
)
{
    public IReadOnlySet<Variable> In(BasicBlock block) => liveIn[block];

    public IReadOnlySet<Variable> Out(BasicBlock block) => liveOut[block];

    public IReadOnlySet<Variable> Out(Quadruple quadruple) => instructionLiveOut[quadruple];
}

public static class LivenessAnalysis
{
    public static Liveness Analyze(IrFunction function)
    {
        Dictionary<BasicBlock, HashSet<Variable>> uses = new(ReferenceEqualityComparer.Instance);
        Dictionary<BasicBlock, HashSet<Variable>> defs = new(ReferenceEqualityComparer.Instance);

        Dictionary<BasicBlock, HashSet<Variable>> liveIn = new(ReferenceEqualityComparer.Instance);
        Dictionary<BasicBlock, HashSet<Variable>> liveOut = new(ReferenceEqualityComparer.Instance);

        foreach (var block in function.Blocks)
        {
            // この基本ブロックの外で定義して、この基本ブロック内で使用される変数
            HashSet<Variable> use = new(ReferenceEqualityComparer.Instance);

            // この基本ブロック内で定義される変数
            HashSet<Variable> def = new(ReferenceEqualityComparer.Instance);

            foreach (var quadruple in block.Instructions)
            {
                use.UnionWith(quadruple.Uses.Where(v => !def.Contains(v)));
                if (quadruple.Def is { } destination) def.Add(destination);
            }
            // 基本ブロック末尾の Jump に条件がある場合は、その条件で用いる変数も use に含める
            use.UnionWith(block.Terminator?.Uses.Where(v => !def.Contains(v)) ?? []);

            uses[block] = use;
            defs[block] = def;
            liveIn[block] = new(ReferenceEqualityComparer.Instance);
            liveOut[block] = new(ReferenceEqualityComparer.Instance);
        }

        // 解析を回して生存解析の結果に変化が出なくなるまでループする
        bool changed;
        do
        {
            changed = false;

            // 基本ブロックを後ろから辿る
            for (var i = function.Blocks.Count - 1; i >= 0; i--)
            {
                var block = function.Blocks[i];
                foreach (var successor in block.Successors) liveOut[block].UnionWith(liveIn[successor]);

                HashSet<Variable> newIn = new(liveOut[block], ReferenceEqualityComparer.Instance);
                newIn.ExceptWith(defs[block]); // 基本ブロック内で定義され直すので、生存から省く
                newIn.UnionWith(uses[block]);  // uses の変数は外から入ってくる必要がある

                if (newIn.SetEquals(liveIn[block])) continue;
                liveIn[block] = newIn;
                changed = true;
            }
        } while (changed);

        Dictionary<Quadruple, HashSet<Variable>> instructionLiveOut = new(ReferenceEqualityComparer.Instance);
        foreach (var block in function.Blocks)
        {
            HashSet<Variable> live = new(liveOut[block], ReferenceEqualityComparer.Instance);
            live.UnionWith(block.Terminator?.Uses ?? []);
            for (var i = block.Instructions.Count - 1; i >= 0; i--)
            {
                var quadruple = block.Instructions[i];
                instructionLiveOut[quadruple] = new(live, ReferenceEqualityComparer.Instance);
                if (quadruple.Def is { } def) live.Remove(def);
                live.UnionWith(quadruple.Uses); // ここで使用されるためには、基本ブロックのこれより前方では生存している必要がある
            }
        }

        return new Liveness(
            liveIn,
            liveOut,
            instructionLiveOut
        );
    }
}
