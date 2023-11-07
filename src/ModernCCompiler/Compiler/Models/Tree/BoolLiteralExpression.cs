﻿namespace Compiler.Models.Tree
{
    public class BoolLiteralExpression : Expression
    {
        public bool Value { get; } 

        public BoolLiteralExpression(Span span, bool value) : base(span)
        {
            Value = value;
        }
    }
}