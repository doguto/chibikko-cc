using System.Text;
using Chibikko.Frontend.Type;

namespace Chibikko.Frontend;

public static class AstDumper
{
    public static string Dump(TranslationUnit unit)
    {
        var builder = new StringBuilder();
        builder.AppendLine("TranslationUnit");
        DumpChildren(builder, "", [.. unit.Functions.Select(f => (Node)new Ast(f))]);
        return builder.ToString();
    }

    abstract record Node;
    sealed record Ast(object Value) : Node;
    sealed record Group(string Header, List<Node> Children) : Node;

    static void DumpChildren(StringBuilder builder, string indent, List<Node> children)
    {
        for (var i = 0; i < children.Count; i++)
        {
            var last = i == children.Count - 1;
            builder.Append(indent).Append(last ? "`-" : "|-");
            DumpNode(builder, indent + (last ? "  " : "| "), children[i]);
        }
    }

    static void DumpNode(StringBuilder builder, string indent, Node node)
    {
        var (header, children) = node switch
        {
            Group group => (group.Header, group.Children),
            Ast ast => Describe(ast.Value),
            _ => throw new NotSupportedException($"Unsupported node: {node.GetType().Name}"),
        };
        builder.AppendLine(header);
        DumpChildren(builder, indent, children);
    }

    static (string Header, List<Node> Children) Describe(object value) => value switch
    {
        FunctionDeclaration f => (
            $"FunctionDecl {f.Name.Text} '{f.ResolvedReturnType} ({string.Join(", ", f.Parameters.Select(p => p.ResolvedType))})'",
            [.. f.Parameters.Select(p => new Ast(p)), new Ast(f.Body)]),
        ParameterDeclaration p => ($"ParmVarDecl {p.Name.Text} '{p.ResolvedType}'", []),

        Block b => ("CompoundStmt", [.. b.Statements.Select(s => new Ast(s))]),
        DeclarationStatement d => ($"VarDecl {d.Name.Text} '{d.ResolvedType}'", Children(d.Initializer)),
        ReturnStatement r => ("ReturnStmt", Children(r.Value)),
        ExpressionStatement e => ("ExprStmt", Children(e.Expression)),
        IfStatement i => ("IfStmt", [new Ast(i.Condition), new Ast(i.Then), .. Children(i.Else)]),
        WhileStatement w => ("WhileStmt", [new Ast(w.Condition), new Ast(w.Body)]),
        ForStatement f => ("ForStmt", [
            Optional("Init", f.Initializer),
            Optional("Cond", f.Condition),
            Optional("Step", f.Step),
            new Ast(f.Body)]),

        LiteralExpression l => ($"{LiteralName(l.Token.Kind)} '{l.Type}' {l.Token.Text}", []),
        DeclarationReferenceExpression r => ($"DeclRefExpr '{r.Type}' {r.Name.Text}", []),
        CallExpression c => ($"CallExpr '{c.Type}' {c.Callee.Text}", [.. c.Arguments.Select(a => new Ast(a))]),
        UnaryExpression u => ($"UnaryOperator '{u.Type}' '{u.Operator.Text}'", [new Ast(u.Operand)]),
        BinaryExpression b => ($"BinaryOperator '{b.Type}' '{b.Operator.Text}'", [new Ast(b.Left), new Ast(b.Right)]),
        AssignmentExpression a => ($"AssignOperator '{a.Type}' '{a.Operator.Text}'", [new Ast(a.Target), new Ast(a.Value)]),
        ImplicitCastExpression c => ($"ImplicitCastExpr '{c.Type}'", [new Ast(c.Operand)]),

        _ => throw new NotSupportedException($"Unsupported AST node: {value.GetType().Name}"),
    };

    static List<Node> Children(object? value) => value is null ? [] : [new Ast(value)];

    static Node Optional(string label, Expression? expression) =>
        new Group(expression is null ? $"{label}: <null>" : $"{label}:", Children(expression));

    static string LiteralName(TokenKind kind) => kind switch
    {
        TokenKind.IntegerLiteral => "IntegerLiteral",
        TokenKind.FloatLiteral => "FloatingLiteral",
        TokenKind.CharLiteral => "CharacterLiteral",
        TokenKind.TrueKeyword or TokenKind.FalseKeyword => "BoolLiteral",
        _ => $"Literal({kind})",
    };
}
