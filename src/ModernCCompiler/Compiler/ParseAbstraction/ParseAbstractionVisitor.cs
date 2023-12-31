﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Compiler.ErrorHandling;
using Compiler.Models;
using Compiler.Models.Operators;
using Compiler.Models.Tree;
using static ModernCParser;

namespace Compiler.ParseAbstraction
{
    /// <summary>
    /// Converts the parse tree generated by ANTLR into an Abstract Syntax Tree.
    /// </summary>
    public sealed class ParseAbstractionVisitor : ModernCBaseVisitor<AbstractSyntaxTree>
    {
        public override ProgramRoot VisitProgram([NotNull] ProgramContext context)
        {
            Span span;
            try
            {
                span = GetSpanOfContext(context);
            }
            catch
            {
                ErrorHandler.Throw("An empty program has no meaning");
                throw;
            }

            var functionDefinitons = context.functionDefinition()
                .Select(VisitFunctionDefinition)
                .ToList();
            var definitions = context.definition()
                .Select(VisitDefinition)
                .ToList();
            var statements = context.topLevelStatement()
                .Select(VisitTopLevelStatement)
                .ToList();
            return new ProgramRoot(span, statements, definitions, functionDefinitons);
        }

        public override Statement VisitTopLevelStatement([NotNull] TopLevelStatementContext context)
        {
            if (context.variableDefinitionStatement() != null)
            {
                return VisitVariableDefinitionStatement(context.variableDefinitionStatement());
            }
            else if (context.variableDefinitionAndAssignmentStatement() != null)
            {
                return VisitVariableDefinitionAndAssignmentStatement(context.variableDefinitionAndAssignmentStatement());
            }

            throw new Exception($"Could not parse {context.GetText()} as top level statement");
        }

        public override FunctionDefinition VisitFunctionDefinition([NotNull] FunctionDefinitionContext context)
        {
            var span = GetSpanOfContext(context);
            var id = VisitId(context.id());
            var parameters = context.parameterList()?
                .parameter()
                .Select(VisitParameter)
                .ToList();
            var returnType = VisitType(context.type());
            var body = VisitCompoundStatement(context.compoundStatement());
            return new FunctionDefinition(span, id, parameters ?? new List<Parameter>(), returnType, body);
        }

        public override Parameter VisitParameter([NotNull] ParameterContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());
            return new Parameter(span, type, id);
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
            else if (context.functionType() != null)
            {
                return VisitFunctionType(context.functionType());
            }
            else if (context.userDefinedType() != null)
            {
                return VisitUserDefinedType(context.userDefinedType());
            }
            else if (context.arrayDefinitionType != null)
            {
                var elementType = VisitType(context.arrayDefinitionType);
                var size = context.intLiteral() != null ? VisitIntLiteral(context.intLiteral()) : null;
                return new ArrayTypeNode(span, elementType, size?.Value);
            }
            else if (context.arrayParameritizedType != null)
            {
                var elementType = VisitType(context.arrayParameritizedType);
                var parameter = VisitId(context.id());
                return new ParameterizedArrayTypeNode(span, elementType, parameter);
            }
            else if (context.pointerType != null)
            {
                var underlyingType = VisitType(context.pointerType);
                return new PointerTypeNode(span, underlyingType);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override PrimitiveTypeNode VisitPrimitiveType([NotNull] PrimitiveTypeContext context)
        {
            var span = GetSpanOfContext(context);
            if (context.INT_TYPE() != null)
            {
                return new IntTypeNode(span);
            }
            else if (context.BYTE_TYPE() != null)
            {
                return new ByteTypeNode(span);
            }
            else if (context.FLOAT_TYPE() != null)
            {
                return new FloatTypeNode(span);
            }
            else if (context.BOOL_TYPE() != null)
            {
                return new BoolTypeNode(span);
            }

            throw new NotImplementedException();
        }

        public override FunctionTypeNode VisitFunctionType([NotNull] FunctionTypeContext context)
        {
            var span = GetSpanOfContext(context);
            var types = context.typeList().type()
                .Select(VisitType)
                .ToList();
            return new FunctionTypeNode(span, types);
        }

        public override UserDefinedTypeNode VisitUserDefinedType([NotNull] UserDefinedTypeContext context)
        {
            var span = GetSpanOfContext(context);
            var id = VisitId(context.id());
            return new UserDefinedTypeNode(span, id);
        }

        public override StructFieldDefinition VisitStructFieldDefinition([NotNull] StructFieldDefinitionContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());

            Expression? expression = null;
            if (context.expression() != null)
            {
                expression = VisitExpression(context.expression());
            }

            return new StructFieldDefinition(span, type, id, expression);
        }

        public override Definition VisitDefinition([NotNull] DefinitionContext context)
        {
            var ast = base.VisitDefinition(context);
            if (ast is Definition definition)
            {
                return definition;
            }

            throw new Exception($"Tried to parse {context.GetText()} as a definition");
        }

        public override StructDefinition VisitStructDefinition([NotNull] StructDefinitionContext context)
        {
            var span = GetSpanOfContext(context);
            var userDefinedType = VisitUserDefinedType(context.userDefinedType());
            var fields = context.structFieldDefinition()
                .Select(VisitStructFieldDefinition)
                .ToList();
            var structType = new StructTypeNode(userDefinedType.Span, userDefinedType.Id, fields);
            return new StructDefinition(span, structType, fields);
        }

        public override AliasDefinition VisitAliasDefinition([NotNull] AliasDefinitionContext context)
        {
            var span = GetSpanOfContext(context);
            var userDefinedType = VisitUserDefinedType(context.userDefinedType());
            var aliasedType = VisitType(context.type());
            return new AliasDefinition(span, userDefinedType, aliasedType);
        }

        public override CompoundStatement VisitCompoundStatement([NotNull] CompoundStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var statements = context.statement()
                .Select(VisitStatement)
                .ToList();
            return new CompoundStatement(span, statements);
        }

        public override Statement VisitStatement([NotNull] StatementContext context)
        {
            if (context.simpleStatement() != null)
            {
                return VisitSimpleStatement(context.simpleStatement());
            }

            var ast = base.VisitStatement(context);
            if (ast is Statement statement)
            {
                return statement;
            }

            throw new Exception($"Tried to parse {ast.GetType()} as a statement, something is wrong with the compiler");
        }

        public override Statement VisitSimpleStatement([NotNull] SimpleStatementContext context)
        {
            var ast = base.VisitSimpleStatement(context);
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

        public override PrintLineStatement VisitPrintlnStatement([NotNull] PrintlnStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            return new PrintLineStatement(span, expression);
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
            var left = VisitFactor(context.factor());
            var op = GetAssignmentOperator(context.GetChild(1).GetText());
            var right = VisitExpression(context.expression());
            return new AssignmentStatement(span, left, op, right);
        }

        public override IncrementStatement VisitIncrementStatement([NotNull] IncrementStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            var op = GetIncrementOperator(context.GetChild(1).GetText());
            return new IncrementStatement(span, expression, op);
        }

        public override VariableDefinitionAndAssignmentStatement VisitVariableDefinitionAndAssignmentStatement([NotNull] VariableDefinitionAndAssignmentStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var type = VisitType(context.type());
            var id = VisitId(context.id());
            Expression expression;
            if (context.expression() != null)
            {
                expression = VisitExpression(context.expression());
            }
            else
            {
                throw new NotImplementedException();
            }

            return new VariableDefinitionAndAssignmentStatement(span, type, id, expression);
        }

        public override CallStatement VisitCallStatement([NotNull] CallStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            if (expression is CallExpression callExpression)
            {
                return new CallStatement(span, callExpression);
            }
            else
            {
                ErrorHandler.Throw("Expression cannot be used as a statement", expression);
                throw new Exception("Error handler failed to quit application");
            }
        }

        public override IfStatement VisitIfStatement([NotNull] IfStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var ifExpression = VisitExpression(context.expression());
            var ifBody = VisitCompoundStatement(context.compoundStatement());

            var elifExpressions = context.elifPart()
                .Select(c => VisitExpression(c.expression()))
                .ToList();
            var elifBodies = context.elifPart()
                .Select(c => VisitCompoundStatement(c.compoundStatement()))
                .ToList();

            CompoundStatement? elseBody = null;
            if (context.elsePart() != null)
            {
                elseBody = VisitCompoundStatement(context.elsePart().compoundStatement());
            }

            return new IfStatement(span, ifExpression, ifBody, elifExpressions, elifBodies, elseBody);
        }


        public override WhileStatement VisitWhileStatement([NotNull] WhileStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            var body = VisitCompoundStatement(context.compoundStatement());
            return new WhileStatement(span, expression, body);
        }

        public override DoWhileStatement VisitDoWhileStatement([NotNull] DoWhileStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var body = VisitCompoundStatement(context.compoundStatement());
            var expression = VisitExpression(context.expression());
            return new DoWhileStatement(span, body, expression);
        }

        public override ForStatement VisitForStatement([NotNull] ForStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var initialStatement = VisitSimpleStatement(context.simpleStatement()[0]);
            var expression = VisitExpression(context.expression());
            var updateStatement = VisitSimpleStatement(context.simpleStatement()[1]);
            var body = VisitCompoundStatement(context.compoundStatement());
            return new ForStatement(span, initialStatement, expression, updateStatement, body);
        }

        public override BreakStatement VisitBreakStatement([NotNull] BreakStatementContext context)
        {
            var span = GetSpanOfContext(context);
            return new BreakStatement(span);
        }

        public override ContinueStatement VisitContinueStatement([NotNull] ContinueStatementContext context)
        {
            var span = GetSpanOfContext(context);
            return new ContinueStatement(span);
        }

        public override ReturnStatement VisitReturnStatement([NotNull] ReturnStatementContext context)
        {
            var span = GetSpanOfContext(context);

            if (context.OK() != null)
            {
                return new ReturnStatement(span, new IntLiteralExpression(span, 0));
            }
            
            var expression = context.expression() != null ? VisitExpression(context.expression()) : null;
            return new ReturnStatement(span, expression);
        }

        public override ExitStatement VisitExitStatement([NotNull] ExitStatementContext context)
        {
            var span = GetSpanOfContext(context);
            var expression = VisitExpression(context.expression());
            return new ExitStatement(span, expression);
        }

        public override Expression VisitExpression([NotNull] ExpressionContext context)
        {
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = GetBinaryOperator(context.GetChild(1).GetText());
                var left = VisitExpression(context.expression());
                var right = VisitOrExpression(context.orExpression());
                return new BinaryOperatorExpression(span, op, left, right);
            }
            else
            {
                return VisitOrExpression(context.orExpression());
            }
        }

        public override Expression VisitOrExpression([NotNull] OrExpressionContext context)
        {
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = GetBinaryOperator(context.GetChild(1).GetText());
                var left = VisitOrExpression(context.orExpression());
                var right = VisitAndExpression(context.andExpression());
                return new BinaryOperatorExpression(span, op, left, right);
            }
            else
            {
                return VisitAndExpression(context.andExpression());
            }
        }

        public override Expression VisitAndExpression([NotNull] AndExpressionContext context)
        {
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = GetBinaryOperator(context.GetChild(1).GetText());
                var left = VisitAndExpression(context.andExpression());
                var right = VisitComparison(context.comparison());
                return new BinaryOperatorExpression(span, op, left, right);
            }
            else
            {
                return VisitComparison(context.comparison());
            }
        }

        public override Expression VisitComparison([NotNull] ComparisonContext context)
        {
            if (context.ChildCount == 3)
            {
                var span = GetSpanOfContext(context);
                var op = GetBinaryOperator(context.GetChild(1).GetText());
                var left = VisitComparison(context.comparison());
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
                var op = GetBinaryOperator(context.GetChild(1).GetText());
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
            var span = GetSpanOfContext(context);
            if (context.callExpressionFactor != null)
            {
                var factor = VisitFactor(context.callExpressionFactor);
                var args = context.argumentList()?
                    .expression()
                    .Select(VisitExpression)
                    .ToList();
                return new CallExpression(span, factor, args ?? new List<Expression>());
            }
            else if (context.fieldAccessExpressionFactor != null)
            {
                var factor = VisitFactor(context.fieldAccessExpressionFactor);
                var field = VisitId(context.id().First());
                return new FieldAccessExpression(span, factor, field);
            }
            else if (context.fieldCallExpressionFactor != null)
            {
                var factor = VisitFactor(context.fieldCallExpressionFactor);
                var field = VisitId(context.id().First());
                var args = context.argumentList()?
                    .expression()
                    .Select(VisitExpression)
                    .ToList();
                return new FieldCallExpression(span, new FieldAccessExpression(span, factor, field), args ?? new List<Expression>());
            }
            else if (context.arrayIndexExpressionFactor != null)
            {
                var factor = VisitFactor(context.arrayIndexExpressionFactor);
                var index = VisitExpression(context.expression());
                return new ArrayIndexExpression(span, factor, index);
            }
            else if (context.expression() != null)
            {
                return VisitExpression(context.expression());
            }

            var ast = base.VisitFactor(context);
            if (ast is Expression expression)
            {
                return expression;
            }

            throw new Exception($"Tried to parse {ast.GetType()} as an expression, something is wrong with the compiler");
        }

        public override UnaryOperatorExpression VisitUnaryExpression([NotNull] UnaryExpressionContext context)
        {
            var span = GetSpanOfContext(context);
            var op = GetUnaryOperator(context.GetChild(0).GetText());
            var expression = VisitFactor(context.factor());
            return new UnaryOperatorExpression(span, op, expression);
        }

        public override ReadExpression VisitReadExpression([NotNull] ReadExpressionContext context)
        {
            var span = GetSpanOfContext(context);
            return new ReadExpression(span);
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

        public override ByteLiteralExpression VisitByteLiteral([NotNull] ByteLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            if (context.ASCII_CHAR() != null)
            {
                var text = context.ASCII_CHAR().GetText();
                var c = text[1];
                if (!char.IsAscii(c))
                {
                    throw new Exception($"Parse error: {text} is an invalid byte");
                }

                var value = Convert.ToByte(c);
                return new ByteLiteralExpression(span, value);
            }
            else if (context.ESCAPED_ASCII_CHAR() != null)
            {
                var text = context.ESCAPED_ASCII_CHAR().GetText();
                var value = ParseEscapedByte(text.Substring(1, 2));
                return new ByteLiteralExpression(span, value);
            }
            else if (context.INT() != null)
            {
                var text = context.INT().GetText();
                if (byte.TryParse(text, out var value))
                {
                    return new ByteLiteralExpression(span, value);
                }

                throw new Exception($"Parse error: {text} is an invalid byte");
            }

            throw new Exception($"Parse error: {context.GetText()} is an invalid byte");
        }

        public override FloatLiteralExpression VisitFloatLiteral([NotNull] FloatLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            if (context.FLOAT() != null)
            {
                var text = context.FLOAT().GetText();
                if (float.TryParse(text, out var value))
                {
                    return new FloatLiteralExpression(span, value);
                }
            }
            else if (context.INT() != null) 
            {
                var text = context.FLOAT().GetText();
                if (int.TryParse(text, out var value))
                {
                    return new FloatLiteralExpression(span, value);
                }
            }
            
            throw new Exception($"Tried to parse {context.GetText()} as a float, something is wrong with the compiler");
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

        public override IdExpression VisitIdExpression([NotNull] IdExpressionContext context)
        {
            var span = GetSpanOfContext(context);
            var id = VisitId(context.id());
            return new IdExpression(span, id);
        }

        public override ComplexLiteralExpression VisitComplexLiteral([NotNull] ComplexLiteralContext context)
        {
            var ast = base.VisitComplexLiteral(context);
            if (ast is ComplexLiteralExpression expression)
            {
                return expression;
            }

            throw new Exception($"Tried to parse {context.GetText()} as complex literal");
        }

        public override ArrayLiteralExpression VisitArrayLiteral([NotNull] ArrayLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            var elements = context.expression()
                .Select(e => new ArrayLiteralElement(span, VisitExpression(e)))
                .ToList();
            return new ArrayLiteralExpression(span, elements);
        }

        public override ArrayLiteralExpression VisitStringLiteral([NotNull] StringLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            var elements = new List<ArrayLiteralElement>();

            var escapedByte = string.Empty;
            var text = context.STRING().GetText();
            var stringText = text[1..^1];
            for (var i = 0; i < stringText.Length; i++)
            {
                var c = stringText[i];
                if (escapedByte.Length == 1)
                {
                    escapedByte += c;
                    var value = ParseEscapedByte(escapedByte);
                    var element = new ArrayLiteralElement(span, new ByteLiteralExpression(span, value));
                    elements.Add(element);
                    escapedByte = string.Empty;
                }
                else if (c == '\\')
                {
                    escapedByte += c;
                }
                else
                {
                    if (!char.IsAscii(c))
                    {
                        throw new Exception($"Parse error: {c} is an invalid byte");
                    }

                    var value = Convert.ToByte(c);
                    var element = new ArrayLiteralElement(span, new ByteLiteralExpression(span, value));
                    elements.Add(element);
                }
            }

            //elements.Add(new ArrayLiteralElement(span, new ByteLiteralExpression(span, 0)));
            return new ArrayLiteralExpression(span, elements);
        }

        public override StructLiteralExpression VisitStructLiteral([NotNull] StructLiteralContext context)
        {
            var span = GetSpanOfContext(context);
            var fields = context.structLiteralField()
                .Select(VisitStructLiteralField)
                .ToList();
            return new StructLiteralExpression(span, fields);
        }

        public override StructLiteralField VisitStructLiteralField([NotNull] StructLiteralFieldContext context)
        {
            var span = GetSpanOfContext(context);
            var id = VisitId(context.id());
            var expression = VisitExpression(context.expression());
            return new StructLiteralField(span, id, expression);
        }

        public override IdNode VisitId([NotNull] IdContext context)
        {
            var span = GetSpanOfContext(context);
            return new IdNode(span, context.GetText());
        }

        private static BinaryOperator GetBinaryOperator(string op)
        {
            return op switch
            {
                "==" => BinaryOperator.EqualTo,
                "!=" => BinaryOperator.NotEqualTo,
                "<" => BinaryOperator.LessThan,
                "<=" => BinaryOperator.LessThanEqualTo,
                ">" => BinaryOperator.GreaterThan,
                ">=" => BinaryOperator.GreaterThanEqualTo,
                "+" => BinaryOperator.Plus,
                "-" => BinaryOperator.Minus,
                "*" => BinaryOperator.Times,
                "/" => BinaryOperator.DividedBy,
                "%" => BinaryOperator.Modulo,
                "and" => BinaryOperator.And,
                "or" => BinaryOperator.Or,
                _ => throw new NotImplementedException()
            };
        }

        private static UnaryOperator GetUnaryOperator(string op)
        {
            return op switch
            {
                "-" => UnaryOperator.Minus,
                "&" => UnaryOperator.AddressOf,
                "*" => UnaryOperator.Dereference,
                "not" => UnaryOperator.Not,
                _ => throw new NotImplementedException()
            };
        }

        private static AssignmentOperator GetAssignmentOperator(string op)
        {
            return op switch
            {
                "=" => AssignmentOperator.Equals,
                "+=" => AssignmentOperator.PlusEquals,
                "-=" => AssignmentOperator.MinusEquals,
                "*=" => AssignmentOperator.TimesEquals,
                "/=" => AssignmentOperator.DividedByEquals,
                "%=" => AssignmentOperator.ModuloEquals,
                _ => throw new NotImplementedException()
            };
        }

        private static IncrementOperator GetIncrementOperator(string op)
        {
            return op switch
            {
                "++" => IncrementOperator.PlusPlus,
                "--" => IncrementOperator.MinusMinus,
                _ => throw new NotImplementedException()
            };
        }

        private static byte ParseEscapedByte(string escaped)
        {
            return escaped switch
            {
                "\\a" => Convert.ToByte('\a'),
                "\\b" => Convert.ToByte('\b'),
                "\\f" => Convert.ToByte('\f'),
                "\\n" => Convert.ToByte('\n'),
                "\\r" => Convert.ToByte('\r'),
                "\\t" => Convert.ToByte('\t'),
                "\\v" => Convert.ToByte('\v'),
                "\\\\" => Convert.ToByte('\\'),
                "\\\'" => Convert.ToByte('\''),
                "\\\"" => Convert.ToByte('\"'),
                "\\0" => Convert.ToByte('\0'),
                _ => throw new Exception($"{escaped} is not recognized as an escape sequence.")
            };
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

