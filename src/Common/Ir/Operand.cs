namespace Chibikko.Common.Ir;

public abstract class Operand;

public class Variable(IrType type, string? name = null) : Operand
{
    public IrType Type { get; } = type;
    public string? Name { get; } = name;
}

public abstract class Constant(IrType type) : Operand
{
    public IrType Type { get; } = type;
}

public class IntegerConstant(IrType type, long value) : Constant(type)
{
    public long Value { get; } = value;
}

public class FloatConstant(IrType type, double value) : Constant(type)
{
    public double Value { get; } = value;
}

public class Label : Operand;

public class FunctionRef(IrFunction function) : Operand
{
    public IrFunction Function { get; } = function;
}
