using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class Parser(Lexer lexer)
{
    readonly TokenStream tokens = new(lexer);

    public TranslationUnit Parse() => TranslationUnit();

    // translationUnit ::= function*
    TranslationUnit TranslationUnit()
    {
        var functions = new List<FunctionDecl>();
        while (tokens.Peek().Kind != TokenKind.Eof)
        {
            functions.Add(Function());
        }
        return new TranslationUnit(functions);
    }

    // function ::= type identifier "(" params? ")" block
    FunctionDecl Function()
    {
        var returnType = Type();
        var name = tokens.Expect(TokenKind.Ident);
        tokens.Expect(TokenKind.LParen);
        List<ParamDecl> parameters = tokens.Peek().Kind == TokenKind.RParen ? [] : Params();
        tokens.Expect(TokenKind.RParen);
        return new FunctionDecl(returnType, name, parameters, Block());
    }

    // params ::= type identifier ("," type identifier)*
    List<ParamDecl> Params()
    {
        var parameters = new List<ParamDecl>();
        do
        {
            var type = Type();
            parameters.Add(new ParamDecl(type, tokens.Expect(TokenKind.Ident)));
        } while (TryConsume(TokenKind.Comma));
        return parameters;
    }

    // block ::= "{" statement* "}"
    Block Block()
    {
        tokens.Expect(TokenKind.LBrace);
        var stmts = new List<Stmt>();
        while (tokens.Peek().Kind != TokenKind.RBrace)
        {
            stmts.Add(Statement());
        }
        tokens.Expect(TokenKind.RBrace);
        return new Block(stmts);
    }

    // statement ::= declaration ";"
    //             | "return" expression? ";"
    //             | expression ";"
    //             | ifStmt
    //             | "while" "(" expression ")" block
    //             | "for" "(" expression? ";" expression? ";" expression? ")" block
    Stmt Statement()
    {
        var token = tokens.Peek();
        switch (token.Kind)
        {
            case TokenKind.KwIf:
                return IfStmt();

            case TokenKind.KwReturn:
            {
                tokens.Consume();
                var value = tokens.Peek().Kind == TokenKind.Semi ? null : Expression();
                tokens.Expect(TokenKind.Semi);
                return new ReturnStmt(token, value);
            }

            case TokenKind.KwWhile:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LParen);
                var cond = Expression();
                tokens.Expect(TokenKind.RParen);
                return new WhileStmt(token, cond, Block());
            }

            case TokenKind.KwFor:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LParen);
                var init = OptionalExpression(TokenKind.Semi);
                tokens.Expect(TokenKind.Semi);
                var cond = OptionalExpression(TokenKind.Semi);
                tokens.Expect(TokenKind.Semi);
                var step = OptionalExpression(TokenKind.RParen);
                tokens.Expect(TokenKind.RParen);
                return new ForStmt(token, init, cond, step, Block());
            }
        }

        if (IsTypeKeyword(token.Kind))
        {
            var decl = Declaration();
            tokens.Expect(TokenKind.Semi);
            return decl;
        }

        var expr = Expression();
        tokens.Expect(TokenKind.Semi);
        return new ExprStmt(expr);
    }

    // ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
    Stmt IfStmt()
    {
        var keyword = tokens.Expect(TokenKind.KwIf);
        tokens.Expect(TokenKind.LParen);
        var cond = Expression();
        tokens.Expect(TokenKind.RParen);
        var then = Block();

        Stmt? @else = null;
        if (TryConsume(TokenKind.KwElse))
        {
            @else = tokens.Peek().Kind == TokenKind.KwIf ? IfStmt() : Block();
        }
        return new IfStmt(keyword, cond, then, @else);
    }

    // declaration ::= type identifier ("=" expression)?
    Stmt Declaration()
    {
        var type = Type();
        var name = tokens.Expect(TokenKind.Ident);
        var init = TryConsume(TokenKind.Assign) ? Expression() : null;
        return new DeclStmt(type, name, init);
    }

    // expression ::= assign
    Expr Expression() => Assign();

    // assign ::= identifier "=" assign | equality
    Expr Assign()
    {
        // 2 つ先まで見て、代入か式かを決める
        if (tokens.Peek().Kind == TokenKind.Ident && tokens.Peek(1).Kind == TokenKind.Assign)
        {
            var target = new DeclRefExpr(tokens.Consume());
            var op = tokens.Consume();
            return new AssignExpr(target, op, Assign());
        }
        return Equality();
    }

    // equality ::= relational (("==" | "!=") relational)*
    Expr Equality() => LeftAssoc(Relational, TokenKind.Eq, TokenKind.Ne);

    // relational ::= add (("<=" | ">=" | "<" | ">") add)*
    Expr Relational() => LeftAssoc(Add, TokenKind.Le, TokenKind.Ge, TokenKind.Lt, TokenKind.Gt);

    // add ::= mul (("+" | "-") mul)*
    Expr Add() => LeftAssoc(Mul, TokenKind.Plus, TokenKind.Minus);

    // mul ::= unary (("*" | "/") unary)*
    Expr Mul() => LeftAssoc(Unary, TokenKind.Star, TokenKind.Slash);

    // unary ::= ("+" | "-") unary | primary
    Expr Unary()
    {
        var kind = tokens.Peek().Kind;
        if (kind is TokenKind.Plus or TokenKind.Minus)
        {
            var op = tokens.Consume();
            return new UnaryExpr(op, Unary());
        }
        return Primary();
    }

    // primary ::= literal
    //           | identifier ("(" args? ")")?
    //           | "(" expression ")"
    Expr Primary()
    {
        var token = tokens.Peek();
        switch (token.Kind)
        {
            case TokenKind.IntLit:
            case TokenKind.FloatLit:
            case TokenKind.CharLit:
            case TokenKind.KwTrue:
            case TokenKind.KwFalse:
                return new LiteralExpr(tokens.Consume());

            case TokenKind.Ident:
            {
                tokens.Consume();
                if (!TryConsume(TokenKind.LParen))
                {
                    return new DeclRefExpr(token);
                }
                List<Expr> args = tokens.Peek().Kind == TokenKind.RParen ? [] : Args();
                tokens.Expect(TokenKind.RParen);
                return new CallExpr(token, args);
            }

            case TokenKind.LParen:
            {
                tokens.Consume();
                var expr = Expression();
                tokens.Expect(TokenKind.RParen);
                return expr;
            }

            default:
                throw new CompileException($"expected expression, found {token.Kind}", token.Offset);
        }
    }

    // args ::= expression ("," expression)*
    List<Expr> Args()
    {
        var args = new List<Expr>();
        do
        {
            args.Add(Expression());
        } while (TryConsume(TokenKind.Comma));
        return args;
    }

    // type ::= "int" | "long" | "void" | "bool" | "char" | "float" | "double"
    Token Type()
    {
        var token = tokens.Peek();
        if (!IsTypeKeyword(token.Kind))
        {
            throw new CompileException($"expected type, found {token.Kind}", token.Offset);
        }
        return tokens.Consume();
    }

    // ---- 補助 ----

    // 左結合の二項演算: next (op next)*
    Expr LeftAssoc(Func<Expr> next, params TokenKind[] ops)
    {
        var lhs = next();
        while (ops.Contains(tokens.Peek().Kind))
        {
            var op = tokens.Consume();
            lhs = new BinaryExpr(op, lhs, next());
        }
        return lhs;
    }

    // for の各節のように、終端 Token が来たら省略とみなす式
    Expr? OptionalExpression(TokenKind terminator) =>
        tokens.Peek().Kind == terminator ? null : Expression();

    bool TryConsume(TokenKind kind)
    {
        if (tokens.Peek().Kind != kind)
        {
            return false;
        }
        tokens.Consume();
        return true;
    }

    static bool IsTypeKeyword(TokenKind kind) => kind is
        TokenKind.KwInt or TokenKind.KwLong or TokenKind.KwVoid or TokenKind.KwBool
        or TokenKind.KwChar or TokenKind.KwFloat or TokenKind.KwDouble;
}
