using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public sealed class RegisterAllocator(IReadOnlyList<Register> usableRegisters)
{
    readonly Dictionary<Register, Variable> registerTable = new();
    readonly Dictionary<Variable, Location> locationTable = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Register> usedRegisters = [];
    readonly HashSet<Variable> spilledTemps = new(ReferenceEqualityComparer.Instance);

    public RegisterAllocator() : this(RegisterGroup.AllocatableRegisters())
    {
    }

    public AllocationResult Allocate(IrFunction function)
    {
        Dictionary<Quadruple, InstructionAllocation> instructions = new(ReferenceEqualityComparer.Instance);
        Dictionary<BasicBlock, InstructionAllocation> terminators = new(ReferenceEqualityComparer.Instance);

        foreach (var block in function.Blocks)
        {
            var nextUses = NextUseAnalysis.Analyze(block);

            for (var position = 0; position < block.Instructions.Count; position++)
            {
                var quadruple = block.Instructions[position];
                instructions[quadruple] = AllocateInstruction(
                    quadruple.Source1,
                    quadruple.Uses(),
                    quadruple.Defs(),
                    nextUses.Of(quadruple),
                    nextUses,
                    position
                );
            }

            if (block.Terminator is { } terminator)
            {
                terminators[block] = AllocateInstruction(
                    null,
                    terminator.Uses(),
                    [],
                    nextUses.OfTerminator(),
                    nextUses,
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
        IEnumerable<Variable> defs,
        NextUseInfo nextUse,
        BlockNextUse nextUses,
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

        foreach (var def in defs.Where(x => x.IsTemporary))
        {
            if (!locationTable.ContainsKey(def))
            {
                locationTable[def] = Assign(def, nextUse.Of(def), nextUses, position, preferred, spills);
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
        Register? preferred,
        List<Spill> spills
    )
    {
        if (preferred is { } reusable && !registerTable.ContainsKey(reusable)) return Occupy(reusable, temp);

        var free = usableRegisters.Where(r => !registerTable.ContainsKey(r)).Cast<Register?>().FirstOrDefault();
        if (free is { } register) return Occupy(register, temp);

        var victim = registerTable
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
