using System.Text;

namespace Chibikko.Common.Ir;

/// <summary>
/// IR をテキストに書き出す(-emit-ir 用)。
/// </summary>
public static class Dump
{
    public static string ToText(Module module)
    {
        var builder = new StringBuilder();
        foreach (var function in module.Functions)
        {
            Write(builder, function);
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, Function function)
    {
        var parameters = string.Join(", ", function.Parameters.Select(parameter => $"{TypeName(parameter.Type)} {parameter}"));
        builder.AppendLine($"func {TypeName(function.ReturnType)} {function.Name}({parameters}) {{");
        foreach (var local in function.Locals)
        {
            builder.AppendLine($"  local {TypeName(local.Type)} {local}");
        }
        foreach (var block in function.Blocks)
        {
            builder.AppendLine($"{block.Name}:");
            foreach (var instruction in block.Instructions)
            {
                builder.AppendLine($"  {Format(instruction)}");
            }
        }
        builder.AppendLine("}");
    }

    public static string Format(Instruction instruction) => instruction.Operation switch
    {
        OperationKind.Jump => $"jump {instruction.Target}",
        OperationKind.Branch => $"branch {instruction.First}, {instruction.Target}, {instruction.ElseTarget}",
        OperationKind.Return => instruction.First is null ? "ret" : $"ret {instruction.First}",
        OperationKind.Call => (instruction.Destination is null ? "" : $"{instruction.Destination} = ")
                       + $"call {instruction.Callee}({string.Join(", ", instruction.Arguments)})",
        OperationKind.Copy => $"{instruction.Destination} = {instruction.First}",
        OperationKind.Negate => $"{instruction.Destination} = neg {instruction.First}",
        OperationKind.Convert => $"{instruction.Destination} = convert {TypeName(instruction.First!.Type)} {instruction.First} to {TypeName(instruction.Destination!.Type)}",
        _ => $"{instruction.Destination} = {instruction.Operation.ToString().ToLowerInvariant()} {TypeName(instruction.First!.Type)} {instruction.First}, {instruction.Second}",
    };

    public static string TypeName(IrType type) => type.ToString().ToLowerInvariant();
}
