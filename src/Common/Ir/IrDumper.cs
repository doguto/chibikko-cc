using System.Globalization;
using System.Text;

namespace Chibikko.Common.Ir;

public static class IrDumper
{
    public static string Dump(Ir ir)
    {
        var builder = new StringBuilder();
        DumpFunctionTable(builder, ir);
        foreach (var function in ir.Functions)
        {
            builder.AppendLine();
            DumpFunction(builder, function);
        }
        return builder.ToString();
    }

    static void DumpFunctionTable(StringBuilder builder, Ir ir)
    {
        builder.AppendLine("function_table:");
        foreach (var function in ir.Functions)
        {
            var parameters = string.Join(", ", function.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            builder.AppendLine($"  @{function.Name}({parameters}) -> {ReturnTypeName(function)}");
        }
    }

    static void DumpFunction(StringBuilder builder, IrFunction function)
    {
        var names = new Names();
        foreach (var variable in function.Parameters.Concat(function.Locals))
        {
            names.Of(variable);
        }

        var parameters = string.Join(", ", function.Parameters.Select(p => $"{names.Of(p)}: {p.Type}"));
        builder.AppendLine($"function @{function.Name}({parameters}) -> {ReturnTypeName(function)}");

        builder.AppendLine("  var_table:");
        foreach (var variable in function.Parameters)
        {
            builder.AppendLine($"    {names.Of(variable),-6} {variable.Type,-4} param");
        }
        foreach (var variable in function.Locals)
        {
            var kind = variable.Name is null ? "temp" : "local";
            builder.AppendLine($"    {names.Of(variable),-6} {variable.Type,-4} {kind}");
        }

        builder.AppendLine("  body:");
        var rows = function.Body.Select(q => QuadrupleFields(q, names)).ToList();
        var widths = Enumerable.Range(0, 3)
            .Select(i => rows.Select(row => row[i].Length).DefaultIfEmpty(0).Max() + 1)
            .ToArray();
        foreach (var row in rows)
        {
            var aligned = Enumerable.Range(0, 3).Select(i => (row[i] + ",").PadRight(widths[i]));
            builder.AppendLine($"    ({string.Join(" ", aligned)} {row[3]})");
        }
    }

    static string ReturnTypeName(IrFunction function) => function.ReturnType?.ToString() ?? "void";

    static string[] QuadrupleFields(Quadruple quadruple, Names names)
    {
        string Of(Operand? operand) => operand is null ? "_" : names.Of(operand);

        return [quadruple.OperationType.ToString(), Of(quadruple.Destination), Of(quadruple.Source1), Of(quadruple.Source2)];
    }

    sealed class Names
    {
        readonly Dictionary<Operand, string> names = new(ReferenceEqualityComparer.Instance);
        int variableCount;
        int labelCount;

        public string Of(Operand operand) => operand switch
        {
            Variable variable => names.TryGetValue(variable, out var name) ? name : names[variable] = NameOf(variable),
            Label label => names.TryGetValue(label, out var name) ? name : names[label] = $"L{labelCount++}",
            IntegerConstant constant => $"{constant.Value}:{constant.Type}",
            FloatConstant constant => $"{constant.Value.ToString("R", CultureInfo.InvariantCulture)}:{constant.Type}",
            FunctionRef function => $"@{function.Function.Name}",
            _ => throw new NotSupportedException($"Unsupported operand: {operand.GetType().Name}"),
        };

        string NameOf(Variable variable)
        {
            var id = variableCount++;
            return variable.Name is null ? $"%{id}" : $"%{id}.{variable.Name}";
        }
    }
}
