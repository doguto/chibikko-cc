namespace Chibikko.Common.Ir;

/// <summary>
/// IR 上の値の型。C の型は Sema で解決し、IrGen でここへ落とす。
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

public enum Opcode
{
    // 算術: Dest = A op B
    Add,
    Sub,
    Mul,
    Div,

    // 単項: Dest = -A
    Neg,

    // 比較: Dest(i1) = A op B
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,

    // 代入: Dest = A
    Copy,

    // 型変換: Dest = (Dest.Type) A
    Convert,

    // 制御: 基本ブロックの末尾にのみ置く
    Jump,   // goto Target
    Branch, // if A goto Target else ElseTarget
    Ret,    // return A(A は null 可)

    // 呼び出し: Dest = Callee(Args...)(Dest は null 可)
    Call,
}

/// <summary>
/// 命令のオペランド。四つ組なので値は名前で参照する。
/// </summary>
public abstract record Operand(IrType Type);

/// <summary>コンパイラが生成する一時変数(t0, t1, ...)。</summary>
public sealed record Temp(int Id, IrType Type) : Operand(Type)
{
    public override string ToString() => $"t{Id}";
}

/// <summary>ソース上の変数・引数。スタックスロットに割り当てる。</summary>
public sealed record Var(string Name, IrType Type) : Operand(Type)
{
    public override string ToString() => Name;
}

public sealed record IntConst(long Value, IrType Type) : Operand(Type)
{
    public override string ToString() => Value.ToString();
}

public sealed record FloatConst(double Value, IrType Type) : Operand(Type)
{
    public override string ToString() => Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// 四つ組 (Op, A, B, Dest)。制御命令は Target / ElseTarget に飛び先のブロック名を持つ。
/// </summary>
public sealed class Instruction(Opcode op, Operand? dest = null, Operand? a = null, Operand? b = null)
{
    public Opcode Op { get; set; } = op;
    public Operand? Dest { get; set; } = dest;
    public Operand? A { get; set; } = a;
    public Operand? B { get; set; } = b;

    public string? Target { get; init; }
    public string? ElseTarget { get; init; }

    public string? Callee { get; init; }
    public List<Operand> Args { get; init; } = [];

    public bool IsTerminator => Op is Opcode.Jump or Opcode.Branch or Opcode.Ret;
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
    public List<Var> Params { get; } = [];

    /// <summary>引数以外のローカル変数。</summary>
    public List<Var> Locals { get; } = [];

    public List<BasicBlock> Blocks { get; } = [];

    /// <summary>次に払い出す一時変数の番号。</summary>
    public int NextTempId { get; set; }
}

public sealed class Module
{
    public List<Function> Functions { get; } = [];
}
