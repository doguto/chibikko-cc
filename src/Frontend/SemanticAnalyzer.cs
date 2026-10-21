using Chibikko.Common;

namespace Chibikko.Frontend;

/// <summary>
/// Parser から構文要素ごとに ActOn* で呼ばれ、型と参照先を埋めたノードを返す(Clang の Sema と同じ)。
/// 暗黙の型変換は ImplicitCastExpression として挿入する。
/// </summary>
public sealed class SemanticAnalyzer(string source)
{
    readonly string source = source;
    readonly Dictionary<string, FunctionDeclaration> functions = new();

    // 内側のブロックほど後ろ。名前は内側から探す
    readonly List<Dictionary<string, IVariableDeclaration>> scopes = [];

    FunctionDeclaration? currentFunction;

    public TranslationUnit ActOnTranslationUnit(List<FunctionDeclaration> functions) => new(functions);

    // ---- 関数 ----

    public ParameterDeclaration ActOnParameterDeclaration(Token type, Token name) =>
        new(type, name) { ResolvedType = VariableType(type) };

    public FunctionDeclaration ActOnStartOfFunctionDefinition(Token returnType, Token name, List<ParameterDeclaration> parameters)
    {
        var spelling = Spelling(name);
        if (functions.ContainsKey(spelling))
        {
            throw new CompileException($"redefinition of function '{spelling}'", name.Offset);
        }

        var signature = new FunctionDeclaration(returnType, name, parameters, new Block([]))
        {
            ResolvedReturnType = ResolveType(returnType),
        };
        functions[spelling] = signature;
        currentFunction = signature;

        PushScope();
        foreach (var parameter in parameters)
        {
            Declare(parameter);
        }
        return signature;
    }

    public FunctionDeclaration ActOnFinishFunctionBody(FunctionDeclaration signature, Block body)
    {
        PopScope();
        currentFunction = null;
        var function = signature with { Body = body };
        functions[Spelling(function.Name)] = function;
        return function;
    }

    // ---- 文 ----

    public void ActOnStartOfBlock() => PushScope();

    public Block ActOnBlock(List<Statement> statements)
    {
        PopScope();
        return new Block(statements);
    }

    public DeclarationStatement ActOnDeclarationStatement(Token type, Token name, Expression? initializer)
    {
        var resolved = VariableType(type);
        var declaration = new DeclarationStatement(type, name, initializer is null ? null : ConvertTo(initializer, resolved))
        {
            ResolvedType = resolved,
        };
        Declare(declaration);
        return declaration;
    }

    public ReturnStatement ActOnReturnStatement(Token keyword, Expression? value)
    {
        var returnType = currentFunction!.ResolvedReturnType!;
        if (value is null)
        {
            if (returnType != BuiltinType.Void)
            {
                throw new CompileException("non-void function should return a value", keyword.Offset);
            }
            return new ReturnStatement(keyword, null);
        }
        if (returnType == BuiltinType.Void)
        {
            throw new CompileException("void function should not return a value", keyword.Offset);
        }
        return new ReturnStatement(keyword, ConvertTo(value, returnType));
    }

    public ExpressionStatement ActOnExpressionStatement(Expression expression) => new(expression);

    public IfStatement ActOnIfStatement(Token keyword, Expression condition, Block then, Statement? elseBranch) =>
        new(keyword, Condition(condition), then, elseBranch);

    public WhileStatement ActOnWhileStatement(Token keyword, Expression condition, Block body) =>
        new(keyword, Condition(condition), body);

    public ForStatement ActOnForStatement(Token keyword, Expression? initializer, Expression? condition, Expression? step, Block body) =>
        new(keyword, initializer, condition is null ? null : Condition(condition), step, body);

    // if / while / for の条件は bool に変換する
    Expression Condition(Expression condition) => ConvertTo(condition, BuiltinType.Bool);

    // ---- 式 ----

    public LiteralExpression ActOnLiteral(Token token) => new(token) { Type = LiteralType(token) };

    public DeclarationReferenceExpression ActOnDeclarationReference(Token name)
    {
        var spelling = Spelling(name);
        var declaration = Lookup(spelling)
            ?? throw new CompileException($"use of undeclared identifier '{spelling}'", name.Offset);
        return new DeclarationReferenceExpression(name) { Declaration = declaration, Type = declaration.ResolvedType };
    }

    public CallExpression ActOnCall(Token callee, List<Expression> arguments)
    {
        var name = Spelling(callee);
        if (!functions.TryGetValue(name, out var function))
        {
            throw new CompileException($"call to undeclared function '{name}'", callee.Offset);
        }
        if (arguments.Count != function.Parameters.Count)
        {
            throw new CompileException(
                $"'{name}' expects {function.Parameters.Count} arguments, but {arguments.Count} were given",
                callee.Offset);
        }

        var converted = arguments
            .Zip(function.Parameters, (argument, parameter) => ConvertTo(argument, parameter.ResolvedType!))
            .ToList();
        return new CallExpression(callee, converted) { Function = function, Type = function.ResolvedReturnType };
    }

    public UnaryExpression ActOnUnaryOperator(Token operatorToken, Expression operand)
    {
        RequireArithmetic(operand);
        var type = BuiltinType.Promote(operand.Type!);
        return new UnaryExpression(operatorToken, ConvertTo(operand, type)) { Type = type };
    }

    public BinaryExpression ActOnBinaryOperator(Token operatorToken, Expression left, Expression right)
    {
        RequireArithmetic(left);
        RequireArithmetic(right);

        // 両辺を共通の型にそろえてから計算する
        var common = BuiltinType.UsualArithmeticConversion(left.Type!, right.Type!);
        var isComparison = operatorToken.Kind is TokenKind.EqualEqual or TokenKind.NotEqual
            or TokenKind.Less or TokenKind.LessEqual or TokenKind.Greater or TokenKind.GreaterEqual;

        return new BinaryExpression(operatorToken, ConvertTo(left, common), ConvertTo(right, common))
        {
            Type = isComparison ? BuiltinType.Bool : common,
        };
    }

    public AssignmentExpression ActOnAssignment(DeclarationReferenceExpression target, Token operatorToken, Expression value) =>
        new(target, operatorToken, ConvertTo(value, target.Type!)) { Type = target.Type };

    // ---- 型 ----

    // 型が違えば ImplicitCastExpression で包む
    Expression ConvertTo(Expression expression, BuiltinType type)
    {
        if (expression.Type == type)
        {
            return expression;
        }
        if (expression.Type == BuiltinType.Void || type == BuiltinType.Void)
        {
            throw new CompileException($"cannot convert '{expression.Type}' to '{type}'", Offset(expression));
        }
        return new ImplicitCastExpression(expression) { Type = type };
    }

    static void RequireArithmetic(Expression expression)
    {
        if (!expression.Type!.IsArithmetic)
        {
            throw new CompileException($"invalid operand of type '{expression.Type}'", Offset(expression));
        }
    }

    static BuiltinType ResolveType(Token token) => token.Kind switch
    {
        TokenKind.VoidKeyword => BuiltinType.Void,
        TokenKind.BoolKeyword => BuiltinType.Bool,
        TokenKind.CharKeyword => BuiltinType.Char,
        TokenKind.IntKeyword => BuiltinType.Int,
        TokenKind.LongKeyword => BuiltinType.Long,
        TokenKind.FloatKeyword => BuiltinType.Float,
        TokenKind.DoubleKeyword => BuiltinType.Double,
        _ => throw new InvalidOperationException($"{token.Kind} is not a type"),
    };

    // 変数と引数は void にできない
    static BuiltinType VariableType(Token token)
    {
        var type = ResolveType(token);
        if (type == BuiltinType.Void)
        {
            throw new CompileException("variable has incomplete type 'void'", token.Offset);
        }
        return type;
    }

    // サフィックスで型が決まる: 1L は long、1.0f は float
    BuiltinType LiteralType(Token token)
    {
        var suffix = Spelling(token)[^1];
        return token.Kind switch
        {
            TokenKind.IntegerLiteral => suffix is 'l' or 'L' ? BuiltinType.Long : BuiltinType.Int,
            TokenKind.FloatLiteral => suffix is 'f' or 'F' ? BuiltinType.Float : BuiltinType.Double,
            TokenKind.CharLiteral => BuiltinType.Char,
            TokenKind.TrueKeyword or TokenKind.FalseKeyword => BuiltinType.Bool,
            _ => throw new InvalidOperationException($"{token.Kind} is not a literal"),
        };
    }

    // ---- スコープ ----

    void PushScope() => scopes.Add(new());

    void PopScope() => scopes.RemoveAt(scopes.Count - 1);

    void Declare(IVariableDeclaration declaration)
    {
        var name = Spelling(declaration.Name);
        if (!scopes[^1].TryAdd(name, declaration))
        {
            throw new CompileException($"redefinition of '{name}'", declaration.Name.Offset);
        }
    }

    IVariableDeclaration? Lookup(string name)
    {
        for (var index = scopes.Count - 1; index >= 0; index--)
        {
            if (scopes[index].TryGetValue(name, out var declaration))
            {
                return declaration;
            }
        }
        return null;
    }

    // ---- 補助 ----

    string Spelling(Token token) => token.Spelling(source);

    // エラー位置として、式の先頭の Token の Offset を返す
    static int Offset(Expression expression) => expression switch
    {
        LiteralExpression literal => literal.Token.Offset,
        DeclarationReferenceExpression reference => reference.Name.Offset,
        CallExpression call => call.Callee.Offset,
        UnaryExpression unary => unary.Operator.Offset,
        BinaryExpression binary => Offset(binary.Left),
        AssignmentExpression assignment => assignment.Target.Name.Offset,
        ImplicitCastExpression cast => Offset(cast.Operand),
        _ => 0,
    };
}
