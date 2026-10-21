namespace Chibikko.Common;

public enum BuiltinKind
{
    Void,
    Bool,
    Char,
    Int,
    Long,
    Float,
    Double,
}

/// <summary>
/// C の組み込み型。Clang の ASTContext と同様に型ごとのインスタンスは 1 つだけなので、参照の比較で同一判定できる。
/// </summary>
public sealed class BuiltinType
{
    public static readonly BuiltinType Void = new(BuiltinKind.Void, "void");
    public static readonly BuiltinType Bool = new(BuiltinKind.Bool, "bool");
    public static readonly BuiltinType Char = new(BuiltinKind.Char, "char");
    public static readonly BuiltinType Int = new(BuiltinKind.Int, "int");
    public static readonly BuiltinType Long = new(BuiltinKind.Long, "long");
    public static readonly BuiltinType Float = new(BuiltinKind.Float, "float");
    public static readonly BuiltinType Double = new(BuiltinKind.Double, "double");

    public BuiltinKind Kind { get; }
    public string Name { get; }

    BuiltinType(BuiltinKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    public bool IsInteger => Kind is BuiltinKind.Bool or BuiltinKind.Char or BuiltinKind.Int or BuiltinKind.Long;
    public bool IsFloating => Kind is BuiltinKind.Float or BuiltinKind.Double;
    public bool IsArithmetic => IsInteger || IsFloating;

    public override string ToString() => Name;

    // 整数拡張: int より小さい整数型は int として計算する
    public static BuiltinType Promote(BuiltinType type) =>
        type.Kind is BuiltinKind.Bool or BuiltinKind.Char ? Int : type;

    // 通常算術変換: 二項演算の両辺をそろえる型を決める
    public static BuiltinType UsualArithmeticConversion(BuiltinType left, BuiltinType right)
    {
        if (left == Double || right == Double)
        {
            return Double;
        }
        if (left == Float || right == Float)
        {
            return Float;
        }
        if (Promote(left) == Long || Promote(right) == Long)
        {
            return Long;
        }
        return Int;
    }
}
