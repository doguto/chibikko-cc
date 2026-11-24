using System.Diagnostics;
using Chibikko.Common.Ir;

namespace Chibikko.Optimizer.Dag;

public sealed class BasicBlockDag
{
    readonly Dictionary<(OperationType, DagNode?, DagNode?), InteriorNode> interiors = new();
    readonly Dictionary<(IrType, object), ConstantLeaf> constants = new();
    readonly Dictionary<Variable, DagNode> current = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<DagNode, List<Variable>> labels = new(ReferenceEqualityComparer.Instance);
    int nextId;

    public DagNode NodeOf(Operand operand)
    {
        switch (operand)
        {
            case Variable variable:
                if (current.TryGetValue(variable, out var node)) return node;
                node = new VariableLeaf(nextId++, variable);
                Define(variable, node);
                return node;

            case Constant constant:
                (IrType, object) key = constant switch
                {
                    IntegerConstant integer => (integer.Type, integer.Value),
                    FloatConstant @float => (@float.Type, @float.Value),
                    _ => throw new UnreachableException($"Unknown constant: {constant.GetType().Name}"),
                };
                if (!constants.TryGetValue(key, out var leaf))
                {
                    leaf = new ConstantLeaf(nextId++, constant);
                    constants[key] = leaf;
                }
                return leaf;

            default:
                throw new UnreachableException($"Operand has no dag node: {operand.GetType().Name}");
        }
    }

    public InteriorNode Interior(
        OperationType operationType,
        DagNode? left,
        DagNode? right
    )
    {
        var isCommutative = operationType is
            OperationType.Add or
            OperationType.Multiply or
            OperationType.Equal or
            OperationType.NotEqual;
        if (isCommutative && left!.Id > right!.Id) (left, right) = (right, left);

        if (!interiors.TryGetValue((operationType, left, right), out var node))
        {
            node = new InteriorNode(
                nextId++,
                operationType,
                left,
                right
            );
            interiors[(operationType, left, right)] = node;
        }
        return node;
    }

    public OpaqueNode Opaque(Quadruple quadruple) => new(nextId++, quadruple);

    public void Define(Variable variable, DagNode node)
    {
        if (current.TryGetValue(variable, out var previous)) labels[previous].Remove(variable);

        current[variable] = node;
        labels.TryAdd(node, []);
        labels[node].Add(variable);
    }

    public Variable? Holder(DagNode node)
    {
        var nodeLabels = labels.GetValueOrDefault(node, []);
        return nodeLabels.FirstOrDefault(v => v.IsTemporary) ?? nodeLabels.FirstOrDefault();
    }
}
