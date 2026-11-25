using Chibikko.Backend;
using Chibikko.Common.Ir;
using Chibikko.Frontend;

namespace Chibikko.Tests.Backend;

public class NextUseAnalysisTests
{
    static IrFunction Main(string body) =>
        new FrontendPipeline().Run($"int main() {{ {body} }}").Functions.Single(f => f.Name == "main");

    static BlockNextUse Analyze(BasicBlock block, params Variable[] liveOut) =>
        NextUseAnalysis.Analyze(block, liveOut.ToHashSet());

    static Quadruple Find(BasicBlock block, OperationType operationType) =>
        block.Instructions.Single(q => q.OperationType == operationType);

    [Fact]
    public void Temporary_IsDeadAfterLastUse()
    {
        var entry = Main("int a = 1; int b = 2; int c = 3; int x = (a + b) * c; return x;").Blocks[0];
        var result = Analyze(entry);

        var add = Find(entry, OperationType.Add);
        var multiply = Find(entry, OperationType.Multiply);
        var t1 = (Variable)add.Destination!;

        Assert.Equal(new NextUse(true, entry.Instructions.IndexOf(multiply)), result.Of(add).Of(t1));
        Assert.Equal(new NextUse(false, null), result.Of(multiply).Of(t1));
    }

    [Fact]
    public void LiveOutVariable_IsLiveWithoutNextUse()
    {
        var entry = Main("int a = 1; int b = 2; int x = a + b; return x;").Blocks[0];
        var add = Find(entry, OperationType.Add);
        var a = (Variable)add.Source1!;
        var result = Analyze(entry, a);

        Assert.Equal(new NextUse(true, null), result.Of(add).Of(a));
    }

    [Fact]
    public void NamedVariable_IsDeadAfterLastUse_WhenNotLiveOut()
    {
        var entry = Main("int a = 1; int b = 2; int x = a + b; return x;").Blocks[0];
        var result = Analyze(entry);

        var add = Find(entry, OperationType.Add);
        var a = (Variable)add.Source1!;

        Assert.Equal(new NextUse(false, null), result.Of(add).Of(a));
    }

    [Fact]
    public void TerminatorUse_IsRecordedAtLastPosition()
    {
        var entry = Main("int a = 1; int b = 2; return a + b;").Blocks[0];
        var result = Analyze(entry);

        var add = Find(entry, OperationType.Add);
        var t = (Variable)add.Destination!;

        Assert.Equal(new NextUse(true, entry.Instructions.Count), result.Of(add).Of(t));
        Assert.Equal(new NextUse(false, null), result.OfTerminator().Of(t));
    }

    [Fact]
    public void BranchCondition_IsUsedByTerminator()
    {
        var function = Main("int i = 0; while (i < 3) { i = i + 1; } return i;");
        var condition = function.Blocks.Single(b => b.Name == "while.cond");
        var result = Analyze(condition);

        var lessThan = Find(condition, OperationType.LessThan);
        var t = (Variable)lessThan.Destination!;

        Assert.Equal(new NextUse(true, condition.Instructions.Count), result.Of(lessThan).Of(t));
        Assert.Equal(new NextUse(false, null), result.OfTerminator().Of(t));
    }

    [Fact]
    public void SameVariableAsUseAndDef_IsLiveBeforeInstruction()
    {
        var x = new Variable(IrType.I32, "x");
        var y = new Variable(IrType.I32, "y");
        var copy = new Quadruple { OperationType = OperationType.Assign, Destination = y, Source1 = x };
        var increment = new Quadruple { OperationType = OperationType.Add, Destination = x, Source1 = x, Source2 = new IntegerConstant(IrType.I32, 1) };

        BasicBlock block = new("entry");
        block.Instructions.Add(copy);
        block.Instructions.Add(increment);
        block.SetTerminator(new Return(y));

        var result = Analyze(block);

        Assert.Equal(new NextUse(true, 1), result.Of(copy).Of(x));
        Assert.Equal(new NextUse(true, 2), result.Of(copy).Of(y));
        Assert.Equal(new NextUse(false, null), result.Of(increment).Of(x));
    }

    [Fact]
    public void TemporaryLiveOnEntry_Throws()
    {
        var t = new Variable(IrType.I32);
        BasicBlock block = new("entry");
        block.SetTerminator(new Return(t));

        Assert.Throws<InvalidOperationException>(() => Analyze(block));
    }
}
