using Chibikko.Common.Ir;

namespace Chibikko.Backend.Type;

public abstract record Location;

public sealed record InRegister(Register Register) : Location;

public sealed record OnStack : Location;

public sealed record Spill(Variable Variable, Register From);

public sealed record Reload(Variable Variable, Register To);

public sealed class InstructionAllocation(
    IReadOnlyDictionary<Variable, Location> operands,
    IReadOnlyList<Spill> spillsBefore,
    IReadOnlyList<Reload> reloadsBefore
)
{
    public IReadOnlyList<Spill> SpillsBefore { get; } = spillsBefore;
    public IReadOnlyList<Reload> ReloadsBefore { get; } = reloadsBefore;

    public Location Of(Variable variable) =>
        operands.TryGetValue(variable, out var location) ? location
        : variable.IsTemporary ? throw new InvalidOperationException("temporary variable is not an operand")
        : new OnStack();
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
