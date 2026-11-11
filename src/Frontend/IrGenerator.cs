using System.Diagnostics;
using System.Globalization;
using Chibikko.Common;
using Chibikko.Common.Ir;
using Chibikko.Common.Utility;
using Chibikko.Frontend.Type;

namespace Chibikko.Frontend;

public class IrGenerator
{
    // 関数 SymbolTable
    readonly Dictionary<FunctionDeclaration, IrFunction> functionsTable = new(ReferenceEqualityComparer.Instance);

    int labelSuffix = 1;

    public Ir Generate(TranslationUnit translationUnit)
    {
        Ir ir = new();
        foreach (var functionDeclaration in translationUnit.Functions)
        {
            // 関数呼び出しで不具合が生じないようにまず空の関数インスタンスを生成する.
            var returnType = functionDeclaration.ResolvedReturnType;
            IrType? irType = returnType == BuiltinType.Void ? null : returnType.ToIrType();
            IrFunction irFunction = new(functionDeclaration.Name.Text, irType);
            functionsTable[functionDeclaration] = irFunction;
            ir.Functions.Add(irFunction);
        }

        foreach (var functionDeclaration in translationUnit.Functions)
        {
            GenerateFunctionIr(functionsTable[functionDeclaration], functionDeclaration);
        }

        return ir;
    }

    void GenerateFunctionIr(IrFunction irFunction, FunctionDeclaration functionDeclaration)
    {
        FunctionContext context = new(irFunction);

        foreach (var parameter in functionDeclaration.Parameters)
        {
            var variable = new Variable(parameter.ResolvedType.ToIrType(), parameter.Name.Text);
            context.LocalVariablesTable[parameter] = variable;
            irFunction.Parameters.Add(variable);
        }

        GenerateStatement(context, functionDeclaration.Body);
        context.Emit(new Quadruple { OperationType = OperationType.Return });
    }

    void GenerateStatement(FunctionContext context, Statement statement)
    {
        switch (statement)
        {
            case Block block:
                foreach (var child in block.Statements)
                {
                    GenerateStatement(context, child);
                }
                break;
            case DeclarationStatement declaration:
                GenerateDeclaration(context, declaration);
                break;
            case ExpressionStatement { Expression: CallExpression call }:
                GenerateCall(context, call);
                break;
            case ExpressionStatement expressionStatement:
                GenerateExpression(context, expressionStatement.Expression);
                break;
            case ReturnStatement returnStatement:
                GenerateReturn(context, returnStatement);
                break;
            case IfStatement ifStatement:
                GenerateIf(context, ifStatement);
                break;
            case WhileStatement whileStatement:
                GenerateWhile(context, whileStatement);
                break;
            case ForStatement forStatement:
                GenerateFor(context, forStatement);
                break;
            default:
                throw new NotSupportedException($"Unsupported statement: {statement.GetType().Name}");
        }
    }

    void GenerateDeclaration(FunctionContext context, DeclarationStatement declaration)
    {
        var variable = context.DeclareLocal(declaration);
        if (declaration.Initializer is null)
        {
            return;
        }

        var value = GenerateExpression(context, declaration.Initializer);
        context.Emit(new Quadruple { OperationType = OperationType.Assign, Destination = variable, Source1 = value });
    }

    void GenerateReturn(FunctionContext context, ReturnStatement returnStatement)
    {
        var value = returnStatement.Value is null ? null : GenerateExpression(context, returnStatement.Value);
        context.Emit(new Quadruple { OperationType = OperationType.Return, Source1 = value });
    }

    void GenerateIf(FunctionContext context, IfStatement ifStatement)
    {
        var endLabel = new Label($"L{labelSuffix}");
        var elseLabel = ifStatement.Else is null ? endLabel : new Label($"L{labelSuffix}");

        var condition = GenerateExpression(context, ifStatement.Condition);
        context.EmitJumpIfFalse(condition, elseLabel);
        GenerateStatement(context, ifStatement.Then);

        if (ifStatement.Else is not null)
        {
            context.EmitJump(endLabel);
            context.EmitLabel(elseLabel);
            GenerateStatement(context, ifStatement.Else);
        }

        context.EmitLabel(endLabel);
    }

    void GenerateWhile(FunctionContext context, WhileStatement whileStatement)
    {
        var beginLabel = new Label($"L{labelSuffix}");
        var endLabel = new Label($"L{labelSuffix}");

        context.EmitLabel(beginLabel);
        var condition = GenerateExpression(context, whileStatement.Condition);
        context.EmitJumpIfFalse(condition, endLabel);
        GenerateStatement(context, whileStatement.Body);
        context.EmitJump(beginLabel);
        context.EmitLabel(endLabel);
    }

    void GenerateFor(FunctionContext context, ForStatement forStatement)
    {
        var beginLabel = new Label($"L{labelSuffix}");
        var endLabel = new Label($"L{labelSuffix}");

        if (forStatement.Initializer is not null)
        {
            GenerateExpression(context, forStatement.Initializer);
        }

        context.EmitLabel(beginLabel);
        if (forStatement.Condition is not null)
        {
            var condition = GenerateExpression(context, forStatement.Condition);
            context.EmitJumpIfFalse(condition, endLabel);
        }

        GenerateStatement(context, forStatement.Body);
        if (forStatement.Step is not null)
        {
            GenerateExpression(context, forStatement.Step);
        }

        context.EmitJump(beginLabel);
        context.EmitLabel(endLabel);
    }

    Operand GenerateExpression(FunctionContext context, Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => GenerateLiteral(literal),
            DeclarationReferenceExpression reference => context.LocalVariablesTable[reference.Declaration!],
            CallExpression call => GenerateCall(context, call) ?? throw new InvalidOperationException($"void call used as value: {call.Callee.Text}"),
            UnaryExpression unary => GenerateUnary(context, unary),
            BinaryExpression binary => GenerateBinary(context, binary),
            AssignmentExpression assignment => GenerateAssignment(context, assignment),
            ImplicitCastExpression cast => throw new NotSupportedException($"ImplicitCastExpression is not supported: {cast.Operand.Type} -> {cast.Type}"),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}"),
        };
    }

    static Constant GenerateLiteral(LiteralExpression literal)
    {
        var token = literal.Token;
        var type = literal.Type.ToIrType();
        return token.Kind switch
        {
            TokenKind.IntegerLiteral => new IntegerConstant(type, long.Parse(token.Text.TrimEnd('l', 'L'), CultureInfo.InvariantCulture)),
            TokenKind.FloatLiteral => new FloatConstant(type, double.Parse(token.Text.TrimEnd('f', 'F'), CultureInfo.InvariantCulture)),
            TokenKind.CharLiteral => new IntegerConstant(type, LiteralUtility.ParseCharLiteral(token.Text)),
            TokenKind.TrueKeyword => new IntegerConstant(type, 1),
            TokenKind.FalseKeyword => new IntegerConstant(type, 0),
            _ => throw new UnreachableException($"Unknown literal: {token.Kind}"),
        };
    }

    Operand? GenerateCall(FunctionContext context, CallExpression call)
    {
        var arguments = call.Arguments.Select(argument => GenerateExpression(context, argument)).ToList();
        foreach (var argument in arguments)
        {
            context.Emit(new Quadruple { OperationType = OperationType.Param, Source1 = argument });
        }

        var function = functionsTable[call.Function!];
        var result = function.ReturnType is { } returnType ? context.NewTemporary(returnType) : null;
        context.Emit(new Quadruple
        {
            OperationType = OperationType.Call,
            Destination = result,
            Source1 = new FunctionRef(function),
        });
        return result;
    }

    Operand GenerateUnary(FunctionContext context, UnaryExpression unary)
    {
        var operand = GenerateExpression(context, unary.Operand);
        if (unary.Operator.Kind is TokenKind.Plus)
        {
            return operand;
        }

        var result = context.NewTemporary(unary.Type.ToIrType());
        context.Emit(new Quadruple { OperationType = OperationType.Negate, Destination = result, Source1 = operand });
        return result;
    }

    Operand GenerateBinary(FunctionContext context, BinaryExpression binary)
    {
        var left = GenerateExpression(context, binary.Left);
        var right = GenerateExpression(context, binary.Right);
        var result = context.NewTemporary(binary.Type.ToIrType());
        context.Emit(new Quadruple
        {
            OperationType = binary.Operator.Kind.ToOperationType(),
            Destination = result,
            Source1 = left,
            Source2 = right,
        });
        return result;
    }

    Operand GenerateAssignment(FunctionContext context, AssignmentExpression assignment)
    {
        var target = context.LocalVariablesTable[assignment.Target.Declaration!];
        var value = GenerateExpression(context, assignment.Value);
        context.Emit(new Quadruple { OperationType = OperationType.Assign, Destination = target, Source1 = value });
        return target;
    }
}
