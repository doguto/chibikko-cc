using System.Diagnostics;

namespace Chibikko.Common.Ir;

public enum IrType
{
    I1,     // Bool
    I8,     // byte, char 等が使用
    I16,
    I32,
    I64,
    F32,
    F64,
}

public static class TypeExtention
{
    public static IrType ToIrType(this BuiltinType type)
    {
        return type.Kind switch
        {
            BuiltinKind.Bool => IrType.I1,
            BuiltinKind.Char => IrType.I8,
            BuiltinKind.Int => IrType.I32,
            BuiltinKind.Long => IrType.I64,
            BuiltinKind.Float => IrType.F32,
            BuiltinKind.Double => IrType.F64,
            BuiltinKind.Void => throw new InvalidOperationException("void has no IrType"),
            _ => throw new UnreachableException($"Unknown BuiltinKind: {type.Kind}"),
        };
    }
}
