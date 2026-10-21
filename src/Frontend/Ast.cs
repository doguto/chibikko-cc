namespace Chibikko.Frontend;

// AST は Token をそのまま保持する。識別子名やリテラルの値はソースの綴りから後段で取り出す。
// ノード名は Clang に倣う(DeclRefExpr, CallExpr など)。

public sealed record TranslationUnit(List<FunctionDecl> Functions);

public sealed record FunctionDecl(Token ReturnType, Token Name, List<ParamDecl> Params, Block Body);

public sealed record ParamDecl(Token Type, Token Name);

// ---- 文 ----

public abstract record Stmt;

// Clang の CompoundStmt に相当する
public sealed record Block(List<Stmt> Stmts) : Stmt;

public sealed record DeclStmt(Token Type, Token Name, Expr? Init) : Stmt;

public sealed record ReturnStmt(Token Keyword, Expr? Value) : Stmt;

public sealed record ExprStmt(Expr Expr) : Stmt;

// Else は Block か、else if の場合は IfStmt
public sealed record IfStmt(Token Keyword, Expr Cond, Block Then, Stmt? Else) : Stmt;

public sealed record WhileStmt(Token Keyword, Expr Cond, Block Body) : Stmt;

public sealed record ForStmt(Token Keyword, Expr? Init, Expr? Cond, Expr? Step, Block Body) : Stmt;

// ---- 式 ----

public abstract record Expr;

// 整数・浮動小数点・文字・真偽値。種別は Token.Kind で区別する
public sealed record LiteralExpr(Token Token) : Expr;

// 変数の参照
public sealed record DeclRefExpr(Token Name) : Expr;

public sealed record CallExpr(Token Callee, List<Expr> Args) : Expr;

public sealed record UnaryExpr(Token Op, Expr Operand) : Expr;

public sealed record BinaryExpr(Token Op, Expr Lhs, Expr Rhs) : Expr;

public sealed record AssignExpr(DeclRefExpr Target, Token Op, Expr Value) : Expr;
