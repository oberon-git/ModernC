﻿using Compiler.ErrorHandling;
using Compiler.Models.NameResolution;
using Compiler.Models.NameResolution.Types;
using Compiler.Models.Operators;
using Compiler.Models.Tree;

namespace Compiler.TreeWalking.TypeCheck
{
    /// <summary>
    /// Does local type checking for compound statements.
    /// </summary>
    public static class LocalTypeChecker
    {
        private class Context
        {
            public Scope? Scope { get; set; }
            public FunctionDefinition? EnclosingFunction { get; set; }
            public bool LValue { get; set; } = false;
            public FunctionDefinition? RValueFunction { get; set; }
        }

        public static void Walk(ProgramRoot program)
        {
            var context = new Context();
            VisitProgramRoot(program, context);
        }

        private static void VisitProgramRoot(ProgramRoot program, Context context)
        {
            foreach (var functionDefinition in program.FunctionDefinitions)
            {
                VisitFunctionDefinition(functionDefinition, context);
            }
        }

        private static void VisitFunctionDefinition(FunctionDefinition functionDefinition, Context context)
        {
            if (functionDefinition.FunctionScope == null)
            {
                throw new Exception("Function scope was null");
            }

            context.Scope = functionDefinition.FunctionScope;
            context.EnclosingFunction = functionDefinition;
            VisitCompoundStatement(functionDefinition.Body, context);
        }

        private static void VisitCompoundStatement(CompoundStatement body, Context context)
        {
            body.LocalScope = new Scope(context.Scope);
            context.Scope = body.LocalScope;
            foreach (var statement in body.Statements)
            {
                VisitStatement(statement, context);
            }
        }

        private static void VisitStatement(Statement statement, Context context)
        {
            switch (statement)
            {
                case PrintStatement s:
                    VisitPrintStatement(s, context);
                    break;
                case VariableDefinitionStatement s:
                    VisitVariableDefinitionStatement(s, context);
                    break;
                case AssignmentStatement s:
                    VisitAssignmentStatement(s, context);
                    break;
                case IncrementStatement s:
                    VisitIncrementStatement(s, context);
                    break;
                case VariableDefinitionAndAssignmentStatement s:
                    VisitVariableDefinitionAndAssignmentStatement(s, context);
                    break;
                case CallStatement s:
                    VisitCallStatement(s, context);
                    break;
                case IfStatement s:
                    VisitIfStatement(s, context);
                    break;
                case WhileStatement s:
                    VisitWhileStatement(s, context);
                    break;
                case DoWhileStatement s:
                    VisitDoWhileStatement(s, context);
                    break;
                case ForStatement s:
                    VisitForStatement(s, context);
                    break;
                case ReturnStatement s:
                    VisitReturnStatement(s, context);
                    break;
                case CompoundStatement s:
                    VisitCompoundStatement(s, context);
                    break;
                default:
                    throw new NotImplementedException($"Unknown statement {statement}");
            }
        }

        private static void VisitPrintStatement(PrintStatement statement, Context context)
        {
            var type = VisitExpression(statement.Expression, context);
            if (type.GetType() == typeof(VoidType))
            {
                ErrorHandler.Throw("Expressions can not have a type of void.", statement);
            }
        }

        private static void VisitVariableDefinitionStatement(VariableDefinitionStatement statement, Context context)
        {
            var type = statement.Type.ToSemanticType();
            context.Scope?.Add(statement.Id, type);
            VisitIdNode(statement.Id, context);
        }

        private static void VisitAssignmentStatement(AssignmentStatement statement, Context context)
        {
            SemanticType rightType;
            if (statement.BinaryExpression != null)
            {
                rightType = VisitExpression(statement.BinaryExpression, context);
            }
            else
            {
                rightType = VisitExpression(statement.Right, context);
            }
            
            context.LValue = true;
            var leftType = VisitExpression(statement.Left, context);
            context.LValue = false;

            switch (leftType)
            {
                case RealType when rightType is NumberType:
                    break;
                case IntegralType when rightType is IntegralType:
                    break;
                default:
                    if (leftType != rightType)
                    {
                        ErrorHandler.Throw("Variable assignment must have matching types.", statement);
                    }

                    break;
            }
        }

        private static void VisitIncrementStatement(IncrementStatement s, Context context)
        {
            context.LValue = true;
            var leftType = VisitExpression(s.Left, context);
            context.LValue = false;

            if (leftType is not IntegralType)
            {
                ErrorHandler.Throw("Increment statements cannot be used on non integral types", s);
            }
        }


        private static void VisitVariableDefinitionAndAssignmentStatement(VariableDefinitionAndAssignmentStatement statement, Context context)
        {
            var type = statement.Type.ToSemanticType();
            var expressionType = VisitExpression(statement.Expression, context);
            switch (type)
            {
                case RealType when expressionType is NumberType:
                    break;
                case IntegralType when expressionType is IntegralType:
                    break;
                default:
                    if (type != expressionType)
                    {
                        ErrorHandler.Throw("Variable assignment must have matching types.", statement);
                    }

                    break;
            }

            context.Scope?.Add(statement.Id, type);
            context.LValue = true;
            VisitIdNode(statement.Id, context);
            context.LValue = false;
        }

        private static void VisitCallStatement(CallStatement s, Context context)
        {
            VisitCallExpression(s.CallExpression, context);
        }

        private static void VisitIfStatement(IfStatement s, Context context)
        {
            var ifExpressionType = VisitExpression(s.IfExpression, context);
            if (ifExpressionType is not BoolType)
            {
                ErrorHandler.Throw("If expression must be of type boolean", s);
            }

            VisitCompoundStatement(s.IfBody, context);

            foreach (var e in s.ElifExpressions)
            {
                var elifExpressionType = VisitExpression(e, context);
                if (elifExpressionType is not BoolType)
                {
                    ErrorHandler.Throw("If expression must be of type boolean", s);
                }
            }

            foreach (var b in s.ElifBodies) 
            {
                VisitCompoundStatement(b, context);
            }
            
            if (s.ElseBody != null)
            {
                VisitCompoundStatement(s.ElseBody, context);
            }
        }

        private static void VisitWhileStatement(WhileStatement s, Context context)
        {
            var expressionType = VisitExpression(s.Expression, context);
            if (expressionType is not BoolType)
            {
                ErrorHandler.Throw("While loop expression must be of type boolean", s);
            }

            VisitCompoundStatement(s.Body, context);
        }

        private static void VisitDoWhileStatement(DoWhileStatement s, Context context)
        {
            VisitCompoundStatement(s.Body, context);
            var expressionType = VisitExpression(s.Expression, context);
            if (expressionType is not BoolType)
            {
                ErrorHandler.Throw("Do while loop expression must be of type boolean", s);
            }
        }

        private static void VisitForStatement(ForStatement s, Context context)
        {
            VisitStatement(s.InitialStatement, context);
            var expressionType = VisitExpression(s.Expression, context);
            if (expressionType is not BoolType)
            {
                ErrorHandler.Throw("For loop expression must be of type boolean", s);
            }

            VisitStatement(s.UpdateStatement, context);
            VisitCompoundStatement(s.Body, context);
        }

        private static void VisitReturnStatement(ReturnStatement statement, Context context)
        {
            if (context.EnclosingFunction?.Id?.Symbol?.Type is FunctionType functionType)
            {
                if (functionType.ReturnType is VoidType && statement.Expression != null)
                {
                    ErrorHandler.Throw("Function has a return type of void and cannot return a value.", statement);
                }
                else if (functionType.ReturnType is not VoidType && statement.Expression == null)
                {
                    ErrorHandler.Throw("Function cannot have an empty return.", statement);
                }

                if (statement.Expression != null)
                {
                    var expressionType = VisitExpression(statement.Expression, context);
                    if (functionType.ReturnType is not VoidType && expressionType != functionType.ReturnType)
                    {
                        ErrorHandler.Throw("Function return type does not match return value.", statement);
                    }
                }

                statement.EnclosingFunction = context.EnclosingFunction;

            }
            else
            {
                throw new Exception("Enclosing function is null");
            }
        }


        private static SemanticType VisitExpression(Expression expression, Context context)
        {
            expression.Type = expression switch
            {
                BinaryOperatorExpression e => VisitBinaryOperatorExpression(e, context),
                CallExpression e => VisitCallExpression(e, context),
                ReadExpression => new ByteType(),
                UnaryOperatorExpression e => VisitUnaryOperatorExpression(e, context),
                IdExpression e => VisitIdExpression(e, context),
                IntLiteralExpression => new IntType(),
                ByteLiteralExpression => new ByteType(),
                FloatLiteralExpression => new FloatType(),
                BoolLiteralExpression => new BoolType(),
                _ => throw new NotImplementedException($"Unknown expression: {expression}"),
            }; ;

            return expression.Type; 
        }

        private static SemanticType VisitCallExpression(CallExpression e, Context context)
        {
            var type = VisitIdExpression(e.Function, context);
            if (type is FunctionType functionType)
            {
                var argTypes = VisitArgumentList(e.ArgumentList, context);
                if (argTypes.Count != functionType.Parameters.Count)
                {
                    ErrorHandler.Throw($"{e.Function.Id.Value} was called with an incorrect number of arguments.", e);
                }

                for (var i = 0; i < argTypes.Count; i++)
                {
                    if (argTypes[i] != functionType.Parameters[i])
                    {
                        ErrorHandler.Throw($"{e.Function.Id.Value} was called with incorrect types.", e);
                    }
                }

                return functionType.ReturnType;
            }

            ErrorHandler.Throw($"{e.Function.Id.Value} cannot be called like a function.", e);
            throw new Exception("Error handler did not stop execution");
        }

        private static IList<SemanticType> VisitArgumentList(ArgumentList argumentList, Context context)
        {
            return argumentList.Arguments
                .Select(a => VisitExpression(a, context))
                .ToList();
        }

        private static SemanticType VisitBinaryOperatorExpression(BinaryOperatorExpression e, Context context)
        {
            var leftType = VisitExpression(e.LeftOperand, context);
            var rightType = VisitExpression(e.RightOperand, context);
            switch (leftType)
            {
                case IntegralType when rightType is IntegralType:
                    break;
                case RealType when rightType is RealType:
                    break;
                default:
                    if (leftType != rightType)
                    {
                        ErrorHandler.Throw("Binary operands must have matching types.", e);
                    }

                    break;
            }

            switch (e.Operator)
            {
                case BinaryOperator.EqualTo:
                case BinaryOperator.NotEqualTo:
                case BinaryOperator.LessThan:
                case BinaryOperator.LessThanEqualTo:
                case BinaryOperator.GreaterThan:
                case BinaryOperator.GreaterThanEqualTo:
                    return new BoolType();
                case BinaryOperator.Plus:
                    if (leftType is not NumberType)
                    {
                        ErrorHandler.Throw("Only numbers can be added", e);
                    }

                    return leftType;
                case BinaryOperator.Minus:
                    if (leftType is not NumberType)
                    {
                        ErrorHandler.Throw("Only numbers can be subtracted", e);
                    }

                    return leftType;
                case BinaryOperator.Times:
                    if (leftType is not NumberType)
                    {
                        ErrorHandler.Throw("Only numbers can be multiplied", e);
                    }

                    return leftType; ;
                case BinaryOperator.DividedBy:
                    if (leftType is not NumberType)
                    {
                        ErrorHandler.Throw("Only numbers can be divided", e);
                    }

                    return leftType;
                case BinaryOperator.And:
                    if (leftType is not BoolType)
                    {
                        ErrorHandler.Throw("Only booleans can be anded", e);
                    }

                    return leftType;
                case BinaryOperator.Or:
                    if (leftType is not BoolType)
                    {
                        ErrorHandler.Throw("Only booleans can be ored", e);
                    }

                    return leftType;
                default:
                    return leftType;
            }
        }

        private static SemanticType VisitUnaryOperatorExpression(UnaryOperatorExpression e, Context context)
        {
            return VisitExpression(e.Operand, context);
        }

        private static SemanticType VisitIdExpression(IdExpression e, Context context)
        {
            return VisitIdNode(e.Id, context);
        }

        private static SemanticType VisitIdNode(IdNode id, Context context)
        {
            id.Symbol = context.Scope?.Lookup(id);
            if (id.Symbol == null)
            {
                throw new Exception("Symbol was null");
            }

            if (id.Symbol.Type is FunctionType)
            {
                if (context.LValue && id.Symbol.EnclosingFunction == null)
                {
                    if (context.RValueFunction == null)
                    {
                        throw new Exception("RValueFunction was null");
                    }

                    id.Symbol.EnclosingFunction = context.RValueFunction;
                }
                else if (!context.LValue)
                {
                    if (id.Symbol.EnclosingFunction == null)
                    {
                        id.Symbol.EnclosingFunction = context.Scope?.LookupFunction(id);
                    }
                    
                    context.RValueFunction = id.Symbol.EnclosingFunction;
                }
            }

            return id.Symbol.Type;
        }
    }
}
