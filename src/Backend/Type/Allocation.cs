using Chibikko.Common.Ir;

namespace Chibikko.Backend.Type;

public abstract record Location;

public sealed record InRegister(Register Register) : Location;

public sealed record OnStack : Location;

public sealed record Spill(Variable Temp, Register From);

public sealed class InstructionAllocation(
    IReadOnlyDictionary<Variable, Location> operands,
    IReadOnlyList<Spill> spillsBefore
)
{
    public IReadOnlyList<Spill> SpillsBefore { get; } = spillsBefore;

    public Location Of(Variable temp) =>
        operands.TryGetValue(temp, out var location)
            ? location
            : throw new InvalidOperationException("temporary variable is not an operand");
}

public sealed class AllocationResult(
    Dictionary<Quadruple, InstructionAllocation> instructions,
    Dictionary<BasicBlock, InstructionAllocation> terminators,
    IReadOnlySet<Register> usedRegisters,
    IReadOnlySet<Variable> spilledTemps
)
{
    public IReadOnlySet<Register> UsedRegisters { get; } = usedRegisters;
    public IReadOnlySet<Variable> SpilledTemps { get; } = spilledTemps;

    public InstructionAllocation Of(Quadruple quadruple) => instructions[quadruple];

    public InstructionAllocation OfTerminator(BasicBlock block) => terminators[block];
}
