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
            HashSet<Variable> use = new(ReferenceEqualityComparer.Instance);
            HashSet<Variable> def = new(ReferenceEqualityComparer.Instance);
            foreach (var quadruple in block.Instructions)
            {
                use.UnionWith(quadruple.Uses().Where(v => !def.Contains(v)));
                def.UnionWith(quadruple.Defs());
            }
            use.UnionWith(block.Terminator?.Uses().Where(v => !def.Contains(v)) ?? []);

            uses[block] = use;
            defs[block] = def;
            liveIn[block] = new(ReferenceEqualityComparer.Instance);
            liveOut[block] = new(ReferenceEqualityComparer.Instance);
        }

        bool changed;
        do
        {
            changed = false;
            for (var i = function.Blocks.Count - 1; i >= 0; i--)
            {
                var block = function.Blocks[i];
                foreach (var successor in block.Successors) liveOut[block].UnionWith(liveIn[successor]);

                HashSet<Variable> newIn = new(liveOut[block], ReferenceEqualityComparer.Instance);
                newIn.ExceptWith(defs[block]);
                newIn.UnionWith(uses[block]);

                if (newIn.SetEquals(liveIn[block])) continue;
                liveIn[block] = newIn;
                changed = true;
            }
        } while (changed);

        Dictionary<Quadruple, HashSet<Variable>> instructionLiveOut = new(ReferenceEqualityComparer.Instance);
        foreach (var block in function.Blocks)
        {
            HashSet<Variable> live = new(liveOut[block], ReferenceEqualityComparer.Instance);
            live.UnionWith(block.Terminator?.Uses() ?? []);
            for (var i = block.Instructions.Count - 1; i >= 0; i--)
            {
                var quadruple = block.Instructions[i];
                instructionLiveOut[quadruple] = new(live, ReferenceEqualityComparer.Instance);
                live.ExceptWith(quadruple.Defs());
                live.UnionWith(quadruple.Uses());
            }
        }

        return new Liveness(
            liveIn,
            liveOut,
            instructionLiveOut
        );
    }
}
