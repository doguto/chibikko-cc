using System.Text;
using Chibikko.Backend.Type;
using Chibikko.Common.Ir;
using Chibikko.Common.Utility;

namespace Chibikko.Backend;

sealed class OperandEmitter(StringBuilder code, FunctionCodegenContext context)
{
    // 定数は即値、変数は rbp 相対で読み込む。レジスタは型の演算幅の下位ビットのみ有効とする
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
                LoadFromStack(variable, register);
                break;
            default:
                throw new NotSupportedException($"Unsupported Operand: {operand.GetType().Name}");
        }
    }

    // bp から offset 分ずらしたアドレスの内容をレジスタにコピーする
    // 4 byte 未満は 32bit に符号拡張して読み込む
    public void LoadFromStack(Variable variable, Register register)
    {
        var suffix = variable.Type.GetAssemblySuffix();
        var instruction = suffix is 'q' or 'l' ? $"mov{suffix}" : $"movs{suffix}l";
        var target = register.ToSized(variable.Type.GetOperationWidth());
        code.AppendNewIndentedLine($"{instruction} {context.OffsetOf(variable)}({Register.rbp.ToName()}), {target.ToName()}");
    }

    // レジスタ上の変数には 64bit のまま転送し、それ以外は変数の幅に合わせて rbp 相対で書き込む
    public void Store(Register source, Variable destination)
    {
        if (RegisterOf(destination) is { } target)
        {
            if (source != target) code.AppendNewIndentedLine($"movq {source.ToName()}, {target.ToName()}");
            return;
        }
        StoreToStack(source, destination);
    }

    public void StoreToStack(Register source, Variable destination)
    {
        var suffix = destination.Type.GetAssemblySuffix();
        var sized = source.ToSized(destination.Type.GetWidth());
        code.AppendNewIndentedLine($"mov{suffix} {sized.ToName()}, {context.OffsetOf(destination)}({Register.rbp.ToName()})");
    }

    public Register? RegisterOf(Variable variable, InstructionAllocation? allocation = null) =>
        (allocation ?? context.Current).Of(variable) is InRegister { Register: var register }
            ? register
            : null;

    public bool IsIn(Operand operand, Register register) =>
        operand is Variable variable && RegisterOf(variable) == register;

    public string SourceOperand(Operand operand)
    {
        var width = TypeOf(operand).GetOperationWidth();
        switch (operand)
        {
            case IntegerConstant constant when constant.Value is >= int.MinValue and <= int.MaxValue:
                return $"${constant.Value}";
            case Variable variable when RegisterOf(variable) is { } register:
                return register.ToSized(width).ToName();
            case Variable variable when variable.Type.GetWidth() == width:
                return $"{context.OffsetOf(variable)}({Register.rbp.ToName()})";
            default:
                Load(operand, Register.rcx);
                return Register.rcx.ToSized(width).ToName();
        }
    }

    public static IrType TypeOf(Operand operand) => operand switch
    {
        Variable variable => variable.Type,
        Constant constant => constant.Type,
        _ => throw new NotSupportedException($"Unsupported Operand: {operand.GetType().Name}"),
    };
}
