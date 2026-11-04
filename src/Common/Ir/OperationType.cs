using System.Diagnostics;
using Chibikko.Frontend.Type;

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

public static class OperationTypeExtension
{
    public static OperationType ToOperationType(this TokenKind kind) => kind switch
    {
        TokenKind.Plus => OperationType.Add,
        TokenKind.Minus => OperationType.Subtract,
        TokenKind.Star => OperationType.Multiply,
        TokenKind.Slash => OperationType.Divide,
        TokenKind.EqualEqual => OperationType.Equal,
        TokenKind.NotEqual => OperationType.NotEqual,
        TokenKind.Less => OperationType.LessThan,
        TokenKind.LessEqual => OperationType.LessThanOrEqual,
        TokenKind.Greater => OperationType.GreaterThan,
        TokenKind.GreaterEqual => OperationType.GreaterThanOrEqual,
        _ => throw new UnreachableException($"Unknown binary operator: {kind}"),
    };
}
