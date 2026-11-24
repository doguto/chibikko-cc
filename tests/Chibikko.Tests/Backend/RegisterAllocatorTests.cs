using Chibikko.Backend;
using Chibikko.Backend.Type;
using Chibikko.Common.Ir;
using Chibikko.Frontend;

namespace Chibikko.Tests.Backend;

public class RegisterAllocatorTests
{
    static IrFunction Function(string source, string name = "main") =>
        new FrontendPipeline().Run(source).Functions.Single(f => f.Name == name);

    static IrFunction Main(string body) => Function($"int main() {{ {body} }}");

    static Quadruple Find(BasicBlock block, OperationType operationType) =>
        block.Instructions.Single(q => q.OperationType == operationType);

    [Fact]
    public void Temporary_IsAssignedRegister()
    {
        var function = Main("int a = 1; int b = 2; int c = 3; int x = (a + b) * c; return x;");
        var result = new RegisterAllocator().Allocate(function);

        var entry = function.Blocks[0];
        var add = Find(entry, OperationType.Add);
        var multiply = Find(entry, OperationType.Multiply);
        var t1 = (Variable)add.Destination!;

        var location = Assert.IsType<InRegister>(result.Of(add).Of(t1));
        Assert.Equal(location, result.Of(multiply).Of(t1));
        Assert.Empty(result.SpilledTemps);
    }

    [Fact]
    public void Register_IsReusedAfterLastUse()
    {
        var function = Main("int a = 1; int b = 2; int c = 3; int x = (a + b) * c; return x;");
        var result = new RegisterAllocator([Register.rbx]).Allocate(function);

        var entry = function.Blocks[0];
        var multiply = Find(entry, OperationType.Multiply);
        var t1 = (Variable)multiply.Source1!;
        var t2 = (Variable)multiply.Destination!;

        Assert.Equal(new InRegister(Register.rbx), result.Of(multiply).Of(t1));
        Assert.Equal(new InRegister(Register.rbx), result.Of(multiply).Of(t2));
        Assert.Empty(result.SpilledTemps);
    }

    [Fact]
    public void Destination_PrefersSource1Register_WhenSource1Dies()
    {
        var function = Main("int a = 1; int b = 2; int c = 3; int x = (a + b) + (a - b) * c; return x;");
        var result = new RegisterAllocator([Register.rbx, Register.r12, Register.r13]).Allocate(function);

        var entry = function.Blocks[0];
        var subtract = Find(entry, OperationType.Subtract);
        var multiply = Find(entry, OperationType.Multiply);
        var t2 = (Variable)subtract.Destination!;
        var t3 = (Variable)multiply.Destination!;

        Assert.Equal(new InRegister(Register.r12), result.Of(subtract).Of(t2));
        Assert.Equal(new InRegister(Register.r12), result.Of(multiply).Of(t3));
    }

    [Fact]
    public void Farthest_IsSpilled_WhenNoRegisterIsFree()
    {
        var function = Main("int a = 1; int b = 2; int x = (a + b) * (a - b); return x;");
        var result = new RegisterAllocator([Register.rbx]).Allocate(function);

        var entry = function.Blocks[0];
        var add = Find(entry, OperationType.Add);
        var subtract = Find(entry, OperationType.Subtract);
        var multiply = Find(entry, OperationType.Multiply);
        var t1 = (Variable)add.Destination!;
        var t2 = (Variable)subtract.Destination!;

        Assert.Equal(new InRegister(Register.rbx), result.Of(add).Of(t1));
        Assert.IsType<OnStack>(result.Of(subtract).Of(t2));
        Assert.Equal(new InRegister(Register.rbx), result.Of(multiply).Of(t1));
        Assert.Contains(t2, result.SpilledTemps);
    }

    [Fact]
    public void Victim_IsSpilledBeforeInstruction_WhenItsNextUseIsFarther()
    {
        var function = Main("int a = 1; int b = 2; int c = 3; int x = (a + b) * (c * (a - b)); return x;");
        var result = new RegisterAllocator([Register.rbx]).Allocate(function);

        var entry = function.Blocks[0];
        var add = Find(entry, OperationType.Add);
        var subtract = Find(entry, OperationType.Subtract);
        var t1 = (Variable)add.Destination!;
        var t2 = (Variable)subtract.Destination!;

        Assert.Equal([new Spill(t1, Register.rbx)], result.Of(subtract).SpillsBefore);
        Assert.Equal(new InRegister(Register.rbx), result.Of(subtract).Of(t2));
        Assert.Contains(t1, result.SpilledTemps);
    }

    [Fact]
    public void ParamOperand_KeepsLocationAtParam()
    {
        var function = Function("int f(int x, int y) { return x + y; } int main() { int a = 1; return f(a + 1, a + 2); }");
        var result = new RegisterAllocator().Allocate(function);

        var entry = function.Blocks[0];
        var parameters = entry.Instructions.Where(q => q.OperationType == OperationType.Param).ToList();

        var locations = parameters.Select(q => result.Of(q).Of((Variable)q.Source1!)).ToList();
        Assert.All(locations, l => Assert.IsType<InRegister>(l));
        Assert.Equal(locations.Count, locations.Distinct().Count());
    }

    [Fact]
    public void UsedRegisters_ContainsOnlyAssignedRegisters()
    {
        var function = Main("int a = 1; int b = 2; return a + b;");
        var result = new RegisterAllocator().Allocate(function);

        Assert.Equal([Register.rbx], result.UsedRegisters);
    }
}
