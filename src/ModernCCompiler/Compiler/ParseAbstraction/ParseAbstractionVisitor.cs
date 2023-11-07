﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Compiler.Models;
using Compiler.Models.Tree;
using System.Diagnostics;
using static ModernCParser;

namespace Compiler.ParseAbstraction
{
    /// <summary>
    /// Converts the parse tree generated by ANTLR into an Abstract Syntax Tree.
    /// </summary>
    public class ParseAbstractionVisitor : ModernCBaseVisitor<AbstractSyntaxTree>
    {
        public override ProgramRoot VisitProgram([NotNull] ProgramContext context)
        {
            var span = GetSpanOfContext(context);
            var functionDefinitions = context.functionDefinition()
                .Select(c => VisitFunctionDefinition(c));
            return new ProgramRoot(span, functionDefinitions);
        }

        public override FunctionDefinition VisitFunctionDefinition([NotNull] FunctionDefinitionContext context)
        {
            var span = GetSpanOfContext(context);
            var returnType = VisitType(context.type());
            var id = VisitId(context.id());
            var parameters = context.parameterList() != null ? VisitParameterList(context.parameterList()) : null;
            var body = VisitCompoundStatement(context.compoundStatement());
            return new FunctionDefinition(span, returnType, id, parameters, body);
        }

        public override TypeNode VisitType([NotNull] TypeContext context)
        {
            var span = GetSpanOfContext(context);
            if (context.VOID_TYPE() != null)
            {
                return new VoidTypeNode(span);
            } 
            else if (context.primitiveType() != null)
            {
                return VisitPrimitiveType(context.primitiveType());
            }
            
            // TODO add handling of complex types here
            throw new NotImplementedException();
        }

        public override PrimitiveTypeNode VisitPrimitiveType([NotNull] PrimitiveTypeContext context)
        {
            var span = GetSpanOfContext(context);
            if (context.INT_TYPE() != null)
            {
                return new IntTypeNode(span);
            }
            else if (context.BOOL_TYPE() != null)
            {
                return new BoolTypeNode(span);
            }

            throw new NotImplementedException();
        }

        public override ParameterList VisitParameterList([NotNull] ParameterListContext context)
        {
            var span = GetSpanOfContext(context);
            var parameters = context.parameter()
                .Select(c => VisitParameter(c));
            return new ParameterList(span, parameters);
        }

        public override Parameter VisitParameter([NotNull] ParameterContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());
            return new Parameter(span, type, id);
        }

        public override CompoundStatement VisitCompoundStatement([NotNull] CompoundStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var statements = context.statement()
                .Select(c => VisitStatement(c)).ToList();

            if (context.returnStatement() != null)
            {
                var returnStatement = VisitReturnStatement(context.returnStatement());
                statements.Add(returnStatement);
            }

            return new CompoundStatement(span, statements);
        }

        public override Statement VisitStatement([NotNull] StatementContext context)
        {
            var ast = base.VisitStatement(context);
            if (ast is Statement statement)
            {
                return statement;
            }

            throw new Exception($"Tried to parse {ast.GetType()} as a statement, something is wrong with the compiler");
        }

        public override PrintStatement VisitPrintStatement([NotNull] PrintStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            return new PrintStatement(span, expression);
        }

        public override VariableDefinitionStatement VisitVariableDefinitionStatement([NotNull] VariableDefinitionStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());
            return new VariableDefinitionStatement(span, type, id);
        }

        public override AssignmentStatement VisitAssignmentStatement([NotNull] AssignmentStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expressions = context.expression();
            Debug.Assert(expressions.Length == 2);

            var left = VisitExpression(expressions[0]); 
            var right = VisitExpression(expressions[1]);
            return new AssignmentStatement(span, left, right);
        }

        public override VariableDefinitionAndAssignmentStatement VisitVariableDefinitionAndAssignmentStatement([NotNull] VariableDefinitionAndAssignmentStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());
            var expression = VisitExpression(context.expression());
            return new VariableDefinitionAndAssignmentStatement(span, type, id, expression);
        }

        public override ReturnStatement VisitReturnStatement([NotNull] ReturnStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = context.expression() != null ? VisitExpression(context.expression()) : null;
            return new ReturnStatement(span, expression);
        }

        public override Expression VisitExpression([NotNull] ExpressionContext context)
        {
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = context.GetChild(1).GetText();
                var left = VisitExpression(context.expression());
                var right = VisitTerm(context.term());
                return new BinaryOperatorExpression(span, op, left, right);
            }
            else
            {
                return VisitTerm(context.term());
            }
        }

        public override Expression VisitTerm([NotNull] TermContext context)
        {
            
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = context.GetChild(1).GetText();
                var left = VisitTerm(context.term());
                var right = VisitFactor(context.factor());
                return new BinaryOperatorExpression(span, op, left, right);
            }
            else
            {
                return VisitFactor(context.factor());
            }
        }

        public override Expression VisitFactor([NotNull] FactorContext context)
        {
            if (context.unaryExpression() != null)
            {
                return VisitUnaryExpression(context.unaryExpression());
            }
            else if (context.idExpression() != null)
            {
                return VisitIdExpression(context.idExpression());
            }
            else if (context.intLiteral() != null)
            {
                return VisitIntLiteral(context.intLiteral());
            }
            else if (context.boolLiteral() != null)
            {
                return VisitBoolLiteral(context.boolLiteral());
            }
            else if (context.expression() != null)
            {
                return VisitExpression(context.expression());
            }

            throw new Exception($"Tried to parse {context.GetText()} as an expression, something is wrong with the compiler");
        }

        public override UnaryOperatorExpression VisitUnaryExpression([NotNull] UnaryExpressionContext context)
        {
            var span = GetSpanOfContext(context);
            var op = context.GetChild(0).GetText();
            var expression = VisitFactor(context.factor());
            return new UnaryOperatorExpression(span, op, expression);
        }

        public override IntLiteralExpression VisitIntLiteral([NotNull] IntLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            var text = context.GetText();
            if (int.TryParse(text, out var value))
            {
                return new IntLiteralExpression(span, value);  
            }

            throw new Exception($"Tried to parse {text} as an int, something is wrong with the compiler");
        }

        public override BoolLiteralExpression VisitBoolLiteral([NotNull] BoolLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            var text = context.GetText();
            if (bool.TryParse(text, out var value))
            {
                return new BoolLiteralExpression(span, value);
            }

            throw new Exception($"Tried to parse {text} as a bool, something is wrong with the compiler");
        }

        public override Expression VisitIdExpression([NotNull] IdExpressionContext context)
        {
            var span = GetSpanOfContext(context);
            var id = VisitId(context.id());
            return new IdExpression(span, id);
        }

        public override IdNode VisitId([NotNull] IdContext context)
        {
            var span = GetSpanOfContext(context);
            return new IdNode(span, context.GetText());
        }

        private static Span GetSpanOfContext(ParserRuleContext context)
        {
            return new Span(
                context.Start.Line,
                context.Start.Column,
                context.Stop.Line,
                context.Stop.Column);
        }
    }
}
