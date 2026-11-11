using Chibikko.Common.Ir;
using System.Diagnostics;
using System.Text;
using Chibikko.Common.Utility;
using Chibikko.Backend.Type;

namespace Chibikko.Backend;

public sealed class CodeGenerator
{
    FunctionCodegenContext context = null!;
    StringBuilder code = new();

    public string Emit(Ir ir)
    {
        StackFrameTable frames = new(new StackFramer());

        foreach (var function in ir.Functions)
        {
            context = new(function, frames.Get(function), frames);
            EmitFunction();
        }

        code.AppendNewIndentedLine(".section .note.GNU-stack,\"\",@progbits");
        code.AppendLine();
        return code.ToString();
    }

    void EmitFunction()
    {
        var frameSize = (context.Frame.Size + 15) / 16 * 16;
        code.AppendNewIndentedLine($".globl {context.Function.Name}");
        code.AppendNewLine($"{context.Function.Name}:");
        code.AppendNewIndentedLine($"pushq {Register.rbp.ToName()}");
        code.AppendNewIndentedLine($"movq {Register.rsp.ToName()}, {Register.rbp.ToName()}");
        if (frameSize != 0) code.AppendNewIndentedLine($"subq ${frameSize}, {Register.rsp.ToName()}");

        foreach (var quadruple in context.Function.Body)
        {
            switch (quadruple.OperationType)
            {
                case OperationType.Assign:
                    GenerateAssign(quadruple);
                    break;

                case OperationType.Add:
                    GenerateAdd(quadruple);
                    break;
                case OperationType.Subtract:
                    GenerateSubtract(quadruple);
                    break;
                case OperationType.Multiply:
                    GenerateMultiply(quadruple);
                    break;
                case OperationType.Divide:
                    GenerateDivide(quadruple);
                    break;
                case OperationType.Negate:
                    GenerateNegate(quadruple);
                    break;

                case OperationType.Equal:
                    GenerateEqual(quadruple);
                    break;
                case OperationType.NotEqual:
                    GenerateNotEqual(quadruple);
                    break;
                case OperationType.LessThan:
                    GenerateLessThan(quadruple);
                    break;
                case OperationType.LessThanOrEqual:
                    GenerateLessThanOrEqual(quadruple);
                    break;
                case OperationType.GreaterThan:
                    GenerateGreaterThan(quadruple);
                    break;
                case OperationType.GreaterThanOrEqual:
                    GenerateGreaterThanOrEqual(quadruple);
                    break;

                case OperationType.Label:
                    GenerateLabel(quadruple);
                    break;
                case OperationType.Jump:
                    GenerateJump(quadruple);
                    break;
                case OperationType.JumpIfFalse:
                    GenerateJumpIfFalse(quadruple);
                    break;

                case OperationType.Param:
                    GenerateParam(quadruple);
                    break;
                case OperationType.Call:
                    GenerateCall(quadruple);
                    break;
                case OperationType.Return:
                    GenerateReturn(quadruple);
                    break;

                default:
                    throw new UnreachableException($"Unknown operation type: {quadruple.OperationType}");
            }
        }
    }

    // Source1 を rax にロードし、Destination の幅で書き戻す
    void GenerateAssign(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Source1 を rax、Source2 を rcx にロードし addq %rcx, %rax の結果を Destination に書き戻す
    void GenerateAdd(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"addq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Add と同様に subq %rcx, %rax
    void GenerateSubtract(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"subq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Add と同様に imulq %rcx, %rax
    void GenerateMultiply(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"imulq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Source1 を rax、Source2 を rcx にロードし、cqto で rdx:rax に符号拡張してから idivq %rcx
    // 商 (rax) を Destination に書き戻す
    void GenerateDivide(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine("cqto"); // rdx:rax に拡張し、溢れを防ぐ
        code.AppendNewIndentedLine($"idivq {Register.rcx.ToName()}"); // rax に商, rdx に余りが格納される
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Source1 を rax にロードし negq %rax の結果を Destination に書き戻す
    void GenerateNegate(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        code.AppendNewIndentedLine($"negq {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Source1 を rax、Source2 を rcx にロードし cmpq %rcx, %rax
    // sete %al → movzbq %al, %rax で 0/1 にして Destination に書き戻す
    void GenerateEqual(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"sete {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Equal と同様に setne
    void GenerateNotEqual(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"setne {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Equal と同様に setl
    void GenerateLessThan(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"setl {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Equal と同様に setle
    void GenerateLessThanOrEqual(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"setle {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Equal と同様に setg
    void GenerateGreaterThan(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"setg {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // Equal と同様に setge
    void GenerateGreaterThanOrEqual(Quadruple quadruple)
    {
        LoadOperand(quadruple.Source1!, Register.rax);
        LoadOperand(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine($"cmpq {Register.rcx.ToName()}, {Register.rax.ToName()}");
        code.AppendNewIndentedLine($"setge {Register.al.ToName()}");
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {Register.rax.ToName()}");
        StoreOperand(Register.rax, (quadruple.Destination as Variable)!);
    }

    // ラベル名をインデントなしで出力する
    void GenerateLabel(Quadruple quadruple)
    {
        Label label = (quadruple.Source1 as Label)!;
        code.AppendNewLine($"{label.Name}:");
    }

    // 対象ラベルへ無条件に jmp する
    void GenerateJump(Quadruple quadruple)
    {
        Label target = (quadruple.Source1 as Label)!;
        code.AppendNewIndentedLine($"jmp {target.Name}");
    }

    // 条件を rax にロードし cmpq $0, %rax
    // 0 なら je で対象ラベルへジャンプする
    void GenerateJumpIfFalse(Quadruple quadruple)
    {
        throw new NotImplementedException("JumpIfFalse");
    }

    // 引数をキューに積み、直後の Call でまとめて配置する
    void GenerateParam(Quadruple quadruple)
    {
        context.PendingArguments.Enqueue(quadruple.Source1!);
    }

    // System V ABI に従い、先頭 6 個をレジスタ、残りを逆順に push して call する
    // call 時点で rsp を 16 byte 境界に揃え、終了後に push 分を戻す
    void GenerateCall(Quadruple quadruple)
    {
        IrFunction irFunction = (quadruple.Source1 as FunctionRef)!.Function;

        List<Operand> arguments = [];
        while (context.PendingArguments.TryDequeue(out var argument)) arguments.Add(argument);

        var stackArguments = arguments.Skip(RegisterGroup.IntArgumentRegisters().Count).ToList();
        var padding = stackArguments.Count % 2 == 1 ? 8 : 0;
        if (padding != 0) code.AppendNewIndentedLine($"subq ${padding}, {Register.rsp.ToName()}");

        // Register に置けない引数は Stack に push する
        foreach (var argument in Enumerable.Reverse(stackArguments))
        {
            LoadOperand(argument, Register.rax);
            code.AppendNewIndentedLine($"pushq {Register.rax.ToName()}");
        }

        // Register への配置
        for (var i = 0; i < Math.Min(arguments.Count, RegisterGroup.IntArgumentRegisters().Count); i++)
        {
            LoadOperand(arguments[i], RegisterGroup.IntArgumentRegisters()[i]);
        }

        code.AppendNewIndentedLine($"movl $0, {Register.eax.ToName()}");
        code.AppendNewIndentedLine($"call {irFunction.Name}");

        var cleanup = stackArguments.Count * 8 + padding;
        if (cleanup != 0) code.AppendNewIndentedLine($"addq ${cleanup}, {Register.rsp.ToName()}");
    }

    void GenerateReturn(Quadruple quadruple)
    {
        if (quadruple.Source1 is { } value) LoadOperand(value, Register.rax);
        code.AppendNewIndentedLine($"movq {Register.rbp.ToName()}, {Register.rsp.ToName()}");
        code.AppendNewIndentedLine($"popq {Register.rbp.ToName()}");
        code.AppendNewIndentedLine("ret");
    }


    // --- Helper

    // 定数は即値、変数は rbp 相対で読み込む。4 byte 以下は符号拡張して 64bit レジスタに載せる
    void LoadOperand(Operand operand, Register register)
    {
        switch (operand)
        {
            case IntegerConstant constant:
                code.AppendNewIndentedLine($"movq ${constant.Value}, {register.ToName()}");
                break;
            case Variable variable:
                // bp から offset 分ずらしたアドレスの内容をレジスタにコピーする
                var suffix = variable.Type.GetAssemblySuffix();
                var instruction = suffix == 'q' ? "movq" : $"movs{suffix}q";
                code.AppendNewIndentedLine($"{instruction} -{context.OffsetOf(variable)}({Register.rbp.ToName()}), {register.ToName()}");
                break;
            default:
                throw new NotSupportedException($"Unsupported Operand: {operand.GetType().Name}");
        }
    }

    // レジスタを変数の幅に合わせた名前にし、rbp 相対で書き込む
    void StoreOperand(Register source, Variable destination)
    {
        var suffix = destination.Type.GetAssemblySuffix();
        var sized = source.ToSized(destination.Type.GetWidth());
        code.AppendNewIndentedLine($"mov{suffix} {sized.ToName()}, -{context.OffsetOf(destination)}({Register.rbp.ToName()})");
    }
}
