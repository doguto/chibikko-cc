using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class TokenStream(Lexer lexer)
{
    readonly Lexer lexer = lexer;
    readonly List<Token> lookahead = [];

    // 文法決定の為、n個先の Token を先読みする
    public Token Peek(int n = 0)
    {
        while (lookahead.Count <= n)
        {
            lookahead.Add(lexer.Lex());
        }
        return lookahead[n];
    }

    // 現在の Token の　AST　上の位置を確定する
    public Token Consume()
    {
        var token = Peek();
        lookahead.RemoveAt(0);
        return token;
    }

    // 現状の文法上次に来る Token が確定している場合に、その Token の AST 上の位置を確定する.
    // もし期待した Token が得られなければ、 CompileError
    public Token Expect(TokenKind kind)
    {
        var token = Peek();
        if (token.Kind != kind)
        {
            throw new CompileException($"expected {kind}, found {token.Kind}", token.Offset);
        }
        return Consume();
    }
}
