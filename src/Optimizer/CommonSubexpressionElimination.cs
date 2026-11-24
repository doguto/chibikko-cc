using Chibikko.Common.Ir;
using Chibikko.Optimizer.Dag;

namespace Chibikko.Optimizer;

public sealed class CommonSubexpressionElimination : IOptimizationPass
{
    public void Run(Ir ir)
    {
        foreach (var block in ir.Functions.SelectMany(f => f.Blocks))
        {
            var dag = new BasicBlockDag();
            var instructions = block.Instructions
                .Select(q => Rewrite(dag, q))
                .OfType<Quadruple>()
                .ToList();

            block.Instructions.Clear();
            block.Instructions.AddRange(instructions);
        }
    }

    static Quadruple? Rewrite(BasicBlockDag dag, Quadruple quadruple)
    {
        if (quadruple.OperationType is OperationType.Param) return quadruple;

        if (quadruple.OperationType is OperationType.Call)
        {
            if (quadruple.Destination is Variable result) dag.Define(result, dag.Opaque(quadruple));
            return quadruple;
        }

        var destination = (Variable)quadruple.Destination!;
        if (quadruple.OperationType is OperationType.Assign)
        {
            dag.Define(destination, dag.NodeOf(quadruple.Source1!));
            return quadruple;
        }

        var node = dag.Interior(
            quadruple.OperationType,
            quadruple.Source1 is { } source1 ? dag.NodeOf(source1) : null,
            quadruple.Source2 is { } source2 ? dag.NodeOf(source2) : null
        );
        var holder = dag.Holder(node);
        dag.Define(destination, node);

        if (holder is null) return quadruple;
        if (ReferenceEquals(holder, destination)) return null;
        return new Quadruple
        {
            OperationType = OperationType.Assign,
            Destination = destination,
            Source1 = holder,
        };
    }
}
