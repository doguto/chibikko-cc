using Chibikko.Common;
using Chibikko.Frontend;

namespace Chibikko.Tests.Frontend;

public class SemanticAnalyzerTests
{
    static TranslationUnit Analyze(string source) =>
        new SemanticAnalyzer(source).Analyze(new Parser(new Lexer(source)).Parse());

    // main の本体を解析し、最後の文の式を取り出す
    static Expression AnalyzeExpression(string body)
    {
        var statements = Analyze($"int main() {{ {body} }}").Functions[0].Body.Statements;
        return Assert.IsType<ExpressionStatement>(statements[^1]).Expression;
    }

    [Theory]
    [InlineData("1;", BuiltinKind.Int)]
    [InlineData("1L;", BuiltinKind.Long)]
    [InlineData("1.0;", BuiltinKind.Double)]
    [InlineData("1.0f;", BuiltinKind.Float)]
    [InlineData("'a';", BuiltinKind.Char)]
    [InlineData("true;", BuiltinKind.Bool)]
    public void Literal_HasTypeFromSuffix(string body, BuiltinKind expected)
    {
        Assert.Equal(expected, AnalyzeExpression(body).Type!.Kind);
    }

    [Fact]
    public void IntPlusDouble_ConvertsLeftToDouble()
    {
        var addition = Assert.IsType<BinaryExpression>(AnalyzeExpression("1 + 2.0;"));

        Assert.Equal(BuiltinType.Double, addition.Type);
        var cast = Assert.IsType<ImplicitCastExpression>(addition.Left);
        Assert.Equal(BuiltinType.Double, cast.Type);
        Assert.Equal(BuiltinType.Int, cast.Operand.Type);
        Assert.IsType<LiteralExpression>(addition.Right);
    }

    [Fact]
    public void CharPlusChar_IsPromotedToInt()
    {
        var addition = Assert.IsType<BinaryExpression>(AnalyzeExpression("'a' + 'b';"));

        Assert.Equal(BuiltinType.Int, addition.Type);
        Assert.IsType<ImplicitCastExpression>(addition.Left);
        Assert.IsType<ImplicitCastExpression>(addition.Right);
    }

    [Fact]
    public void Comparison_HasBoolType()
    {
        Assert.Equal(BuiltinType.Bool, AnalyzeExpression("1 < 2;").Type);
    }

    [Fact]
    public void Reference_PointsToDeclaration()
    {
        var unit = Analyze("int main() { long x = 1; x; }");
        var statements = unit.Functions[0].Body.Statements;

        var declaration = Assert.IsType<DeclarationStatement>(statements[0]);
        var reference = Assert.IsType<DeclarationReferenceExpression>(
            Assert.IsType<ExpressionStatement>(statements[1]).Expression);
        Assert.Same(declaration, reference.Declaration);
        Assert.Equal(BuiltinType.Long, reference.Type);

        // int の 1 は long に変換されて初期化される
        Assert.IsType<ImplicitCastExpression>(declaration.Initializer);
    }

    [Fact]
    public void InnerScope_ShadowsOuterVariable()
    {
        var unit = Analyze("int main() { int x = 1; if (true) { double x = 2.0; x; } }");
        var ifStatement = Assert.IsType<IfStatement>(unit.Functions[0].Body.Statements[1]);

        var reference = Assert.IsType<DeclarationReferenceExpression>(
            Assert.IsType<ExpressionStatement>(ifStatement.Then.Statements[1]).Expression);
        Assert.Equal(BuiltinType.Double, reference.Type);
    }

    [Fact]
    public void Call_ConvertsArgumentsToParameterTypes()
    {
        var unit = Analyze("double half(double x) { return x / 2.0; } int main() { half(1); }");
        var call = Assert.IsType<CallExpression>(
            Assert.IsType<ExpressionStatement>(unit.Functions[1].Body.Statements[0]).Expression);

        Assert.Equal(BuiltinType.Double, call.Type);
        Assert.IsType<ImplicitCastExpression>(Assert.Single(call.Arguments));
    }

    [Fact]
    public void RecursiveCall_IsAllowed()
    {
        Analyze("int fib(int n) { if (n < 2) { return n; } return fib(n - 1) + fib(n - 2); }");
    }

    [Fact]
    public void Condition_IsConvertedToBool()
    {
        var unit = Analyze("int main() { while (1) { } }");
        var whileStatement = Assert.IsType<WhileStatement>(unit.Functions[0].Body.Statements[0]);

        Assert.Equal(BuiltinType.Bool, whileStatement.Condition.Type);
    }

    [Theory]
    [InlineData("int main() { x; }")]                          // 未宣言の変数
    [InlineData("int main() { int x; int x; }")]               // 同じスコープでの再宣言
    [InlineData("int main() { f(); }")]                        // 未宣言の関数
    [InlineData("int f(int a) { return a; } int main() { f(); }")] // 引数の数
    [InlineData("int main() { void x; }")]                     // void の変数
    [InlineData("void f() { return 1; }")]                     // void 関数が値を返す
    [InlineData("int main() { return; }")]                     // 値を返さない
    [InlineData("void f() { } int main() { return f(); }")]    // void を int に変換
    [InlineData("int f() { } int f() { }")]                    // 関数の再定義
    [InlineData("int main() { if (true) { int y; } y; }")]     // スコープ外
    public void InvalidProgram_Throws(string source)
    {
        Assert.Throws<CompileException>(() => Analyze(source));
    }
}
