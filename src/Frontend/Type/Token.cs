namespace Chibikko.Frontend.Type;

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

// ソース上の綴り(Text)と位置(Offset)を保持する。値の変換は後段で行う。
public readonly record struct Token(TokenKind Kind, string Text, int Offset)
{
    public int Length => Text.Length;
}
