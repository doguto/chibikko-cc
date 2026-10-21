using Chibikko.Common;
using Chibikko.Frontend;

namespace Chibikko.Tests.Frontend;

public class ParserTests
{
    static TranslationUnit Parse(string source) => new Parser(new Lexer(source)).Parse();

    // 関数 main の本体だけを取り出す
    static List<Statement> ParseBody(string body) =>
        Parse($"int main() {{ {body} }}").Functions[0].Body.Statements;

    static Expression ParseExpression(string expression) =>
        Assert.IsType<ExpressionStatement>(ParseBody($"{expression};")[0]).Expression;

    [Fact]
    public void Function_HasParametersAndBody()
    {
        var unit = Parse("int add(int a, long b) { return a; }");

        var function = Assert.Single(unit.Functions);
        Assert.Equal(TokenKind.IntKeyword, function.ReturnType.Kind);
        Assert.Equal(2, function.Parameters.Count);
        Assert.Equal(TokenKind.LongKeyword, function.Parameters[1].Type.Kind);
        Assert.IsType<ReturnStatement>(Assert.Single(function.Body.Statements));
    }

    [Fact]
    public void Multiplication_BindsTighterThanAddition()
    {
        // 1 + 2 * 3 => 1 + (2 * 3)
        var addition = Assert.IsType<BinaryExpression>(ParseExpression("1 + 2 * 3"));

        Assert.Equal(TokenKind.Plus, addition.Operator.Kind);
        Assert.IsType<LiteralExpression>(addition.Left);
        var multiplication = Assert.IsType<BinaryExpression>(addition.Right);
        Assert.Equal(TokenKind.Star, multiplication.Operator.Kind);
    }

    [Fact]
    public void Subtraction_IsLeftAssociative()
    {
        // 1 - 2 - 3 => (1 - 2) - 3
        var outer = Assert.IsType<BinaryExpression>(ParseExpression("1 - 2 - 3"));

        Assert.IsType<BinaryExpression>(outer.Left);
        Assert.IsType<LiteralExpression>(outer.Right);
    }

    [Fact]
    public void Assignment_IsRightAssociative()
    {
        // a = b = 1 => a = (b = 1)
        var outer = Assert.IsType<AssignmentExpression>(ParseExpression("a = b = 1"));

        Assert.IsType<AssignmentExpression>(outer.Value);
    }

    [Fact]
    public void Unary_CanBeNested()
    {
        var outer = Assert.IsType<UnaryExpression>(ParseExpression("- -x"));

        Assert.IsType<DeclarationReferenceExpression>(Assert.IsType<UnaryExpression>(outer.Operand).Operand);
    }

    [Fact]
    public void Parentheses_OverridePrecedence()
    {
        // (1 + 2) * 3
        var multiplication = Assert.IsType<BinaryExpression>(ParseExpression("(1 + 2) * 3"));

        Assert.Equal(TokenKind.Star, multiplication.Operator.Kind);
        Assert.IsType<BinaryExpression>(multiplication.Left);
    }

    [Fact]
    public void Call_HasArguments()
    {
        var call = Assert.IsType<CallExpression>(ParseExpression("f(1, x)"));

        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void Declaration_HasInitializer()
    {
        var declaration = Assert.IsType<DeclarationStatement>(Assert.Single(ParseBody("int x = 1;")));

        Assert.Equal(TokenKind.IntKeyword, declaration.Type.Kind);
        Assert.NotNull(declaration.Initializer);
    }

    [Fact]
    public void ElseIf_IsNestedIfStatement()
    {
        var statement = Assert.IsType<IfStatement>(
            Assert.Single(ParseBody("if (a) { } else if (b) { } else { }")));

        var elseIf = Assert.IsType<IfStatement>(statement.Else);
        Assert.IsType<Block>(elseIf.Else);
    }

    [Fact]
    public void For_AllowsEmptyClauses()
    {
        var statement = Assert.IsType<ForStatement>(Assert.Single(ParseBody("for (;;) { }")));

        Assert.Null(statement.Initializer);
        Assert.Null(statement.Condition);
        Assert.Null(statement.Step);
    }

    [Theory]
    [InlineData("int main() { return 1 }")]
    [InlineData("int main( { }")]
    [InlineData("main() { }")]
    [InlineData("int main() { 1 + ; }")]
    public void InvalidSyntax_Throws(string source)
    {
        Assert.Throws<CompileException>(() => Parse(source));
    }
}
