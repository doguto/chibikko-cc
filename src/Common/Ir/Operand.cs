namespace Chibikko.Common.Ir;

public abstract class Operand;

public class Variable(IrType type) : Operand
{
    public IrType Type { get; } = type;
}

public class Constant(IrType type, long value) : Operand
{
    public IrType Type { get; } = type;
    public long Value { get; } = value;
}

public class Label : Operand;

public class FunctionRef(IrFunction function) : Operand
{
    public IrFunction Function { get; } = function;
}
