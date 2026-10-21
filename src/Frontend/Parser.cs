using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class Parser(Lexer lexer)
{
    readonly TokenStream tokens = new(lexer);

    public TranslationUnit Parse() => TranslationUnit();

    // translationUnit ::= function*
    TranslationUnit TranslationUnit()
    {
        var functions = new List<FunctionDeclaration>();
        while (tokens.Peek().Kind != TokenKind.EndOfFile)
        {
            functions.Add(Function());
        }
        return new TranslationUnit(functions);
    }

    // function ::= type identifier "(" params? ")" block
    FunctionDeclaration Function()
    {
        var returnType = Type();
        var name = tokens.Expect(TokenKind.Identifier);
        tokens.Expect(TokenKind.LeftParen);
        List<ParameterDeclaration> parameters = tokens.Peek().Kind == TokenKind.RightParen ? [] : Parameters();
        tokens.Expect(TokenKind.RightParen);
        return new FunctionDeclaration(returnType, name, parameters, Block());
    }

    // params ::= type identifier ("," type identifier)*
    List<ParameterDeclaration> Parameters()
    {
        var parameters = new List<ParameterDeclaration>();
        do
        {
            var type = Type();
            parameters.Add(new ParameterDeclaration(type, tokens.Expect(TokenKind.Identifier)));
        } while (TryConsume(TokenKind.Comma));
        return parameters;
    }

    // block ::= "{" statement* "}"
    Block Block()
    {
        tokens.Expect(TokenKind.LeftBrace);
        var statements = new List<Statement>();
        while (tokens.Peek().Kind != TokenKind.RightBrace)
        {
            statements.Add(Statement());
        }
        tokens.Expect(TokenKind.RightBrace);
        return new Block(statements);
    }

    // statement ::= declaration ";"
    //             | "return" expression? ";"
    //             | expression ";"
    //             | ifStmt
    //             | "while" "(" expression ")" block
    //             | "for" "(" expression? ";" expression? ";" expression? ")" block
    Statement Statement()
    {
        var token = tokens.Peek();
        switch (token.Kind)
        {
            case TokenKind.IfKeyword:
                return IfStatement();

            case TokenKind.ReturnKeyword:
            {
                tokens.Consume();
                var value = tokens.Peek().Kind == TokenKind.Semicolon ? null : Expression();
                tokens.Expect(TokenKind.Semicolon);
                return new ReturnStatement(token, value);
            }

            case TokenKind.WhileKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var condition = Expression();
                tokens.Expect(TokenKind.RightParen);
                return new WhileStatement(token, condition, Block());
            }

            case TokenKind.ForKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var initializer = OptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var condition = OptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var step = OptionalExpression(TokenKind.RightParen);
                tokens.Expect(TokenKind.RightParen);
                return new ForStatement(token, initializer, condition, step, Block());
            }
        }

        if (IsTypeKeyword(token.Kind))
        {
            var declaration = Declaration();
            tokens.Expect(TokenKind.Semicolon);
            return declaration;
        }

        var expression = Expression();
        tokens.Expect(TokenKind.Semicolon);
        return new ExpressionStatement(expression);
    }

    // ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
    Statement IfStatement()
    {
        var keyword = tokens.Expect(TokenKind.IfKeyword);
        tokens.Expect(TokenKind.LeftParen);
        var condition = Expression();
        tokens.Expect(TokenKind.RightParen);
        var then = Block();

        Statement? @else = null;
        if (TryConsume(TokenKind.ElseKeyword))
        {
            @else = tokens.Peek().Kind == TokenKind.IfKeyword ? IfStatement() : Block();
        }
        return new IfStatement(keyword, condition, then, @else);
    }

    // declaration ::= type identifier ("=" expression)?
    Statement Declaration()
    {
        var type = Type();
        var name = tokens.Expect(TokenKind.Identifier);
        var initializer = TryConsume(TokenKind.Assign) ? Expression() : null;
        return new DeclarationStatement(type, name, initializer);
    }

    // expression ::= assign
    Expression Expression() => Assignment();

    // assign ::= identifier "=" assign | equality
    Expression Assignment()
    {
        // 2 つ先まで見て、代入か式かを決める
        if (tokens.Peek().Kind == TokenKind.Identifier && tokens.Peek(1).Kind == TokenKind.Assign)
        {
            var target = new DeclarationReferenceExpression(tokens.Consume());
            var @operator = tokens.Consume();
            return new AssignmentExpression(target, @operator, Assignment());
        }
        return Equality();
    }

    // equality ::= relational (("==" | "!=") relational)*
    Expression Equality() => LeftAssociative(Relational, TokenKind.EqualEqual, TokenKind.NotEqual);

    // relational ::= add (("<=" | ">=" | "<" | ">") add)*
    Expression Relational() => LeftAssociative(Additive, TokenKind.LessEqual, TokenKind.GreaterEqual, TokenKind.Less, TokenKind.Greater);

    // add ::= mul (("+" | "-") mul)*
    Expression Additive() => LeftAssociative(Multiplicative, TokenKind.Plus, TokenKind.Minus);

    // mul ::= unary (("*" | "/") unary)*
    Expression Multiplicative() => LeftAssociative(Unary, TokenKind.Star, TokenKind.Slash);

    // unary ::= ("+" | "-") unary | primary
    Expression Unary()
    {
        var kind = tokens.Peek().Kind;
        if (kind is TokenKind.Plus or TokenKind.Minus)
        {
            var @operator = tokens.Consume();
            return new UnaryExpression(@operator, Unary());
        }
        return Primary();
    }

    // primary ::= literal
    //           | identifier ("(" arguments? ")")?
    //           | "(" expression ")"
    Expression Primary()
    {
        var token = tokens.Peek();
        switch (token.Kind)
        {
            case TokenKind.IntegerLiteral:
            case TokenKind.FloatLiteral:
            case TokenKind.CharLiteral:
            case TokenKind.TrueKeyword:
            case TokenKind.FalseKeyword:
                return new LiteralExpression(tokens.Consume());

            case TokenKind.Identifier:
            {
                tokens.Consume();
                if (!TryConsume(TokenKind.LeftParen))
                {
                    return new DeclarationReferenceExpression(token);
                }
                List<Expression> arguments = tokens.Peek().Kind == TokenKind.RightParen ? [] : Arguments();
                tokens.Expect(TokenKind.RightParen);
                return new CallExpression(token, arguments);
            }

            case TokenKind.LeftParen:
            {
                tokens.Consume();
                var expression = Expression();
                tokens.Expect(TokenKind.RightParen);
                return expression;
            }

            default:
                throw new CompileException($"expected expression, found {token.Kind}", token.Offset);
        }
    }

    // arguments ::= expression ("," expression)*
    List<Expression> Arguments()
    {
        var arguments = new List<Expression>();
        do
        {
            arguments.Add(Expression());
        } while (TryConsume(TokenKind.Comma));
        return arguments;
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

    // 左結合の二項演算: parseOperand (@operator parseOperand)*
    Expression LeftAssociative(Func<Expression> parseOperand, params TokenKind[] operators)
    {
        var left = parseOperand();
        while (operators.Contains(tokens.Peek().Kind))
        {
            var @operator = tokens.Consume();
            left = new BinaryExpression(@operator, left, parseOperand());
        }
        return left;
    }

    // for の各節のように、終端 Token が来たら省略とみなす式
    Expression? OptionalExpression(TokenKind terminator) =>
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
