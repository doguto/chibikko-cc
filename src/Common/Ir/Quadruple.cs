namespace Chibikko.Common.Ir;

public class Quadruple
{
    public OperationType OperationType { get; init; }
    public Operand? Destination { get; init; }
    public Operand? Source1 { get; init; }
    public Operand? Source2 { get; init; }
    public IReadOnlyList<Operand> Arguments { get; init; } = [];
}
