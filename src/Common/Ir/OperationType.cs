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

    Call,
    Return,
}
