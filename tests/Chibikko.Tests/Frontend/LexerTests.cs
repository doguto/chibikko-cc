using Chibikko.Common;
using Chibikko.Frontend;

namespace Chibikko.Tests.Frontend;

public class LexerTests
{
    static List<TokenKind> Kinds(string source) =>
        new Lexer(source).Tokenize().Select(token => token.Kind).ToList();

    [Fact]
    public void EmptySource_ReturnsEndOfFile()
    {
        Assert.Equal([TokenKind.EndOfFile], Kinds(""));
    }

    [Fact]
    public void Keywords_AreDistinguishedFromIdentifiers()
    {
        Assert.Equal(
            [TokenKind.IntKeyword, TokenKind.Identifier, TokenKind.ReturnKeyword, TokenKind.Identifier, TokenKind.EndOfFile],
            Kinds("int main return returnValue"));
    }

    [Fact]
    public void TwoCharacterOperators_UseLongestMatch()
    {
        Assert.Equal(
            [TokenKind.EqualEqual, TokenKind.Assign, TokenKind.LessEqual, TokenKind.Less, TokenKind.NotEqual, TokenKind.EndOfFile],
            Kinds("== = <= < !="));
    }

    [Theory]
    [InlineData("42", TokenKind.IntegerLiteral)]
    [InlineData("42L", TokenKind.IntegerLiteral)]
    [InlineData("3.14", TokenKind.FloatLiteral)]
    [InlineData("3.14f", TokenKind.FloatLiteral)]
    [InlineData("'a'", TokenKind.CharLiteral)]
    [InlineData("'\\n'", TokenKind.CharLiteral)]
    public void Literal_IsSingleToken(string source, TokenKind expected)
    {
        var token = new Lexer(source).Lex();

        Assert.Equal(expected, token.Kind);
        Assert.Equal(0, token.Offset);
        Assert.Equal(source.Length, token.Length);
    }

    [Fact]
    public void Token_HoldsSourcePosition()
    {
        var tokens = new Lexer("  foo = 1;").Tokenize();

        Assert.Equal(new Token(TokenKind.Identifier, 2, 3), tokens[0]);
        Assert.Equal(new Token(TokenKind.Assign, 6, 1), tokens[1]);
        Assert.Equal(new Token(TokenKind.IntegerLiteral, 8, 1), tokens[2]);
    }

    [Fact]
    public void Comments_AreSkipped()
    {
        Assert.Equal(
            [TokenKind.IntKeyword, TokenKind.Identifier, TokenKind.EndOfFile],
            Kinds("// line\nint /* block */ x"));
    }

    [Theory]
    [InlineData("@")]
    [InlineData("'ab'")]
    [InlineData("/* never closed")]
    public void InvalidInput_Throws(string source)
    {
        Assert.Throws<CompileException>(() => new Lexer(source).Tokenize());
    }
}
