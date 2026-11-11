using Chibikko.Backend.Type;
using Chibikko.Common;
using Chibikko.Common.Ir;

namespace Chibikko.Backend;

public class StackFramer
{
    public StackFrame GenerateStackFrameInfo(IrFunction irFunction)
    {
        List<StackPiece> parameters = irFunction.Parameters.Select(x => new StackPiece(x.Type, x)).ToList();
        StackPiece returnedValue = new StackPiece(irFunction.ReturnType);
        StackPiece controlLink = new StackPiece(Address.ToIrType());
        List<StackPiece> savedMachineState = new() { new StackPiece(Address.ToIrType()) };
        List<StackPiece> localVariables = irFunction.Locals.Where(x => !x.IsTemporary).Select(x => new StackPiece(x.Type, x)).ToList();
        List<StackPiece> temporaryVariables = irFunction.Locals.Where(x => x.IsTemporary).Select(x => new StackPiece(x.Type, x)).ToList();

        StackFrame stackFrame = new()
        {
            Parameters = parameters,
            ReturnedValue = returnedValue,
            ControlLink = controlLink,
            SavedMachineState = savedMachineState,
            LocalVariables = localVariables,
            TemporaryVariables = temporaryVariables
        };

        return stackFrame;
    }

}
