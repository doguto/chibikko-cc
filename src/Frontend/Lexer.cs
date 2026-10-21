namespace Chibikko.Frontend;

public sealed class Lexer(string source)
{
    readonly string source = source;
    int pos;

    public Token Lex()
    {
        throw new NotImplementedException("lexer");
    }

    void SkipWhitespaceAndComments()
    {
        throw new NotImplementedException("lexer");
    }

    Token LexNumber()
    {
        throw new NotImplementedException("lexer");
    }

    Token LexIdentOrKeyword()
    {
        throw new NotImplementedException("lexer");
    }

    Token LexCharLit()
    {
        throw new NotImplementedException("lexer");
    }

    Token LexPunct()
    {
        throw new NotImplementedException("lexer");
    }

    char Peek(int offset = 0) => pos + offset < source.Length ? source[pos + offset] : '\0';
}
