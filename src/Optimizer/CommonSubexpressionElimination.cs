using Chibikko.Common.Ir;
using Chibikko.Optimizer.Dag;

namespace Chibikko.Optimizer;

public sealed class CommonSubexpressionElimination : IOptimizationPass
{
    public void Run(Ir ir)
    {
        foreach (var function in ir.Functions)
        {
            foreach (var block in function.Blocks)
            {
                Run(block);
            }
        }
    }

    static void Run(BasicBlock block)
    {
        var dag = new BasicBlockDag();
        List<Quadruple> instructions = [];

        foreach (var quadruple in block.Instructions)
        {
            var rewritten = Visit(dag, quadruple);
            if (rewritten is not null) instructions.Add(rewritten);
        }

        block.Instructions.Clear();
        block.Instructions.AddRange(instructions);
    }

    static Quadruple? Visit(BasicBlockDag dag, Quadruple quadruple)
    {
        switch (quadruple.OperationType)
        {
            case OperationType.Assign:
                dag.Define(Destination(quadruple), dag.NodeOf(quadruple.Source1!));
                return quadruple;

            case OperationType.Param:
                return quadruple;

            case OperationType.Call:
                if (quadruple.Destination is Variable result) dag.Define(result, dag.CreateOpaque(quadruple));
                return quadruple;

            default:
                return VisitExpression(dag, quadruple);
        }
    }

    static Quadruple? VisitExpression(BasicBlockDag dag, Quadruple quadruple)
    {
        var destination = Destination(quadruple);
        var left = quadruple.Source1 is { } source1 ? dag.NodeOf(source1) : null;
        var right = quadruple.Source2 is { } source2 ? dag.NodeOf(source2) : null;

        if (dag.Find(quadruple.OperationType, left, right) is { } existing && dag.Holder(existing) is { } holder)
        {
            if (ReferenceEquals(holder, destination)) return null;

            dag.Define(destination, existing);
            return new Quadruple
            {
                OperationType = OperationType.Assign,
                Destination = destination,
                Source1 = holder,
            };
        }

        dag.Define(destination, dag.CreateInterior(quadruple.OperationType, left, right));
        return quadruple;
    }

    static Variable Destination(Quadruple quadruple) =>
        quadruple.Destination as Variable
            ?? throw new InvalidOperationException($"{quadruple.OperationType} has no destination variable");
}
