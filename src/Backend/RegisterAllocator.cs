using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public sealed class RegisterAllocator(
    IReadOnlyList<Register> callerSavedRegisters,
    IReadOnlyList<Register> calleeSavedRegisters
)
{
    readonly Dictionary<Register, Variable> registerTable = new();
    readonly Dictionary<Variable, Location> locationTable = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Register> usedRegisters = [];
    readonly HashSet<Variable> spilledTemps = new(ReferenceEqualityComparer.Instance);

    public RegisterAllocator() : this(
        RegisterGroup.CallerSavedAllocatableRegisters(),
        RegisterGroup.CalleeSavedAllocatableRegisters()
    )
    {
    }

    public AllocationResult Allocate(IrFunction function)
    {
        Dictionary<Quadruple, InstructionAllocation> instructions = new(ReferenceEqualityComparer.Instance);
        Dictionary<BasicBlock, InstructionAllocation> terminators = new(ReferenceEqualityComparer.Instance);

        foreach (var block in function.Blocks)
        {
            var nextUses = NextUseAnalysis.Analyze(block);
            var calls = block.Instructions
                .Select((quadruple, position) => (quadruple, position))
                .Where(x => x.quadruple.OperationType == OperationType.Call)
                .Select(x => x.position)
                .ToList();

            for (var position = 0; position < block.Instructions.Count; position++)
            {
                var quadruple = block.Instructions[position];
                instructions[quadruple] = AllocateInstruction(
                    quadruple.Source1,
                    quadruple.Uses,
                    quadruple.Def,
                    nextUses.Of(quadruple),
                    nextUses,
                    calls,
                    position
                );
            }

            if (block.Terminator is { } terminator)
            {
                terminators[block] = AllocateInstruction(
                    null,
                    terminator.Uses,
                    null,
                    nextUses.OfTerminator(),
                    nextUses,
                    calls,
                    block.Instructions.Count
                );
            }

            if (registerTable.Count != 0)
            {
                throw new InvalidOperationException($"temporary variable is live on exit from block: {block.Name}");
            }
        }

        return new AllocationResult(instructions, terminators, usedRegisters, spilledTemps);
    }

    InstructionAllocation AllocateInstruction(
        Operand? source1,
        IEnumerable<Variable> uses,
        Variable? def,
        NextUseInfo nextUse,
        BlockNextUse nextUses,
        List<int> calls,
        int position
    )
    {
        Dictionary<Variable, Location> operands = new(ReferenceEqualityComparer.Instance);
        List<Spill> spills = [];

        var temporaryUses = uses.Where(x => x.IsTemporary).ToList();
        foreach (var use in temporaryUses)
        {
            operands[use] = locationTable.TryGetValue(use, out var location)
                ? location
                : throw new InvalidOperationException("temporary variable is used before definition");
        }

        var preferred = source1 is Variable { IsTemporary: true } first
            && !nextUse.Of(first).IsLive
            && locationTable[first] is InRegister { Register: var firstRegister }
                ? firstRegister
                : (Register?)null;

        foreach (var use in temporaryUses)
        {
            if (!nextUse.Of(use).IsLive) Free(use);
        }

        if (def is { IsTemporary: true })
        {
            if (!locationTable.ContainsKey(def))
            {
                var crossesCall = nextUses.LastUse(def) is { } lastUse && calls.Any(p => position < p && p < lastUse);
                locationTable[def] = Assign(def, nextUse.Of(def), nextUses, position, crossesCall, preferred, spills);
            }
            operands[def] = locationTable[def];
            if (!nextUse.Of(def).IsLive) Free(def);
        }

        return new InstructionAllocation(operands, spills);
    }

    Location Assign(
        Variable temp,
        NextUse nextUse,
        BlockNextUse nextUses,
        int position,
        bool crossesCall,
        Register? preferred,
        List<Spill> spills
    )
    {
        var candidates = crossesCall
            ? calleeSavedRegisters
            : callerSavedRegisters.Concat(calleeSavedRegisters).ToList();

        if (preferred is { } reusable && candidates.Contains(reusable) && !registerTable.ContainsKey(reusable))
        {
            return Occupy(reusable, temp);
        }

        var free = candidates.Where(r => !registerTable.ContainsKey(r)).Cast<Register?>().FirstOrDefault();
        if (free is { } register) return Occupy(register, temp);

        var victims = registerTable.Where(x => candidates.Contains(x.Key)).ToList();
        if (victims.Count == 0)
        {
            spilledTemps.Add(temp);
            return new OnStack();
        }

        var victim = victims
            .Select(x => (
                Register: x.Key,
                Temp: x.Value,
                Position: nextUses.NextUseAfter(x.Value, position) ?? int.MaxValue
            ))
            .MaxBy(x => x.Position);

        if ((nextUse.Position ?? int.MaxValue) >= victim.Position)
        {
            spilledTemps.Add(temp);
            return new OnStack();
        }

        spills.Add(new Spill(victim.Temp, victim.Register));
        spilledTemps.Add(victim.Temp);
        locationTable[victim.Temp] = new OnStack();
        registerTable.Remove(victim.Register);
        return Occupy(victim.Register, temp);
    }

    Location Occupy(Register register, Variable temp)
    {
        registerTable[register] = temp;
        usedRegisters.Add(register);
        return new InRegister(register);
    }

    void Free(Variable temp)
    {
        if (locationTable.GetValueOrDefault(temp) is not InRegister { Register: var register }) return;
        if (registerTable.GetValueOrDefault(register) != temp) return;

        registerTable.Remove(register);
    }
}
