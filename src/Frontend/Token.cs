namespace Chibikko.Frontend;

public enum TokenKind
{
    EndOfFile,
    Identifier,

    // リテラル
    IntegerLiteral,
    FloatLiteral,
    CharLiteral,

    // キーワード
    ReturnKeyword,
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    ForKeyword,
    TrueKeyword,
    FalseKeyword,

    // 型
    IntKeyword,
    LongKeyword,
    VoidKeyword,
    BoolKeyword,
    CharKeyword,
    FloatKeyword,
    DoubleKeyword,

    // 記号
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Semicolon,
    Comma,

    // 算術演算子
    Plus,
    Minus,
    Star,
    Slash,

    // 比較演算子
    EqualEqual,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // 代入演算子
    Assign,
}

/// <summary>
/// ソース上の綴り(Offset, Length)のみを保持する。値の変換は後段で行う。
/// </summary>
public readonly record struct Token(TokenKind Kind, int Offset, int Length);
