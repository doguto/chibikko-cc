using Chibikko.Common.Ir;

namespace Chibikko.Optimizer.Dag;

public abstract class DagNode(int id)
{
    public int Id { get; } = id;
}

public sealed class VariableLeaf(int id, Variable variable) : DagNode(id)
{
    public Variable Variable { get; } = variable;
}

public sealed class ConstantLeaf(int id, Constant constant) : DagNode(id)
{
    public Constant Constant { get; } = constant;
}

public sealed class InteriorNode(
    int id,
    OperationType operationType,
    DagNode? left,
    DagNode? right
) : DagNode(id)
{
    public OperationType OperationType { get; } = operationType;
    public DagNode? Left { get; } = left;
    public DagNode? Right { get; } = right;
}

public sealed class OpaqueNode(int id, Quadruple quadruple) : DagNode(id)
{
    public Quadruple Quadruple { get; } = quadruple;
}
