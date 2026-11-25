using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

// 名前付き変数はブロック入口でメモリ上にあるものとし、ブロック内でのみレジスタに載せる
// レジスタ上で書き換えた (dirty) 変数は、追い出し時・Call 前・ブロック出口で必要な場合のみメモリへ書き戻す
public sealed class RegisterAllocator(
    IReadOnlyList<Register> callerSavedRegisters,
    IReadOnlyList<Register> calleeSavedRegisters
)
{
    readonly Dictionary<Register, Variable> registerTable = new();
    readonly Dictionary<Variable, Location> locationTable = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Variable> dirtyVariables = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Register> usedRegisters = [];
    readonly HashSet<Variable> spilledTemps = new(ReferenceEqualityComparer.Instance);
    readonly List<Variable> pendingArguments = [];

    BlockNextUse nextUses = null!;
    IReadOnlySet<Variable> liveOut = null!;
    List<int> calls = [];

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
        var liveness = LivenessAnalysis.Analyze(function);

        foreach (var block in function.Blocks)
        {
            liveOut = liveness.Out(block);
            nextUses = NextUseAnalysis.Analyze(block, liveOut);
            calls = block.Instructions
                .Select((quadruple, position) => (quadruple, position))
                .Where(x => x.quadruple.OperationType == OperationType.Call)
                .Select(x => x.position)
                .ToList();

            for (var position = 0; position < block.Instructions.Count; position++)
            {
                var quadruple = block.Instructions[position];
                List<Spill> spills = quadruple.OperationType == OperationType.Call
                    ? SpillCallerSaved(position)
                    : [];
                instructions[quadruple] = AllocateInstruction(
                    quadruple,
                    quadruple.Uses,
                    nextUses.Of(quadruple),
                    position,
                    spills
                );
            }

            if (block.Terminator is { } terminator)
            {
                var writebacks = registerTable
                    .Where(x => dirtyVariables.Contains(x.Value) && liveOut.Contains(x.Value))
                    .Select(x => new Spill(x.Value, x.Key))
                    .ToList();
                terminators[block] = AllocateInstruction(
                    null,
                    terminator.Uses,
                    nextUses.OfTerminator(),
                    block.Instructions.Count,
                    writebacks
                );
            }

            foreach (var named in registerTable.Where(x => !x.Value.IsTemporary).ToList())
            {
                Release(named.Key, named.Value);
            }
            if (registerTable.Count != 0)
            {
                throw new InvalidOperationException($"temporary variable is live on exit from block: {block.Name}");
            }
        }

        return new AllocationResult(instructions, terminators, usedRegisters, spilledTemps);
    }

    InstructionAllocation AllocateInstruction(
        Quadruple? quadruple,
        IEnumerable<Variable> uses,
        NextUseInfo nextUse,
        int position,
        List<Spill> spills
    )
    {
        var def = quadruple?.Def;
        Dictionary<Variable, Location> operands = new(ReferenceEqualityComparer.Instance);
        List<Reload> reloads = [];

        foreach (var use in uses.Distinct())
        {
            if (locationTable.TryGetValue(use, out var location))
            {
                operands[use] = location;
            }
            else if (use.IsTemporary)
            {
                throw new InvalidOperationException("temporary variable is used before definition");
            }
            else
            {
                operands[use] = LoadNamed(use, nextUse.Of(use), reloads);
            }
        }

        var preferred = quadruple?.Source1 is Variable first
            && first != def
            && operands[first] is InRegister { Register: var firstRegister }
            && IsReleasable(first, nextUse.Of(first))
                ? firstRegister
                : (Register?)null;

        // Call 時に Param 時点の配置から引数を読むため、Param のオペランドは Call まで解放しない
        var releasables = operands.Keys.Where(x => x != def && IsReleasable(x, nextUse.Of(x))).ToList();
        if (quadruple?.OperationType == OperationType.Param)
        {
            pendingArguments.AddRange(releasables);
        }
        else
        {
            releasables.ForEach(Free);
        }
        if (quadruple?.OperationType == OperationType.Call)
        {
            pendingArguments.ForEach(Free);
            pendingArguments.Clear();
        }

        if (def is not null)
        {
            var location = operands.GetValueOrDefault(def)
                ?? (locationTable.GetValueOrDefault(def) as InRegister)
                ?? (def.IsTemporary || nextUse.Of(def).Position is not null
                    ? Assign(def, nextUse.Of(def), position, preferred, spills)
                    : new OnStack());

            if (!def.IsTemporary && location is InRegister) dirtyVariables.Add(def);
            operands[def] = location;
            if (IsReleasable(def, nextUse.Of(def))) Free(def);
        }

        return new InstructionAllocation(operands, spills, reloads);
    }

    Location LoadNamed(Variable variable, NextUse nextUse, List<Reload> reloads)
    {
        if (nextUse.Position is null) return new OnStack();

        var free = callerSavedRegisters.Concat(calleeSavedRegisters)
            .Where(r => !registerTable.ContainsKey(r))
            .Cast<Register?>()
            .FirstOrDefault();
        if (free is not { } register) return new OnStack();

        reloads.Add(new Reload(variable, register));
        return Occupy(register, variable);
    }

    Location Assign(
        Variable variable,
        NextUse nextUse,
        int position,
        Register? preferred,
        List<Spill> spills
    )
    {
        var crossesCall = variable.IsTemporary
            && nextUses.LastUse(variable) is { } lastUse
            && calls.Any(p => position < p && p < lastUse);
        var candidates = crossesCall
            ? calleeSavedRegisters
            : callerSavedRegisters.Concat(calleeSavedRegisters).ToList();

        if (preferred is { } reusable && candidates.Contains(reusable) && !registerTable.ContainsKey(reusable))
        {
            return Occupy(reusable, variable);
        }

        var free = candidates.Where(r => !registerTable.ContainsKey(r)).Cast<Register?>().FirstOrDefault();
        if (free is { } register) return Occupy(register, variable);

        var victims = registerTable.Where(x => candidates.Contains(x.Key)).ToList();
        if (victims.Count == 0) return PlaceOnStack(variable);

        var victim = victims
            .Select(x => (
                Register: x.Key,
                Variable: x.Value,
                Position: nextUses.NextUseAfter(x.Value, position) ?? int.MaxValue
            ))
            .MaxBy(x => x.Position);

        if ((nextUse.Position ?? int.MaxValue) >= victim.Position) return PlaceOnStack(variable);

        if (IsNeededInMemory(victim.Variable, position)) spills.Add(new Spill(victim.Variable, victim.Register));
        if (victim.Variable.IsTemporary)
        {
            spilledTemps.Add(victim.Variable);
            locationTable[victim.Variable] = new OnStack();
            registerTable.Remove(victim.Register);
        }
        else
        {
            Release(victim.Register, victim.Variable);
        }
        return Occupy(victim.Register, variable);
    }

    List<Spill> SpillCallerSaved(int position)
    {
        List<Spill> spills = [];
        foreach (var (register, variable) in registerTable.Where(x => !x.Value.IsTemporary && callerSavedRegisters.Contains(x.Key)).ToList())
        {
            if (IsNeededInMemory(variable, position)) spills.Add(new Spill(variable, register));
            Release(register, variable);
        }
        return spills;
    }

    bool IsNeededInMemory(Variable variable, int position) =>
        variable.IsTemporary
        || dirtyVariables.Contains(variable) && (nextUses.NextUseAfter(variable, position) is not null || liveOut.Contains(variable));

    bool IsReleasable(Variable variable, NextUse nextUse) =>
        nextUse.Position is null && !(nextUse.IsLive && dirtyVariables.Contains(variable));

    Location PlaceOnStack(Variable variable)
    {
        if (!variable.IsTemporary) return new OnStack();

        spilledTemps.Add(variable);
        return locationTable[variable] = new OnStack();
    }

    Location Occupy(Register register, Variable variable)
    {
        registerTable[register] = variable;
        locationTable[variable] = new InRegister(register);
        usedRegisters.Add(register);
        return new InRegister(register);
    }

    void Release(Register register, Variable variable)
    {
        registerTable.Remove(register);
        locationTable.Remove(variable);
        dirtyVariables.Remove(variable);
    }

    void Free(Variable variable)
    {
        if (locationTable.GetValueOrDefault(variable) is not InRegister { Register: var register }) return;
        if (registerTable.GetValueOrDefault(register) != variable) return;

        if (variable.IsTemporary) registerTable.Remove(register);
        else Release(register, variable);
    }
}
