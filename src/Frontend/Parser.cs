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
        while (tokens.Peek().Kind != TokenKind.EndOfFile)
        {
            functions.Add(Function());
        }
        return new TranslationUnit(functions);
    }

    // function ::= type identifier "(" params? ")" block
    FunctionDecl Function()
    {
        var returnType = Type();
        var name = tokens.Expect(TokenKind.Identifier);
        tokens.Expect(TokenKind.LeftParen);
        List<ParamDecl> parameters = tokens.Peek().Kind == TokenKind.RightParen ? [] : Params();
        tokens.Expect(TokenKind.RightParen);
        return new FunctionDecl(returnType, name, parameters, Block());
    }

    // params ::= type identifier ("," type identifier)*
    List<ParamDecl> Params()
    {
        var parameters = new List<ParamDecl>();
        do
        {
            var type = Type();
            parameters.Add(new ParamDecl(type, tokens.Expect(TokenKind.Identifier)));
        } while (TryConsume(TokenKind.Comma));
        return parameters;
    }

    // block ::= "{" statement* "}"
    Block Block()
    {
        tokens.Expect(TokenKind.LeftBrace);
        var stmts = new List<Stmt>();
        while (tokens.Peek().Kind != TokenKind.RightBrace)
        {
            stmts.Add(Statement());
        }
        tokens.Expect(TokenKind.RightBrace);
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
            case TokenKind.IfKeyword:
                return IfStmt();

            case TokenKind.ReturnKeyword:
            {
                tokens.Consume();
                var value = tokens.Peek().Kind == TokenKind.Semicolon ? null : Expression();
                tokens.Expect(TokenKind.Semicolon);
                return new ReturnStmt(token, value);
            }

            case TokenKind.WhileKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var cond = Expression();
                tokens.Expect(TokenKind.RightParen);
                return new WhileStmt(token, cond, Block());
            }

            case TokenKind.ForKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var init = OptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var cond = OptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var step = OptionalExpression(TokenKind.RightParen);
                tokens.Expect(TokenKind.RightParen);
                return new ForStmt(token, init, cond, step, Block());
            }
        }

        if (IsTypeKeyword(token.Kind))
        {
            var decl = Declaration();
            tokens.Expect(TokenKind.Semicolon);
            return decl;
        }

        var expr = Expression();
        tokens.Expect(TokenKind.Semicolon);
        return new ExprStmt(expr);
    }

    // ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
    Stmt IfStmt()
    {
        var keyword = tokens.Expect(TokenKind.IfKeyword);
        tokens.Expect(TokenKind.LeftParen);
        var cond = Expression();
        tokens.Expect(TokenKind.RightParen);
        var then = Block();

        Stmt? @else = null;
        if (TryConsume(TokenKind.ElseKeyword))
        {
            @else = tokens.Peek().Kind == TokenKind.IfKeyword ? IfStmt() : Block();
        }
        return new IfStmt(keyword, cond, then, @else);
    }

    // declaration ::= type identifier ("=" expression)?
    Stmt Declaration()
    {
        var type = Type();
        var name = tokens.Expect(TokenKind.Identifier);
        var init = TryConsume(TokenKind.Assign) ? Expression() : null;
        return new DeclStmt(type, name, init);
    }

    // expression ::= assign
    Expr Expression() => Assign();

    // assign ::= identifier "=" assign | equality
    Expr Assign()
    {
        // 2 つ先まで見て、代入か式かを決める
        if (tokens.Peek().Kind == TokenKind.Identifier && tokens.Peek(1).Kind == TokenKind.Assign)
        {
            var target = new DeclRefExpr(tokens.Consume());
            var op = tokens.Consume();
            return new AssignExpr(target, op, Assign());
        }
        return Equality();
    }

    // equality ::= relational (("==" | "!=") relational)*
    Expr Equality() => LeftAssoc(Relational, TokenKind.EqualEqual, TokenKind.NotEqual);

    // relational ::= add (("<=" | ">=" | "<" | ">") add)*
    Expr Relational() => LeftAssoc(Add, TokenKind.LessEqual, TokenKind.GreaterEqual, TokenKind.Less, TokenKind.Greater);

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
            case TokenKind.IntegerLiteral:
            case TokenKind.FloatLiteral:
            case TokenKind.CharLiteral:
            case TokenKind.TrueKeyword:
            case TokenKind.FalseKeyword:
                return new LiteralExpr(tokens.Consume());

            case TokenKind.Identifier:
            {
                tokens.Consume();
                if (!TryConsume(TokenKind.LeftParen))
                {
                    return new DeclRefExpr(token);
                }
                List<Expr> args = tokens.Peek().Kind == TokenKind.RightParen ? [] : Args();
                tokens.Expect(TokenKind.RightParen);
                return new CallExpr(token, args);
            }

            case TokenKind.LeftParen:
            {
                tokens.Consume();
                var expr = Expression();
                tokens.Expect(TokenKind.RightParen);
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
        TokenKind.IntKeyword or TokenKind.LongKeyword or TokenKind.VoidKeyword or TokenKind.BoolKeyword
        or TokenKind.CharKeyword or TokenKind.FloatKeyword or TokenKind.DoubleKeyword;
}
