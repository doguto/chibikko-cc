using Chibikko.Common;

namespace Chibikko.Frontend.Type;

// AST は Token をそのまま保持する。識別子名やリテラルの値はソースの綴りから後段で取り出す。
// ノード名は Clang に倣う(DeclarationReferenceExpression, CallExpression など)。
// ノードは Parser から呼ばれた SemanticAnalyzer が作り、型や参照先(init プロパティ)もその場で埋める。

// translationUnit ::= function*
public sealed record TranslationUnit(List<FunctionDeclaration> Functions);

// function ::= type identifier "(" params? ")" block
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

// params ::= type identifier ("," type identifier)*
public sealed record ParameterDeclaration(Token Type, Token Name) : IVariableDeclaration
{
    public BuiltinType? ResolvedType { get; init; }
}

// ---- 文 ----

public abstract record Statement;

// block ::= "{" statement* "}"
// Clang の CompoundStmt に相当する
public sealed record Block(List<Statement> Statements) : Statement;

// declaration ::= type identifier ("=" expression)? ";"
public sealed record DeclarationStatement(Token Type, Token Name, Expression? Initializer) : Statement, IVariableDeclaration
{
    public BuiltinType? ResolvedType { get; init; }
}

// "return" expression? ";"
public sealed record ReturnStatement(Token Keyword, Expression? Value) : Statement;

// expression ";"
public sealed record ExpressionStatement(Expression Expression) : Statement;

// ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
// Else は Block か、else if の場合は IfStatement
public sealed record IfStatement(Token Keyword, Expression Condition, Block Then, Statement? Else) : Statement;

// "while" "(" expression ")" block
public sealed record WhileStatement(Token Keyword, Expression Condition, Block Body) : Statement;

// "for" "(" expression? ";" expression? ";" expression? ")" block
public sealed record ForStatement(Token Keyword, Expression? Initializer, Expression? Condition, Expression? Step, Block Body) : Statement;

// ---- 式 ----

public abstract record Expression
{
    public BuiltinType? Type { get; init; }
}

// primary ::= literal
// 整数・浮動小数点・文字・真偽値。種別は Token.Kind で区別する
public sealed record LiteralExpression(Token Token) : Expression;

// primary ::= identifier
// 変数の参照
public sealed record DeclarationReferenceExpression(Token Name) : Expression
{
    public IVariableDeclaration? Declaration { get; init; }
}

// primary ::= identifier "(" arguments? ")"
// arguments ::= expression ("," expression)*
public sealed record CallExpression(Token Callee, List<Expression> Arguments) : Expression
{
    public FunctionDeclaration? Function { get; init; }
}

// unary ::= ("+" | "-") unary
public sealed record UnaryExpression(Token Operator, Expression Operand) : Expression;

// equality ::= relational (("==" | "!=") relational)*
// relational ::= add (("<=" | ">=" | "<" | ">") add)*
// add ::= mul (("+" | "-") mul)*
// mul ::= unary (("*" | "/") unary)*
public sealed record BinaryExpression(Token Operator, Expression Left, Expression Right) : Expression;

// assign ::= identifier "=" assign
public sealed record AssignmentExpression(DeclarationReferenceExpression Target, Token Operator, Expression Value) : Expression;

// Sema が挿入する暗黙の型変換。変換先は Type
public sealed record ImplicitCastExpression(Expression Operand) : Expression;
