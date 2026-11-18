using Chibikko.Common.Ir;

namespace Chibikko.Backend.Type;

public class StackFrame
{
    public const int MemoryArgumentsOffset = 16;
    public const int ReturnAddressOffset = 8;
    public const int PreviousBasePointerOffset = 0;
    public const int Alignment = 16;

    public required List<StackPiece> MemoryArguments { get; init; }
    public required List<StackPiece> RegisterParameters { get; init; }
    public required List<StackPiece> LocalVariables { get; init; }
    public required List<StackPiece> TemporaryVariables { get; init; }

    public int Size
    {
        get
        {
            var size = -Layout().Bottom;
            return (size + Alignment - 1) / Alignment * Alignment;
        }
    }

    (Dictionary<Variable, int> Offsets, int Bottom)? layout;

    public int OffsetOf(Variable variable) =>
        Layout().Offsets.TryGetValue(variable, out var offset)
            ? offset
            : throw new InvalidOperationException($"variable not found in stack frame: {variable.Name ?? "<temp>"}");

    (Dictionary<Variable, int> Offsets, int Bottom) Layout()
    {
        if (layout is { } cached) return cached;

        Dictionary<Variable, int> offsets = new(ReferenceEqualityComparer.Instance);

        var above = MemoryArgumentsOffset;
        foreach (var piece in MemoryArguments)
        {
            if (piece.Variable is { } variable) offsets[variable] = above;
            above += 8;
        }

        var below = PreviousBasePointerOffset;
        foreach (var piece in RegisterParameters.Concat(LocalVariables).Concat(TemporaryVariables))
        {
            var width = Math.Max(piece.Width, 1);
            below -= width;
            below = -((-below + width - 1) / width * width);
            if (piece.Variable is { } variable) offsets[variable] = below;
        }

        layout = (offsets, below);
        return layout.Value;
    }
}

public record StackPiece(IrType? type, Variable? Variable = null)
{
    public int Width => type.HasValue ? type.Value.GetWidth() : 0;
}
