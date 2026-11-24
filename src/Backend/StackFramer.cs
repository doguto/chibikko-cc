using Chibikko.Backend.Type;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public class StackFramer
{
    public StackFrame GenerateStackFrameInfo(IrFunction irFunction, AllocationResult allocation)
    {
        var registerCount = RegisterGroup.IntArgumentRegisters().Count;
        List<StackPiece> parameters = irFunction.Parameters.Select(x => new StackPiece(x.Type, x)).ToList();
        List<StackPiece> registerParameters = parameters.Take(registerCount).ToList();
        List<StackPiece> memoryArguments = parameters.Skip(registerCount).ToList();
        List<StackPiece> localVariables = irFunction.Locals.Where(x => !x.IsTemporary).Select(x => new StackPiece(x.Type, x)).ToList();
        List<StackPiece> temporaryVariables = irFunction.Locals.Where(x => x.IsTemporary && allocation.SpilledTemps.Contains(x)).Select(x => new StackPiece(x.Type, x)).ToList();

        StackFrame stackFrame = new()
        {
            MemoryArguments = memoryArguments,
            RegisterParameters = registerParameters,
            LocalVariables = localVariables,
            TemporaryVariables = temporaryVariables,
            SavedRegisters = RegisterGroup.AllocatableRegisters().Where(allocation.UsedRegisters.Contains).ToList()
        };

        return stackFrame;
    }

}
