namespace Chibikko.Common.Ir;

public class Quadruple
{
    public OperationType OperationType { get; init; }
    public Operand? Destination { get; init; }
    public Operand? Source1 { get; init; }
    public Operand? Source2 { get; init; }

    // 定数等は除き、 Variable 型のみを取り出す
    public IEnumerable<Variable> Uses => new[] { Source1, Source2 }.OfType<Variable>();

    public Variable? Def => Destination as Variable;
}
