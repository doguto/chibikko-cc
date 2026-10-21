using System.Text;

namespace Chibikko.Common.Ir;

/// <summary>
/// IR をテキストに書き出す(-emit-ir 用)。
/// </summary>
public static class Dump
{
    public static string ToText(Module module)
    {
        var sb = new StringBuilder();
        foreach (var function in module.Functions)
        {
            Write(sb, function);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, Function function)
    {
        var parameters = string.Join(", ", function.Params.Select(p => $"{TypeName(p.Type)} {p}"));
        sb.AppendLine($"func {TypeName(function.ReturnType)} {function.Name}({parameters}) {{");
        foreach (var local in function.Locals)
        {
            sb.AppendLine($"  local {TypeName(local.Type)} {local}");
        }
        foreach (var block in function.Blocks)
        {
            sb.AppendLine($"{block.Name}:");
            foreach (var inst in block.Instructions)
            {
                sb.AppendLine($"  {Format(inst)}");
            }
        }
        sb.AppendLine("}");
    }

    public static string Format(Instruction inst) => inst.Op switch
    {
        Opcode.Jump => $"jump {inst.Target}",
        Opcode.Branch => $"branch {inst.A}, {inst.Target}, {inst.ElseTarget}",
        Opcode.Ret => inst.A is null ? "ret" : $"ret {inst.A}",
        Opcode.Call => (inst.Dest is null ? "" : $"{inst.Dest} = ")
                       + $"call {inst.Callee}({string.Join(", ", inst.Args)})",
        Opcode.Copy => $"{inst.Dest} = {inst.A}",
        Opcode.Neg => $"{inst.Dest} = neg {inst.A}",
        Opcode.Convert => $"{inst.Dest} = convert {TypeName(inst.A!.Type)} {inst.A} to {TypeName(inst.Dest!.Type)}",
        _ => $"{inst.Dest} = {inst.Op.ToString().ToLowerInvariant()} {TypeName(inst.A!.Type)} {inst.A}, {inst.B}",
    };

    public static string TypeName(IrType type) => type.ToString().ToLowerInvariant();
}
