using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

sealed class StackFrameTable(StackFramer stackFramer)
{
    readonly Dictionary<IrFunction, (StackFrame Frame, AllocationResult Allocation)> frames = new(ReferenceEqualityComparer.Instance);

    public StackFrame Get(IrFunction function) => Resolve(function).Frame;

    public AllocationResult AllocationOf(IrFunction function) => Resolve(function).Allocation;

    (StackFrame Frame, AllocationResult Allocation) Resolve(IrFunction function)
    {
        if (frames.TryGetValue(function, out var entry)) return entry;

        var allocation = new RegisterAllocator().Allocate(function);
        entry = (stackFramer.GenerateStackFrameInfo(function, allocation), allocation);
        frames[function] = entry;
        return entry;
    }
}
