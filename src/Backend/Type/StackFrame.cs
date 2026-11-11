using Chibikko.Common;
using Chibikko.Common.Ir;

namespace Chibikko.Backend.Type;

public class StackFrame
{
    public required List<StackPiece> Parameters { get; init; }
    public required StackPiece ReturnedValue { get; init; }
    public required StackPiece ControlLink { get; init; }
    public required List<StackPiece> SavedMachineState { get; init; }
    public required List<StackPiece> LocalVariables { get; init; }
    public required List<StackPiece> TemporaryVariables { get; init; }

    public int ParametersOffset => 0;
    public int ReturnedValueOffset => ParametersOffset + Parameters.Sum(x => x.Width);
    public int ControlLinkOffset => ReturnedValueOffset + ReturnedValue.Width;
    public int SavedMachineStateOffset => ControlLinkOffset + ControlLink.Width;
    public int LocalVariablesOffset => SavedMachineStateOffset + SavedMachineState.Sum(x => x.Width);
    public int TemporaryVariablesOffset => LocalVariablesOffset + LocalVariables.Sum(x => x.Width);
    public int Size => TemporaryVariablesOffset + TemporaryVariables.Sum(x => x.Width);

    Dictionary<Variable, int>? variableOffsets;

    public int OffsetOf(Variable variable) =>
        VariableOffsets().TryGetValue(variable, out var offset)
            ? offset
            : throw new InvalidOperationException($"variable not found in stack frame: {variable.Name ?? "<temp>"}");

    Dictionary<Variable, int> VariableOffsets()
    {
        if (variableOffsets is not null) return variableOffsets;

        variableOffsets = new(ReferenceEqualityComparer.Instance);
        PlaceVariables(Parameters, ParametersOffset);
        PlaceVariables(LocalVariables, LocalVariablesOffset);
        PlaceVariables(TemporaryVariables, TemporaryVariablesOffset);
        return variableOffsets;
    }

    void PlaceVariables(List<StackPiece> pieces, int offset)
    {
        foreach (var piece in pieces)
        {
            if (piece.Variable is { } variable) variableOffsets![variable] = offset;
            offset += piece.Width;
        }
    }
}

public record StackPiece(IrType? type, Variable? Variable = null)
{
    public int Width => type.HasValue ? type.Value.GetWidth() : 0;
}
