using System.Text;
using Chibikko.Backend.Type;
using Chibikko.Common.Ir;
using Chibikko.Common.Utility;

namespace Chibikko.Backend;

sealed class OperandEmitter(StringBuilder code, FunctionCodegenContext context)
{
    // 定数は即値、変数は rbp 相対で読み込む。4 byte 以下は符号拡張して 64bit レジスタに載せる
    public void Load(Operand operand, Register register, InstructionAllocation? allocation = null)
    {
        if (operand is Variable temp && RegisterOf(temp, allocation) is { } source)
        {
            if (source != register) code.AppendNewIndentedLine($"movq {source.ToName()}, {register.ToName()}");
            return;
        }

        switch (operand)
        {
            case IntegerConstant constant:
                code.AppendNewIndentedLine($"movq ${constant.Value}, {register.ToName()}");
                break;
            case Variable variable:
                // bp から offset 分ずらしたアドレスの内容をレジスタにコピーする
                var suffix = variable.Type.GetAssemblySuffix();
                var instruction = suffix == 'q' ? "movq" : $"movs{suffix}q";
                code.AppendNewIndentedLine($"{instruction} {context.OffsetOf(variable)}({Register.rbp.ToName()}), {register.ToName()}");
                break;
            default:
                throw new NotSupportedException($"Unsupported Operand: {operand.GetType().Name}");
        }
    }

    // レジスタ上の Temp には 64bit のまま転送し、それ以外は変数の幅に合わせて rbp 相対で書き込む
    public void Store(Register source, Variable destination)
    {
        var suffix = destination.Type.GetAssemblySuffix();
        var sized = source.ToSized(destination.Type.GetWidth());
        if (RegisterOf(destination) is { } target)
        {
            if (source != target) code.AppendNewIndentedLine($"movq {source.ToName()}, {target.ToName()}");
            return;
        }
        code.AppendNewIndentedLine($"mov{suffix} {sized.ToName()}, {context.OffsetOf(destination)}({Register.rbp.ToName()})");
    }

    public Register? RegisterOf(Variable variable, InstructionAllocation? allocation = null) =>
        variable.IsTemporary && (allocation ?? context.Current).Of(variable) is InRegister { Register: var register }
            ? register
            : null;

    public bool IsIn(Operand operand, Register register) =>
        operand is Variable variable && RegisterOf(variable) == register;

    public string SourceOperand(Operand operand)
    {
        switch (operand)
        {
            case IntegerConstant constant when constant.Value is >= int.MinValue and <= int.MaxValue:
                return $"${constant.Value}";
            case Variable variable when RegisterOf(variable) is { } register:
                return register.ToName();
            case Variable { Type: IrType.I64 } variable:
                return $"{context.OffsetOf(variable)}({Register.rbp.ToName()})";
            default:
                Load(operand, Register.rcx);
                return Register.rcx.ToName();
        }
    }
}
