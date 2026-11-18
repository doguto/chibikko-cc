using Chibikko.Common.Ir;
using Chibikko.Frontend.Type;

namespace Chibikko.Frontend;

sealed class FunctionContext
{
    readonly Dictionary<string, int> blockNameCounts = new();

    public FunctionContext(IrFunction function)
    {
        Function = function;
        CurrentBlock = NewBlock("entry");
        Function.Blocks.Add(CurrentBlock);
    }

    public IrFunction Function { get; }
    public Dictionary<IVariableDeclaration, Variable> LocalVariablesTable { get; } = new(ReferenceEqualityComparer.Instance);
    public BasicBlock CurrentBlock { get; private set; }

    public BasicBlock NewBlock(string hint)
    {
        var count = blockNameCounts.GetValueOrDefault(hint);
        blockNameCounts[hint] = count + 1;
        return new BasicBlock(count == 0 ? hint : $"{hint}{count}");
    }

    public void SetInsertPoint(BasicBlock block)
    {
        if (!Function.Blocks.Contains(block)) Function.Blocks.Add(block);
        CurrentBlock = block;
    }

    public void Emit(Quadruple quadruple)
    {
        if (CurrentBlock.Terminator is not null) SetInsertPoint(NewBlock("unreachable"));
        CurrentBlock.Instructions.Add(quadruple);
    }

    public void Terminate(Terminator terminator)
    {
        if (CurrentBlock.Terminator is not null) SetInsertPoint(NewBlock("unreachable"));
        CurrentBlock.SetTerminator(terminator);
    }

    public void EmitJump(BasicBlock target) => Terminate(new Jump(target));

    public void EmitBranch(Operand condition, BasicBlock then, BasicBlock @else) => Terminate(new Branch(condition, then, @else));

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
}
