using Chibikko.Common;

namespace Chibikko.Frontend;

/// <summary>
/// 型なし AST を受け取り、型と参照先を埋めた新しい AST を返す。
/// 暗黙の型変換は ImplicitCastExpression として挿入する(Clang の Sema と同じ)。
/// </summary>
public sealed class SemanticAnalyzer(string source)
{
    readonly string source = source;
    readonly Dictionary<string, FunctionDeclaration> functions = new();

    // 内側のブロックほど後ろ。名前は内側から探す
    readonly List<Dictionary<string, IVariableDeclaration>> scopes = [];

    FunctionDeclaration? currentFunction;

    public TranslationUnit Analyze(TranslationUnit unit)
    {
        // 先に全関数のシグネチャを登録し、定義より前の呼び出しや再帰を許す
        var signatures = unit.Functions.Select(Signature).ToList();
        return new TranslationUnit(signatures.Select(Function).ToList());
    }

    FunctionDeclaration Signature(FunctionDeclaration function)
    {
        var name = Spelling(function.Name);
        if (functions.ContainsKey(name))
        {
            throw new CompileException($"redefinition of function '{name}'", function.Name.Offset);
        }

        var parameters = function.Parameters
            .Select(parameter => parameter with { ResolvedType = VariableType(parameter.Type) })
            .ToList();
        var signature = function with
        {
            Parameters = parameters,
            ResolvedReturnType = ResolveType(function.ReturnType),
        };
        functions[name] = signature;
        return signature;
    }

    FunctionDeclaration Function(FunctionDeclaration signature)
    {
        currentFunction = signature;
        PushScope();
        foreach (var parameter in signature.Parameters)
        {
            Declare(parameter);
        }
        var body = Block(signature.Body);
        PopScope();
        return signature with { Body = body };
    }

    // ---- 文 ----

    Block Block(Block block)
    {
        PushScope();
        var statements = block.Statements.Select(Statement).ToList();
        PopScope();
        return new Block(statements);
    }

    Statement Statement(Statement statement) => statement switch
    {
        Block block => Block(block),
        DeclarationStatement declaration => Declaration(declaration),
        ReturnStatement returnStatement => Return(returnStatement),
        ExpressionStatement expressionStatement => new ExpressionStatement(Expression(expressionStatement.Expression)),
        IfStatement ifStatement => new IfStatement(
            ifStatement.Keyword,
            Condition(ifStatement.Condition),
            Block(ifStatement.Then),
            ifStatement.Else is null ? null : Statement(ifStatement.Else)),
        WhileStatement whileStatement => new WhileStatement(
            whileStatement.Keyword,
            Condition(whileStatement.Condition),
            Block(whileStatement.Body)),
        ForStatement forStatement => For(forStatement),
        _ => throw new InvalidOperationException($"unknown statement {statement.GetType().Name}"),
    };

    DeclarationStatement Declaration(DeclarationStatement declaration)
    {
        var type = VariableType(declaration.Type);

        // 初期化式は宣言の前に解析する。int x = x; の右辺の x は外側の x を指す
        var initializer = declaration.Initializer is null
            ? null
            : ConvertTo(Expression(declaration.Initializer), type, declaration.Initializer);

        var analyzed = declaration with { Initializer = initializer, ResolvedType = type };
        Declare(analyzed);
        return analyzed;
    }

    ReturnStatement Return(ReturnStatement returnStatement)
    {
        var returnType = currentFunction!.ResolvedReturnType!;
        if (returnStatement.Value is null)
        {
            if (returnType != BuiltinType.Void)
            {
                throw new CompileException("non-void function should return a value", returnStatement.Keyword.Offset);
            }
            return returnStatement;
        }
        if (returnType == BuiltinType.Void)
        {
            throw new CompileException("void function should not return a value", returnStatement.Keyword.Offset);
        }
        var value = ConvertTo(Expression(returnStatement.Value), returnType, returnStatement.Value);
        return returnStatement with { Value = value };
    }

    ForStatement For(ForStatement forStatement) => forStatement with
    {
        Initializer = forStatement.Initializer is null ? null : Expression(forStatement.Initializer),
        Condition = forStatement.Condition is null ? null : Condition(forStatement.Condition),
        Step = forStatement.Step is null ? null : Expression(forStatement.Step),
        Body = Block(forStatement.Body),
    };

    // if / while / for の条件は bool に変換する
    Expression Condition(Expression condition) => ConvertTo(Expression(condition), BuiltinType.Bool, condition);

    // ---- 式 ----

    Expression Expression(Expression expression) => expression switch
    {
        LiteralExpression literal => literal with { Type = LiteralType(literal.Token) },
        DeclarationReferenceExpression reference => Reference(reference),
        CallExpression call => Call(call),
        UnaryExpression unary => Unary(unary),
        BinaryExpression binary => Binary(binary),
        AssignmentExpression assignment => Assignment(assignment),
        _ => throw new InvalidOperationException($"unknown expression {expression.GetType().Name}"),
    };

    DeclarationReferenceExpression Reference(DeclarationReferenceExpression reference)
    {
        var name = Spelling(reference.Name);
        var declaration = Lookup(name)
            ?? throw new CompileException($"use of undeclared identifier '{name}'", reference.Name.Offset);
        return reference with { Declaration = declaration, Type = declaration.ResolvedType };
    }

    CallExpression Call(CallExpression call)
    {
        var name = Spelling(call.Callee);
        if (!functions.TryGetValue(name, out var function))
        {
            throw new CompileException($"call to undeclared function '{name}'", call.Callee.Offset);
        }
        if (call.Arguments.Count != function.Parameters.Count)
        {
            throw new CompileException(
                $"'{name}' expects {function.Parameters.Count} arguments, but {call.Arguments.Count} were given",
                call.Callee.Offset);
        }

        var arguments = call.Arguments
            .Zip(function.Parameters, (argument, parameter) =>
                ConvertTo(Expression(argument), parameter.ResolvedType!, argument))
            .ToList();
        return call with { Arguments = arguments, Function = function, Type = function.ResolvedReturnType };
    }

    UnaryExpression Unary(UnaryExpression unary)
    {
        var operand = Expression(unary.Operand);
        RequireArithmetic(operand, unary.Operand);
        var type = BuiltinType.Promote(operand.Type!);
        return unary with { Operand = ConvertTo(operand, type, unary.Operand), Type = type };
    }

    BinaryExpression Binary(BinaryExpression binary)
    {
        var left = Expression(binary.Left);
        var right = Expression(binary.Right);
        RequireArithmetic(left, binary.Left);
        RequireArithmetic(right, binary.Right);

        // 両辺を共通の型にそろえてから計算する
        var common = BuiltinType.UsualArithmeticConversion(left.Type!, right.Type!);
        var isComparison = binary.Operator.Kind is TokenKind.EqualEqual or TokenKind.NotEqual
            or TokenKind.Less or TokenKind.LessEqual or TokenKind.Greater or TokenKind.GreaterEqual;

        return binary with
        {
            Left = ConvertTo(left, common, binary.Left),
            Right = ConvertTo(right, common, binary.Right),
            Type = isComparison ? BuiltinType.Bool : common,
        };
    }

    AssignmentExpression Assignment(AssignmentExpression assignment)
    {
        var target = Reference(assignment.Target);
        var value = ConvertTo(Expression(assignment.Value), target.Type!, assignment.Value);
        return assignment with { Target = target, Value = value, Type = target.Type };
    }

    // ---- 型 ----

    // 型が違えば ImplicitCastExpression で包む。original はエラー位置を取るための解析前のノード
    Expression ConvertTo(Expression expression, BuiltinType type, Expression original)
    {
        if (expression.Type == type)
        {
            return expression;
        }
        if (expression.Type == BuiltinType.Void || type == BuiltinType.Void)
        {
            throw new CompileException($"cannot convert '{expression.Type}' to '{type}'", Offset(original));
        }
        return new ImplicitCastExpression(expression) { Type = type };
    }

    void RequireArithmetic(Expression expression, Expression original)
    {
        if (!expression.Type!.IsArithmetic)
        {
            throw new CompileException($"invalid operand of type '{expression.Type}'", Offset(original));
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
