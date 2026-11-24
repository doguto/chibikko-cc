using System.Diagnostics;
using Chibikko.Common.Ir;

namespace Chibikko.Optimizer.Dag;

public sealed class BasicBlockDag
{
    readonly record struct InteriorKey(
        OperationType OperationType,
        DagNode? Left,
        DagNode? Right
    );

    readonly record struct ConstantKey(
        IrType Type,
        long IntegerValue,
        double FloatValue
    );

    readonly Dictionary<InteriorKey, InteriorNode> interiorTable = new();
    readonly Dictionary<ConstantKey, ConstantLeaf> constantTable = new();
    readonly Dictionary<Variable, DagNode> current = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<DagNode, List<Variable>> labels = new(ReferenceEqualityComparer.Instance);
    int nextId;

    public DagNode NodeOf(Operand operand) => operand switch
    {
        Variable variable => NodeOf(variable),
        Constant constant => NodeOf(constant),
        _ => throw new UnreachableException($"Operand has no dag node: {operand.GetType().Name}"),
    };

    public InteriorNode? Find(
        OperationType operationType,
        DagNode? left,
        DagNode? right
    ) =>
        interiorTable.GetValueOrDefault(Key(operationType, left, right));

    public InteriorNode CreateInterior(
        OperationType operationType,
        DagNode? left,
        DagNode? right
    )
    {
        var node = new InteriorNode(
            nextId++,
            operationType,
            left,
            right
        );
        interiorTable[Key(operationType, left, right)] = node;
        return node;
    }

    public OpaqueNode CreateOpaque(Quadruple quadruple) => new(nextId++, quadruple);

    public void Define(Variable variable, DagNode node)
    {
        if (current.TryGetValue(variable, out var previous)) labels[previous].Remove(variable);

        current[variable] = node;
        if (!labels.TryGetValue(node, out var nodeLabels))
        {
            nodeLabels = [];
            labels[node] = nodeLabels;
        }
        nodeLabels.Add(variable);
    }

    public Variable? Holder(DagNode node)
    {
        if (!labels.TryGetValue(node, out var nodeLabels) || nodeLabels.Count == 0) return null;
        return nodeLabels.FirstOrDefault(v => v.IsTemporary) ?? nodeLabels[0];
    }

    DagNode NodeOf(Variable variable)
    {
        if (current.TryGetValue(variable, out var node)) return node;

        var leaf = new VariableLeaf(nextId++, variable);
        Define(variable, leaf);
        return leaf;
    }

    DagNode NodeOf(Constant constant)
    {
        var key = constant switch
        {
            IntegerConstant integer => new ConstantKey(integer.Type, integer.Value, 0),
            FloatConstant @float => new ConstantKey(@float.Type, 0, @float.Value),
            _ => throw new UnreachableException($"Unknown constant: {constant.GetType().Name}"),
        };
        if (constantTable.TryGetValue(key, out var leaf)) return leaf;

        leaf = new ConstantLeaf(nextId++, constant);
        constantTable[key] = leaf;
        return leaf;
    }

    static InteriorKey Key(
        OperationType operationType,
        DagNode? left,
        DagNode? right
    )
    {
        if (IsCommutative(operationType) && left is not null && right is not null && left.Id > right.Id)
        {
            (left, right) = (right, left);
        }
        return new InteriorKey(operationType, left, right);
    }

    static bool IsCommutative(OperationType operationType) => operationType is
        OperationType.Add or
        OperationType.Multiply or
        OperationType.Equal or
        OperationType.NotEqual;
}
