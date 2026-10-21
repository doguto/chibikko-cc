namespace Chibikko.Common.Ir;

/// <summary>
/// IR 上の値の型。C の型は SemanticAnalyzer で解決し、IrGenerator でここへ落とす。
/// </summary>
public enum IrType
{
    Void,
    I1,
    I8,
    I32,
    I64,
    F32,
    F64,
}

public enum OperationKind
{
    // 算術: Destination = First operation Second
    Add,
    Subtract,
    Multiply,
    Divide,

    // 単項: Destination = -First
    Negate,

    // 比較: Destination(i1) = First operation Second
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // 代入: Destination = First
    Copy,

    // 型変換: Destination = (Destination.Type) First
    Convert,

    // 制御: 基本ブロックの末尾にのみ置く
    Jump,   // goto Target
    Branch, // if First goto Target else ElseTarget
    Return, // return First(First は null 可)

    // 呼び出し: Destination = Callee(Arguments...)(Destination は null 可)
    Call,
}

/// <summary>
/// 命令のオペランド。四つ組なので値は名前で参照する。
/// </summary>
public abstract record Operand(IrType Type);

/// <summary>コンパイラが生成する一時変数(t0, t1, ...)。</summary>
public sealed record Temporary(int Id, IrType Type) : Operand(Type)
{
    public override string ToString() => $"t{Id}";
}

/// <summary>ソース上の変数・引数。スタックスロットに割り当てる。</summary>
public sealed record Variable(string Name, IrType Type) : Operand(Type)
{
    public override string ToString() => Name;
}

public sealed record IntegerConstant(long Value, IrType Type) : Operand(Type)
{
    public override string ToString() => Value.ToString();
}

public sealed record FloatConstant(double Value, IrType Type) : Operand(Type)
{
    public override string ToString() => Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// 四つ組 (Operation, First, Second, Destination)。制御命令は Target / ElseTarget に飛び先のブロック名を持つ。
/// </summary>
public sealed class Instruction(OperationKind operation, Operand? destination = null, Operand? first = null, Operand? second = null)
{
    public OperationKind Operation { get; set; } = operation;
    public Operand? Destination { get; set; } = destination;
    public Operand? First { get; set; } = first;
    public Operand? Second { get; set; } = second;

    public string? Target { get; init; }
    public string? ElseTarget { get; init; }

    public string? Callee { get; init; }
    public List<Operand> Arguments { get; init; } = [];

    public bool IsTerminator => Operation is OperationKind.Jump or OperationKind.Branch or OperationKind.Return;
}

public sealed class BasicBlock(string name)
{
    public string Name { get; } = name;
    public List<Instruction> Instructions { get; } = [];
}

public sealed class Function(string name, IrType returnType)
{
    public string Name { get; } = name;
    public IrType ReturnType { get; } = returnType;
    public List<Variable> Parameters { get; } = [];

    /// <summary>引数以外のローカル変数。</summary>
    public List<Variable> Locals { get; } = [];

    public List<BasicBlock> Blocks { get; } = [];

    /// <summary>次に払い出す一時変数の番号。</summary>
    public int NextTemporaryId { get; set; }
}

public sealed class Module
{
    public List<Function> Functions { get; } = [];
}
