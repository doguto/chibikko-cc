using Chibikko.Common;

namespace Chibikko.Frontend;

public sealed class Parser(Lexer lexer)
{
    readonly TokenStream tokens = new(lexer);

    public TranslationUnit Parse() => ParseTranslationUnit();

    // translationUnit ::= function*
    TranslationUnit ParseTranslationUnit()
    {
        var functions = new List<FunctionDeclaration>();
        while (tokens.Peek().Kind != TokenKind.EndOfFile)
        {
            functions.Add(ParseFunction());
        }
        return new TranslationUnit(functions);
    }

    // function ::= type identifier "(" params? ")" block
    FunctionDeclaration ParseFunction()
    {
        var returnType = ParseType();
        var name = tokens.Expect(TokenKind.Identifier);
        tokens.Expect(TokenKind.LeftParen);
        List<ParameterDeclaration> parameters = tokens.Peek().Kind == TokenKind.RightParen ? [] : ParseParameters();
        tokens.Expect(TokenKind.RightParen);
        return new FunctionDeclaration(returnType, name, parameters, ParseBlock());
    }

    // params ::= type identifier ("," type identifier)*
    List<ParameterDeclaration> ParseParameters()
    {
        var parameters = new List<ParameterDeclaration>();
        do
        {
            var type = ParseType();
            parameters.Add(new ParameterDeclaration(type, tokens.Expect(TokenKind.Identifier)));
        } while (TryConsume(TokenKind.Comma));
        return parameters;
    }

    // block ::= "{" statement* "}"
    Block ParseBlock()
    {
        tokens.Expect(TokenKind.LeftBrace);
        var statements = new List<Statement>();
        while (tokens.Peek().Kind != TokenKind.RightBrace)
        {
            statements.Add(ParseStatement());
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
    Statement ParseStatement()
    {
        var token = tokens.Peek();
        switch (token.Kind)
        {
            case TokenKind.IfKeyword:
                return ParseIfStatement();

            case TokenKind.ReturnKeyword:
            {
                tokens.Consume();
                var value = tokens.Peek().Kind == TokenKind.Semicolon ? null : ParseExpression();
                tokens.Expect(TokenKind.Semicolon);
                return new ReturnStatement(token, value);
            }

            case TokenKind.WhileKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var condition = ParseExpression();
                tokens.Expect(TokenKind.RightParen);
                return new WhileStatement(token, condition, ParseBlock());
            }

            case TokenKind.ForKeyword:
            {
                tokens.Consume();
                tokens.Expect(TokenKind.LeftParen);
                var initializer = ParseOptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var condition = ParseOptionalExpression(TokenKind.Semicolon);
                tokens.Expect(TokenKind.Semicolon);
                var step = ParseOptionalExpression(TokenKind.RightParen);
                tokens.Expect(TokenKind.RightParen);
                return new ForStatement(token, initializer, condition, step, ParseBlock());
            }
        }

        if (IsTypeKeyword(token.Kind))
        {
            var declaration = ParseDeclaration();
            tokens.Expect(TokenKind.Semicolon);
            return declaration;
        }

        var expression = ParseExpression();
        tokens.Expect(TokenKind.Semicolon);
        return new ExpressionStatement(expression);
    }

    // ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
    Statement ParseIfStatement()
    {
        var keyword = tokens.Expect(TokenKind.IfKeyword);
        tokens.Expect(TokenKind.LeftParen);
        var condition = ParseExpression();
        tokens.Expect(TokenKind.RightParen);
        var then = ParseBlock();

        Statement? @else = null;
        if (TryConsume(TokenKind.ElseKeyword))
        {
            @else = tokens.Peek().Kind == TokenKind.IfKeyword ? ParseIfStatement() : ParseBlock();
        }
        return new IfStatement(keyword, condition, then, @else);
    }

    // declaration ::= type identifier ("=" expression)?
    Statement ParseDeclaration()
    {
        var type = ParseType();
        var name = tokens.Expect(TokenKind.Identifier);
        var initializer = TryConsume(TokenKind.Assign) ? ParseExpression() : null;
        return new DeclarationStatement(type, name, initializer);
    }

    // expression ::= assign
    Expression ParseExpression() => ParseAssignment();

    // assign ::= identifier "=" assign | equality
    Expression ParseAssignment()
    {
        // 2 つ先まで見て、代入か式かを決める
        if (tokens.Peek().Kind == TokenKind.Identifier && tokens.Peek(1).Kind == TokenKind.Assign)
        {
            var target = new DeclarationReferenceExpression(tokens.Consume());
            var @operator = tokens.Consume();
            return new AssignmentExpression(target, @operator, ParseAssignment());
        }
        return ParseEquality();
    }

    // equality ::= relational (("==" | "!=") relational)*
    Expression ParseEquality() => ParseLeftAssociative(ParseRelational, TokenKind.EqualEqual, TokenKind.NotEqual);

    // relational ::= add (("<=" | ">=" | "<" | ">") add)*
    Expression ParseRelational() => ParseLeftAssociative(ParseAdditive, TokenKind.LessEqual, TokenKind.GreaterEqual, TokenKind.Less, TokenKind.Greater);

    // add ::= mul (("+" | "-") mul)*
    Expression ParseAdditive() => ParseLeftAssociative(ParseMultiplicative, TokenKind.Plus, TokenKind.Minus);

    // mul ::= unary (("*" | "/") unary)*
    Expression ParseMultiplicative() => ParseLeftAssociative(ParseUnary, TokenKind.Star, TokenKind.Slash);

    // unary ::= ("+" | "-") unary | primary
    Expression ParseUnary()
    {
        var kind = tokens.Peek().Kind;
        if (kind is TokenKind.Plus or TokenKind.Minus)
        {
            var @operator = tokens.Consume();
            return new UnaryExpression(@operator, ParseUnary());
        }
        return ParsePrimary();
    }

    // primary ::= literal
    //           | identifier ("(" arguments? ")")?
    //           | "(" expression ")"
    Expression ParsePrimary()
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
                List<Expression> arguments = tokens.Peek().Kind == TokenKind.RightParen ? [] : ParseArguments();
                tokens.Expect(TokenKind.RightParen);
                return new CallExpression(token, arguments);
            }

            case TokenKind.LeftParen:
            {
                tokens.Consume();
                var expression = ParseExpression();
                tokens.Expect(TokenKind.RightParen);
                return expression;
            }

            default:
                throw new CompileException($"expected expression, found {token.Kind}", token.Offset);
        }
    }

    // arguments ::= expression ("," expression)*
    List<Expression> ParseArguments()
    {
        var arguments = new List<Expression>();
        do
        {
            arguments.Add(ParseExpression());
        } while (TryConsume(TokenKind.Comma));
        return arguments;
    }

    // type ::= "int" | "long" | "void" | "bool" | "char" | "float" | "double"
    Token ParseType()
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
    Expression ParseLeftAssociative(Func<Expression> parseOperand, params TokenKind[] operators)
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
    Expression? ParseOptionalExpression(TokenKind terminator) =>
        tokens.Peek().Kind == terminator ? null : ParseExpression();

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
