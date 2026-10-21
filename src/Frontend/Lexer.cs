using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class Lexer(string source)
{
    static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        ["return"] = TokenKind.ReturnKeyword,
        ["if"] = TokenKind.IfKeyword,
        ["else"] = TokenKind.ElseKeyword,
        ["while"] = TokenKind.WhileKeyword,
        ["for"] = TokenKind.ForKeyword,
        ["true"] = TokenKind.TrueKeyword,
        ["false"] = TokenKind.FalseKeyword,
        ["int"] = TokenKind.IntKeyword,
        ["long"] = TokenKind.LongKeyword,
        ["void"] = TokenKind.VoidKeyword,
        ["bool"] = TokenKind.BoolKeyword,
        ["char"] = TokenKind.CharKeyword,
        ["float"] = TokenKind.FloatKeyword,
        ["double"] = TokenKind.DoubleKeyword,
    };

    readonly string source = source;
    int position;

    // 呼ばれるたびに次の Token を 1 つ返す。末尾では EndOfFile を返し続ける
    public Token Lex()
    {
        SkipWhitespaceAndComments();

        var current = Peek();
        if (current == '\0')
        {
            return new Token(TokenKind.EndOfFile, position, 0);
        }
        if (char.IsAsciiDigit(current))
        {
            return LexNumber();
        }
        if (IsIdentifierStart(current))
        {
            return LexIdentifierOrKeyword();
        }
        if (current == '\'')
        {
            return LexCharacterLiteral();
        }
        return LexPunctuator();
    }

    void SkipWhitespaceAndComments()
    {
        while (true)
        {
            if (char.IsWhiteSpace(Peek()))
            {
                position++;
            }
            else if (Peek() == '/' && Peek(1) == '/')
            {
                while (Peek() is not ('\n' or '\0'))
                {
                    position++;
                }
            }
            else if (Peek() == '/' && Peek(1) == '*')
            {
                var start = position;
                position += 2;
                while (!(Peek() == '*' && Peek(1) == '/'))
                {
                    if (Peek() == '\0')
                    {
                        throw new CompileException("unterminated comment", start);
                    }
                    position++;
                }
                position += 2;
            }
            else
            {
                return;
            }
        }
    }

    // integer ::= [0-9]+ [lL]?
    // float   ::= [0-9]+ "." [0-9]+ [fF]?
    Token LexNumber()
    {
        var start = position;
        SkipDigits();

        if (Peek() == '.' && char.IsAsciiDigit(Peek(1)))
        {
            position++;
            SkipDigits();
            if (Peek() is 'f' or 'F')
            {
                position++;
            }
            return MakeToken(TokenKind.FloatLiteral, start);
        }

        if (Peek() is 'l' or 'L')
        {
            position++;
        }
        return MakeToken(TokenKind.IntegerLiteral, start);
    }

    Token LexIdentifierOrKeyword()
    {
        var start = position;
        while (IsIdentifierStart(Peek()) || char.IsAsciiDigit(Peek()))
        {
            position++;
        }
        var spelling = source[start..position];
        var kind = Keywords.GetValueOrDefault(spelling, TokenKind.Identifier);
        return MakeToken(kind, start);
    }

    // char ::= "'" ([^'\\\n] | "\\" [nrt0\\']) "'"
    Token LexCharacterLiteral()
    {
        var start = position;
        position++;

        if (Peek() == '\\')
        {
            position++;
            if (Peek() is not ('n' or 'r' or 't' or '0' or '\\' or '\''))
            {
                throw new CompileException("unknown escape sequence", position - 1);
            }
            position++;
        }
        else if (Peek() is '\'' or '\n' or '\0')
        {
            throw new CompileException("empty or unterminated character literal", start);
        }
        else
        {
            position++;
        }

        if (Peek() != '\'')
        {
            throw new CompileException("unterminated character literal", start);
        }
        position++;
        return MakeToken(TokenKind.CharLiteral, start);
    }

    Token LexPunctuator()
    {
        var start = position;

        // 2 文字の演算子を先に判定する(最長一致)
        TokenKind? twoCharacter = (Peek(), Peek(1)) switch
        {
            ('=', '=') => TokenKind.EqualEqual,
            ('!', '=') => TokenKind.NotEqual,
            ('<', '=') => TokenKind.LessEqual,
            ('>', '=') => TokenKind.GreaterEqual,
            _ => null,
        };
        if (twoCharacter is { } twoCharacterKind)
        {
            position += 2;
            return MakeToken(twoCharacterKind, start);
        }

        TokenKind? oneCharacter = Peek() switch
        {
            '(' => TokenKind.LeftParen,
            ')' => TokenKind.RightParen,
            '{' => TokenKind.LeftBrace,
            '}' => TokenKind.RightBrace,
            ';' => TokenKind.Semicolon,
            ',' => TokenKind.Comma,
            '+' => TokenKind.Plus,
            '-' => TokenKind.Minus,
            '*' => TokenKind.Star,
            '/' => TokenKind.Slash,
            '<' => TokenKind.Less,
            '>' => TokenKind.Greater,
            '=' => TokenKind.Assign,
            _ => null,
        };
        if (oneCharacter is { } oneCharacterKind)
        {
            position++;
            return MakeToken(oneCharacterKind, start);
        }

        throw new CompileException($"unexpected character '{Peek()}'", start);
    }

    void SkipDigits()
    {
        while (char.IsAsciiDigit(Peek()))
        {
            position++;
        }
    }

    Token MakeToken(TokenKind kind, int start) => new(kind, start, position - start);

    static bool IsIdentifierStart(char character) => char.IsAsciiLetter(character) || character == '_';

    char Peek(int offset = 0) => position + offset < source.Length ? source[position + offset] : '\0';
}
