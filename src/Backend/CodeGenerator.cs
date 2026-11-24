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
    OperandEmitter operands = null!;

    public string Emit(Ir ir)
    {
        StackFrameTable frames = new(new StackFramer());

        foreach (var function in ir.Functions)
        {
            context = new(function, frames.Get(function), frames.AllocationOf(function), frames);
            operands = new(code, context);
            EmitFunction();
        }

        code.AppendNewIndentedLine(".section .note.GNU-stack,\"\",@progbits");
        code.AppendLine();
        return code.ToString();
    }

    void EmitFunction()
    {
        var frameSize = context.Frame.Size;
        code.AppendNewIndentedLine($".globl {context.Function.Name}");
        code.AppendNewLine($"{context.Function.Name}:");
        code.AppendNewIndentedLine($"pushq {Register.rbp.ToName()}");
        code.AppendNewIndentedLine($"movq {Register.rsp.ToName()}, {Register.rbp.ToName()}");
        if (frameSize != 0) code.AppendNewIndentedLine($"subq ${frameSize}, {Register.rsp.ToName()}");
        EmitSaveRegisters();
        EmitParameterSpill();

        var blocks = context.Function.Blocks;
        for (var i = 0; i < blocks.Count; i++)
        {
            var next = i + 1 < blocks.Count ? blocks[i + 1] : null;
            EmitBlock(blocks[i], next);
        }
    }

    void EmitBlock(BasicBlock block, BasicBlock? next)
    {
        if (block != context.Function.Blocks[0]) code.AppendNewLine($"{BlockLabel(block)}:");

        foreach (var quadruple in block.Instructions)
        {
            context.Current = context.Allocation.Of(quadruple);
            EmitSpills();

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


                case OperationType.Param:
                    GenerateParam(quadruple);
                    break;
                case OperationType.Call:
                    GenerateCall(quadruple);
                    break;

                default:
                    throw new UnreachableException($"Unknown operation type: {quadruple.OperationType}");
            }
        }

        if (block.Terminator is not null)
        {
            context.Current = context.Allocation.OfTerminator(block);
            EmitSpills();
        }

        switch (block.Terminator)
        {
            case Jump jump:
                GenerateJump(jump, next);
                break;
            case Branch branch:
                GenerateBranch(branch, next);
                break;
            case Return @return:
                GenerateReturn(@return);
                break;
            default:
                throw new UnreachableException($"Block has no terminator: {block.Name}");
        }
    }

    string BlockLabel(BasicBlock block) => $".L{context.Function.Name}.{context.Function.Blocks.IndexOf(block)}";

    // Destination か Source1 がレジスタ上の Temp ならレジスタ間で直接転送し、それ以外は rax を経由する
    void GenerateAssign(Quadruple quadruple)
    {
        var destination = (quadruple.Destination as Variable)!;
        if (operands.RegisterOf(destination) is { } target)
        {
            operands.Load(quadruple.Source1!, target);
            return;
        }
        if (quadruple.Source1 is Variable source && operands.RegisterOf(source) is { } sourceRegister)
        {
            operands.Store(sourceRegister, destination);
            return;
        }

        operands.Load(quadruple.Source1!, Register.rax);
        operands.Store(Register.rax, destination);
    }

    void GenerateAdd(Quadruple quadruple) => GenerateArithmetic(quadruple, "addq");

    void GenerateSubtract(Quadruple quadruple) => GenerateArithmetic(quadruple, "subq");

    void GenerateMultiply(Quadruple quadruple) => GenerateArithmetic(quadruple, "imulq");

    // Destination に割り当てたレジスタ (無ければ rax) を演算先とし、Source2 は即値・レジスタのまま使う
    // Source2 が演算先と同じレジスタにある場合は rax で演算する
    void GenerateArithmetic(Quadruple quadruple, string instruction)
    {
        var destination = (quadruple.Destination as Variable)!;
        var target = operands.RegisterOf(destination) is { } register && !operands.IsIn(quadruple.Source2!, register)
            ? register
            : Register.rax;

        operands.Load(quadruple.Source1!, target);
        code.AppendNewIndentedLine($"{instruction} {operands.SourceOperand(quadruple.Source2!)}, {target.ToName()}");
        if (target == Register.rax) operands.Store(Register.rax, destination);
    }

    // Source1 を rax にロードし、cqto で rdx:rax に符号拡張してから idivq する
    // 商 (rax) を Destination に書き戻す
    void GenerateDivide(Quadruple quadruple)
    {
        operands.Load(quadruple.Source1!, Register.rax);
        var divisor = quadruple.Source2 is Variable variable && operands.RegisterOf(variable) is { } register
            ? register
            : Register.rcx;
        if (divisor == Register.rcx) operands.Load(quadruple.Source2!, Register.rcx);
        code.AppendNewIndentedLine("cqto"); // rdx:rax に拡張し、溢れを防ぐ
        code.AppendNewIndentedLine($"idivq {divisor.ToName()}"); // rax に商, rdx に余りが格納される
        operands.Store(Register.rax, (quadruple.Destination as Variable)!);
    }

    void GenerateNegate(Quadruple quadruple)
    {
        var destination = (quadruple.Destination as Variable)!;
        var target = operands.RegisterOf(destination) ?? Register.rax;

        operands.Load(quadruple.Source1!, target);
        code.AppendNewIndentedLine($"negq {target.ToName()}");
        if (target == Register.rax) operands.Store(Register.rax, destination);
    }

    void GenerateEqual(Quadruple quadruple) => GenerateComparison(quadruple, "sete");

    void GenerateNotEqual(Quadruple quadruple) => GenerateComparison(quadruple, "setne");

    void GenerateLessThan(Quadruple quadruple) => GenerateComparison(quadruple, "setl");

    void GenerateLessThanOrEqual(Quadruple quadruple) => GenerateComparison(quadruple, "setle");

    void GenerateGreaterThan(Quadruple quadruple) => GenerateComparison(quadruple, "setg");

    void GenerateGreaterThanOrEqual(Quadruple quadruple) => GenerateComparison(quadruple, "setge");

    // Source1 がレジスタ上にあればそのまま比較し、set* %al → movzbq で 0/1 を Destination に書き込む
    void GenerateComparison(Quadruple quadruple, string instruction)
    {
        var destination = (quadruple.Destination as Variable)!;
        var left = quadruple.Source1 is Variable variable && operands.RegisterOf(variable) is { } register
            ? register
            : Register.rax;
        if (left == Register.rax) operands.Load(quadruple.Source1!, Register.rax);

        code.AppendNewIndentedLine($"cmpq {operands.SourceOperand(quadruple.Source2!)}, {left.ToName()}");
        code.AppendNewIndentedLine($"{instruction} {Register.al.ToName()}");

        var target = operands.RegisterOf(destination) ?? Register.rax;
        code.AppendNewIndentedLine($"movzbq {Register.al.ToName()}, {target.ToName()}");
        if (target == Register.rax) operands.Store(Register.rax, destination);
    }

    // 次のブロックへのジャンプは省略する
    void GenerateJump(Jump jump, BasicBlock? next)
    {
        if (jump.Target != next) code.AppendNewIndentedLine($"jmp {BlockLabel(jump.Target)}");
    }

    // 条件がレジスタ上になければ rax にロードし、cmpq $0 で比較する
    // 0 なら je で Else へ、そうでなければ Then へ進む。Then が次のブロックなら jmp を省略する
    void GenerateBranch(Branch branch, BasicBlock? next)
    {
        var condition = branch.Condition is Variable variable && operands.RegisterOf(variable) is { } register
            ? register
            : Register.rax;
        if (condition == Register.rax) operands.Load(branch.Condition, Register.rax);

        code.AppendNewIndentedLine($"cmpq $0, {condition.ToName()}");
        code.AppendNewIndentedLine($"je {BlockLabel(branch.Else)}");
        if (branch.Then != next) code.AppendNewIndentedLine($"jmp {BlockLabel(branch.Then)}");
    }

    // 引数をキューに積み、直後の Call でまとめて配置する
    void GenerateParam(Quadruple quadruple)
    {
        context.PendingArguments.Enqueue((quadruple.Source1!, context.Current));
    }

    // System V ABI に従い、先頭 6 個をレジスタ、残りを逆順に push して call する
    // call 時点で rsp を 16 byte 境界に揃え、終了後に push 分を戻す
    void GenerateCall(Quadruple quadruple)
    {
        IrFunction irFunction = (quadruple.Source1 as FunctionRef)!.Function;

        List<(Operand Operand, InstructionAllocation Allocation)> arguments = [];
        while (context.PendingArguments.TryDequeue(out var argument)) arguments.Add(argument);

        var stackArguments = arguments.Skip(RegisterGroup.IntArgumentRegisters().Count).ToList();
        var padding = stackArguments.Count % 2 == 1 ? 8 : 0;
        if (padding != 0) code.AppendNewIndentedLine($"subq ${padding}, {Register.rsp.ToName()}");

        // Register に置けない引数は Stack に push する
        foreach (var argument in Enumerable.Reverse(stackArguments))
        {
            operands.Load(argument.Operand, Register.rax, argument.Allocation);
            code.AppendNewIndentedLine($"pushq {Register.rax.ToName()}");
        }

        // Register への配置
        for (var i = 0; i < Math.Min(arguments.Count, RegisterGroup.IntArgumentRegisters().Count); i++)
        {
            operands.Load(arguments[i].Operand, RegisterGroup.IntArgumentRegisters()[i], arguments[i].Allocation);
        }

        code.AppendNewIndentedLine($"movl $0, {Register.eax.ToName()}");
        code.AppendNewIndentedLine($"call {irFunction.Name}");

        var cleanup = stackArguments.Count * 8 + padding;
        if (cleanup != 0) code.AppendNewIndentedLine($"addq ${cleanup}, {Register.rsp.ToName()}");

        if (quadruple.Destination is Variable destination) operands.Store(Register.rax, destination);
    }

    void GenerateReturn(Return @return)
    {
        if (@return.Value is { } value) operands.Load(value, Register.rax);
        EmitRestoreRegisters();
        code.AppendNewIndentedLine($"movq {Register.rbp.ToName()}, {Register.rsp.ToName()}");
        code.AppendNewIndentedLine($"popq {Register.rbp.ToName()}");
        code.AppendNewIndentedLine("ret");
    }


    void EmitSaveRegisters()
    {
        var saved = context.Frame.SavedRegisters;
        for (var i = 0; i < saved.Count; i++)
        {
            code.AppendNewIndentedLine($"movq {saved[i].ToName()}, {context.Frame.SavedRegisterOffset(i)}({Register.rbp.ToName()})");
        }
    }

    void EmitRestoreRegisters()
    {
        var saved = context.Frame.SavedRegisters;
        for (var i = 0; i < saved.Count; i++)
        {
            code.AppendNewIndentedLine($"movq {context.Frame.SavedRegisterOffset(i)}({Register.rbp.ToName()}), {saved[i].ToName()}");
        }
    }

    void EmitSpills()
    {
        foreach (var spill in context.Current.SpillsBefore)
        {
            var suffix = spill.Temp.Type.GetAssemblySuffix();
            var sized = spill.From.ToSized(spill.Temp.Type.GetWidth());
            code.AppendNewIndentedLine($"mov{suffix} {sized.ToName()}, {context.OffsetOf(spill.Temp)}({Register.rbp.ToName()})");
        }
    }

    // 引数レジスタを自フレームの Parameter スロットへ書き込む
    // 7 個目以降の引数は呼び出し元フレーム上 (16(%rbp) 以降) をそのまま参照する
    void EmitParameterSpill()
    {
        var registers = RegisterGroup.IntArgumentRegisters();
        var parameters = context.Function.Parameters;
        for (var i = 0; i < Math.Min(parameters.Count, registers.Count); i++)
        {
            operands.Store(registers[i], parameters[i]);
        }
    }
}
