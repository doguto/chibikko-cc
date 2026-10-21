using Chibikko.Frontend;

namespace Chibikko.Tests.Frontend;

public class TokenTests
{
    [Fact]
    public void Token_HoldsKindAndSpan()
    {
        var token = new Token(TokenKind.Identifier, 4, 3);

        Assert.Equal(TokenKind.Identifier, token.Kind);
        Assert.Equal(4, token.Offset);
        Assert.Equal(3, token.Length);
    }
}
