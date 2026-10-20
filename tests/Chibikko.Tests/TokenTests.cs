using Chibikko.Frontend;

namespace Chibikko.Tests;

public class TokenTests
{
    [Fact]
    public void Token_HoldsKindAndSpan()
    {
        var token = new Token(TokenKind.Ident, 4, 3);

        Assert.Equal(TokenKind.Ident, token.Kind);
        Assert.Equal(4, token.Offset);
        Assert.Equal(3, token.Length);
    }
}
