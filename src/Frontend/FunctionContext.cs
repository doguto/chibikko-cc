using Chibikko.Common.Ir;
using Chibikko.Frontend.Type;

namespace Chibikko.Frontend;

sealed class FunctionContext(IrFunction function)
{
    public IrFunction Function { get; } = function;
    public Dictionary<IVariableDeclaration, Variable> LocalVariablesTable { get; } = new(ReferenceEqualityComparer.Instance);

    public void Emit(Quadruple quadruple) => Function.Body.Add(quadruple);

    public Variable NewTemporary(IrType type)
    {
        var variable = new Variable(type);
        Function.Locals.Add(variable);
        return variable;
    }

    public Variable DeclareLocal(IVariableDeclaration declaration)
    {
        var variable = new Variable(declaration.ResolvedType.ToIrType(), declaration.Name.Text);
        Function.Locals.Add(variable);
        LocalVariablesTable[declaration] = variable;
        return variable;
    }

    public void EmitLabel(Label label) =>
        Emit(new Quadruple { OperationType = OperationType.Label, Source1 = label });

    public void EmitJump(Label target) =>
        Emit(new Quadruple { OperationType = OperationType.Jump, Source1 = target });

    public void EmitJumpIfFalse(Operand condition, Label target) =>
        Emit(new Quadruple { OperationType = OperationType.JumpIfFalse, Source1 = condition, Source2 = target });
}
