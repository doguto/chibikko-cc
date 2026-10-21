using Chibikko.Common;

namespace Chibikko.Frontend;

// AST は Token をそのまま保持する。識別子名やリテラルの値はソースの綴りから後段で取り出す。
// ノード名は Clang に倣う(DeclarationReferenceExpression, CallExpression など)。
// Parser は構文だけを埋め、型や参照先(init プロパティ)は SemanticAnalyzer が埋めた新しいノードを作る。

public sealed record TranslationUnit(List<FunctionDeclaration> Functions);

public sealed record FunctionDeclaration(Token ReturnType, Token Name, List<ParameterDeclaration> Parameters, Block Body)
{
    public BuiltinType? ResolvedReturnType { get; init; }
}

// 引数とローカル変数。DeclarationReferenceExpression の参照先になる
public interface IVariableDeclaration
{
    Token Type { get; }
    Token Name { get; }
    BuiltinType? ResolvedType { get; }
}

public sealed record ParameterDeclaration(Token Type, Token Name) : IVariableDeclaration
{
    public BuiltinType? ResolvedType { get; init; }
}

// ---- 文 ----

public abstract record Statement;

// Clang の CompoundStmt に相当する
public sealed record Block(List<Statement> Statements) : Statement;

public sealed record DeclarationStatement(Token Type, Token Name, Expression? Initializer) : Statement, IVariableDeclaration
{
    public BuiltinType? ResolvedType { get; init; }
}

public sealed record ReturnStatement(Token Keyword, Expression? Value) : Statement;

public sealed record ExpressionStatement(Expression Expression) : Statement;

// Else は Block か、else if の場合は IfStatement
public sealed record IfStatement(Token Keyword, Expression Condition, Block Then, Statement? Else) : Statement;

public sealed record WhileStatement(Token Keyword, Expression Condition, Block Body) : Statement;

public sealed record ForStatement(Token Keyword, Expression? Initializer, Expression? Condition, Expression? Step, Block Body) : Statement;

// ---- 式 ----

public abstract record Expression
{
    public BuiltinType? Type { get; init; }
}

// 整数・浮動小数点・文字・真偽値。種別は Token.Kind で区別する
public sealed record LiteralExpression(Token Token) : Expression;

// 変数の参照
public sealed record DeclarationReferenceExpression(Token Name) : Expression
{
    public IVariableDeclaration? Declaration { get; init; }
}

public sealed record CallExpression(Token Callee, List<Expression> Arguments) : Expression
{
    public FunctionDeclaration? Function { get; init; }
}

public sealed record UnaryExpression(Token Operator, Expression Operand) : Expression;

public sealed record BinaryExpression(Token Operator, Expression Left, Expression Right) : Expression;

public sealed record AssignmentExpression(DeclarationReferenceExpression Target, Token Operator, Expression Value) : Expression;

// Sema が挿入する暗黙の型変換。変換先は Type
public sealed record ImplicitCastExpression(Expression Operand) : Expression;
