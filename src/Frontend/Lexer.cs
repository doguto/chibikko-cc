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
        if (current is '\0')
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
        if (current is '\'')
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
            else if ((Peek(), Peek(1)) is ('/', '/'))
            {
                while (Peek() is not ('\n' or '\0'))
                {
                    position++;
                }
            }
            else if ((Peek(), Peek(1)) is ('/', '*'))
            {
                var start = position;
                position += 2;
                while ((Peek(), Peek(1)) is not ('*', '/'))
                {
                    if (Peek() is '\0')
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

        if (Peek() is '.' && char.IsAsciiDigit(Peek(1)))
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

        if (Peek() is '\\')
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

        if (Peek() is not '\'')
        {
            throw new CompileException("unterminated character literal", start);
        }
        position++;
        return MakeToken(TokenKind.CharLiteral, start);
    }

    Token LexPunctuator()
    {
        var start = position;

        // 2 文字の演算子を先に並べる(最長一致)
        var (kind, length) = (Peek(), Peek(1)) switch
        {
            ('=', '=') => (TokenKind.EqualEqual, 2),
            ('!', '=') => (TokenKind.NotEqual, 2),
            ('<', '=') => (TokenKind.LessEqual, 2),
            ('>', '=') => (TokenKind.GreaterEqual, 2),
            ('(', _) => (TokenKind.LeftParen, 1),
            (')', _) => (TokenKind.RightParen, 1),
            ('{', _) => (TokenKind.LeftBrace, 1),
            ('}', _) => (TokenKind.RightBrace, 1),
            (';', _) => (TokenKind.Semicolon, 1),
            (',', _) => (TokenKind.Comma, 1),
            ('+', _) => (TokenKind.Plus, 1),
            ('-', _) => (TokenKind.Minus, 1),
            ('*', _) => (TokenKind.Star, 1),
            ('/', _) => (TokenKind.Slash, 1),
            ('<', _) => (TokenKind.Less, 1),
            ('>', _) => (TokenKind.Greater, 1),
            ('=', _) => (TokenKind.Assign, 1),
            _ => throw new CompileException($"unexpected character '{Peek()}'", start),
        };

        position += length;
        return MakeToken(kind, start);
    }

    void SkipDigits()
    {
        while (char.IsAsciiDigit(Peek()))
        {
            position++;
        }
    }

    Token MakeToken(TokenKind kind, int start) => new(kind, start, position - start);

    static bool IsIdentifierStart(char character) => char.IsAsciiLetter(character) || character is '_';

    char Peek(int offset = 0) => position + offset < source.Length ? source[position + offset] : '\0';
}
