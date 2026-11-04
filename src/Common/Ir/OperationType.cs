namespace Chibikko.Common.Ir;

public enum OperationType
{
    Assign,

    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,

    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,

    Label,
    Jump,
    JumpIfFalse,

    Param,              // 関数呼び出しの際には Parameter を Param で Call の前に宣言する
    Call,
    Return,
}
