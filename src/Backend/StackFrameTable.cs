using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

sealed class StackFrameTable(StackFramer stackFramer)
{
    readonly Dictionary<IrFunction, StackFrame> frames = new(ReferenceEqualityComparer.Instance);

    public StackFrame Get(IrFunction function)
    {
        if (frames.TryGetValue(function, out var frame)) return frame;

        frame = stackFramer.GenerateStackFrameInfo(function);
        frames[function] = frame;
        return frame;
    }
}
