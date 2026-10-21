using Chibikko.Frontend;

namespace Chibikko.Tests.Frontend;

public static class LexerExtensions
{
    public static List<Token> Tokenize(this Lexer lexer)
    {
        var tokens = new List<Token>();
        Token token;
        do
        {
            token = lexer.Lex();
            tokens.Add(token);
        } while (token.Kind != TokenKind.Eof);
        return tokens;
    }
}
