using Chibikko.Frontend;
using Chibikko.Frontend.Type;

namespace Chibikko.Tests.Frontend;

public class TokenTests
{
    [Fact]
    public void Token_HoldsKindAndSpan()
    {
        var token = new Token(TokenKind.Identifier, "foo", 4);

        Assert.Equal(TokenKind.Identifier, token.Kind);
        Assert.Equal("foo", token.Text);
        Assert.Equal(4, token.Offset);
        Assert.Equal(3, token.Length);
    }
}
