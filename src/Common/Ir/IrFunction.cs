namespace Chibikko.Common.Ir;

public sealed class IrFunction(string name, IrType? returnType)
{
    public string Name { get; } = name;
    public IrType? ReturnType { get; } = returnType;
    public List<Variable> Parameters { get; } = [];
    public List<Variable> Locals { get; } = [];
    public List<Quadruple> Body { get; } = [];
}
