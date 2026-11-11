using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

sealed class FunctionCodegenContext(IrFunction function, StackFrame frame, StackFrameTable frames)
{
    public IrFunction Function { get; } = function;
    public StackFrame Frame { get; } = frame;
    public StackFrameTable Frames { get; } = frames;
    public Queue<Operand> PendingArguments { get; } = new();

    public int OffsetOf(Variable variable) => Frame.OffsetOf(variable);

    public StackFrame FrameOf(IrFunction callee) => Frames.Get(callee);
}
