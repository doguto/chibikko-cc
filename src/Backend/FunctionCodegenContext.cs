using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

sealed class FunctionCodegenContext(
    IrFunction function,
    StackFrame frame,
    AllocationResult allocation,
    StackFrameTable frames
)
{
    public IrFunction Function { get; } = function;
    public StackFrame Frame { get; } = frame;
    public AllocationResult Allocation { get; } = allocation;
    public StackFrameTable Frames { get; } = frames;
    public InstructionAllocation Current { get; set; } = null!;
    public Queue<(Operand Operand, InstructionAllocation Allocation)> PendingArguments { get; } = new();

    public int OffsetOf(Variable variable) => Frame.OffsetOf(variable);

    public StackFrame FrameOf(IrFunction callee) => Frames.Get(callee);
}
