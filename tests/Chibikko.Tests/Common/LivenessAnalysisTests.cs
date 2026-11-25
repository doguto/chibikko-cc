using Chibikko.Common.Ir;
using Chibikko.Frontend;

namespace Chibikko.Tests.Common;

public class LivenessAnalysisTests
{
    static (IrFunction Function, Liveness Liveness) Analyze(string source, string name = "main")
    {
        var function = new FrontendPipeline().Run(source).Functions.Single(f => f.Name == name);
        return (function, LivenessAnalysis.Analyze(function));
    }

    static Variable Named(IrFunction function, string name) =>
        function.Parameters.Concat(function.Locals).Single(v => v.Name == name);

    [Fact]
    public void LoopVariable_IsLiveAroundBackEdge()
    {
        var (function, liveness) = Analyze("int main() { int i = 0; int s = 0; while (i < 3) { s = s + i; i = i + 1; } return s; }");
        var i = Named(function, "i");
        var s = Named(function, "s");
        var condition = function.Blocks.Single(b => b.Name == "while.cond");

        Assert.Contains(i, liveness.In(condition));
        Assert.Contains(s, liveness.In(condition));
        Assert.Contains(i, liveness.Out(function.Blocks[0]));
        Assert.DoesNotContain(i, liveness.In(function.Blocks[0]));
    }

    [Fact]
    public void VariableNotUsedLater_IsDeadOnExit()
    {
        var (function, liveness) = Analyze("int main() { int a = 1; int b = a + 2; if (b < 3) { b = 4; } return b; }");
        var a = Named(function, "a");

        Assert.All(function.Blocks, block => Assert.DoesNotContain(a, liveness.Out(block)));
    }

    [Fact]
    public void Temporaries_AreNotLiveAcrossBlocks()
    {
        var (function, liveness) = Analyze("int main() { int i = 0; while (i * 2 < 3) { i = i + 1; } return i; }");

        Assert.All(
            function.Blocks,
            block => Assert.DoesNotContain(liveness.In(block), v => v.IsTemporary)
        );
    }

    [Fact]
    public void InstructionLiveOut_ExcludesVariableAfterLastUse()
    {
        var (function, liveness) = Analyze("int main() { int a = 1; int b = 2; int c = a + b; return c; }");
        var entry = function.Blocks[0];
        var add = entry.Instructions.Single(q => q.OperationType == OperationType.Add);
        var a = Named(function, "a");
        var t = (Variable)add.Destination!;

        Assert.DoesNotContain(a, liveness.Out(add));
        Assert.Contains(t, liveness.Out(add));
    }

    [Fact]
    public void Parameter_IsLiveOnEntryWhenUsed()
    {
        var (function, liveness) = Analyze("int f(int x) { return x + 1; } int main() { return f(1); }", "f");

        Assert.Contains(function.Parameters[0], liveness.In(function.Blocks[0]));
    }
}
