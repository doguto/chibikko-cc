namespace Chibikko.Frontend;

public enum TokenKind
{
    Eof,
    Ident,

    // リテラル
    IntLit,
    FloatLit,
    CharLit,

    // キーワード
    KwReturn,
    KwIf,
    KwElse,
    KwWhile,
    KwFor,
    KwTrue,
    KwFalse,

    // 型
    KwInt,
    KwLong,
    KwVoid,
    KwBool,
    KwChar,
    KwFloat,
    KwDouble,

    // 記号
    LParen,
    RParen,
    LBrace,
    RBrace,
    Semi,
    Comma,

    // 算術演算子
    Plus,
    Minus,
    Star,
    Slash,

    // 比較演算子
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,

    // 代入演算子
    Assign,
}

/// <summary>
/// ソース上の綴り(Offset, Length)のみを保持する。値の変換は後段で行う。
/// </summary>
public readonly record struct Token(TokenKind Kind, int Offset, int Length);
