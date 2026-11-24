using Chibikko.Common.Ir;
using Chibikko.Frontend;
using Chibikko.Optimizer;

namespace Chibikko.Tests.Optimizer;

public class CommonSubexpressionEliminationTests
{
    static IrFunction Optimize(string source, string name)
    {
        var ir = new FrontendPipeline().Run(source);
        new CommonSubexpressionElimination().Run(ir);
        return ir.Functions.Single(f => f.Name == name);
    }

    static BasicBlock Optimize(string body) =>
        Optimize($"int f(int a, int b, int c, int d) {{ {body} }}", "f").Blocks[0];

    static int Count(BasicBlock block, OperationType operationType) =>
        block.Instructions.Count(q => q.OperationType == operationType);

    static Quadruple AssignTo(BasicBlock block, string name) =>
        block.Instructions.Last(q => q.Destination is Variable { Name: var n } && n == name);

    static Quadruple Definition(BasicBlock block, Variable variable) =>
        block.Instructions.Single(q => ReferenceEquals(q.Destination, variable));

    [Fact]
    public void SameExpression_IsReplacedWithCopy()
    {
        var block = Optimize("a = b + c; b = a - d; c = b + c; d = a - d; return 0;");

        Assert.Equal(1, Count(block, OperationType.Subtract));
        Assert.Equal(2, Count(block, OperationType.Add));

        var copy = (Variable)AssignTo(block, "d").Source1!;
        var source = (Variable)Definition(block, copy).Source1!;
        var subtract = block.Instructions.Single(q => q.OperationType == OperationType.Subtract);
        Assert.Same(subtract.Destination, source);
    }

    [Fact]
    public void ExpressionWithRedefinedOperand_IsNotEliminated()
    {
        var block = Optimize("a = b + c; b = b - d; c = c + d; int e = b + c; return e;");

        Assert.Equal(3, Count(block, OperationType.Add));
        Assert.Equal(1, Count(block, OperationType.Subtract));
    }

    [Fact]
    public void CommutativeOperands_AreMatched()
    {
        var block = Optimize("c = b + c; int u = b * c; int s = b + c; int e = c + b; a = b * c; return u + s + e;");

        Assert.Equal(1, Count(block, OperationType.Multiply));
        Assert.Equal(4, Count(block, OperationType.Add));
    }

    [Fact]
    public void NonCommutativeOperands_AreNotMatched()
    {
        var block = Optimize("int x = a - b; int y = b - a; return x + y;");

        Assert.Equal(2, Count(block, OperationType.Subtract));
    }

    [Fact]
    public void CallResult_IsNotShared()
    {
        var block = Optimize("int g() { return 1; } int main() { int x = g(); int y = g(); return x + y; }", "main").Blocks[0];

        Assert.Equal(2, Count(block, OperationType.Call));
    }

    [Fact]
    public void ExpressionOverCallResults_IsNotShared()
    {
        var block = Optimize("int g() { return 1; } int main() { int x = g() + 1; int y = g() + 1; return x + y; }", "main").Blocks[0];

        Assert.Equal(3, Count(block, OperationType.Add));
    }

    [Fact]
    public void ExpressionAcrossBlocks_IsNotEliminated()
    {
        var function = Optimize("int f(int a, int b) { int x = a + b; if (x > 0) { a = 0; } return a + b; }", "f");

        Assert.Equal(2, function.Blocks.Sum(b => Count(b, OperationType.Add)));
    }
}
