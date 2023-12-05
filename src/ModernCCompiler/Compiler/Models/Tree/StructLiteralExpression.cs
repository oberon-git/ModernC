﻿using Compiler.ErrorHandling;
using Compiler.Models.NameResolution;
using Compiler.Models.NameResolution.Types;

namespace Compiler.Models.Tree
{
    public class StructLiteralExpression : ComplexLiteralExpression
    {
        public IList<StructLiteralField> Fields { get; }
        public StructDefinition? StructDefinition { get; private set; }
        public int Offset { get; set; }

        public StructLiteralExpression(Span span, IList<StructLiteralField> fields) : base(span)
        {
            Fields = fields;
        }

        public SemanticType MapDefaultExpressionsFromDefinition(StructType type, StructDefinition definition)
        {
            
            StructDefinition = definition;
            foreach (var fieldDefinition in definition.Fields)
            {
                var field = Fields.Where(f => f.Id.Value == fieldDefinition.Id.Value).FirstOrDefault();
                if (field == null)
                {
                    if (fieldDefinition.DefaultExpression == null)
                    {
                        ErrorHandler.Throw("Struct fields without default expressions must be initialized in a struct literal", this);
                    }
                    else
                    {
                        field = fieldDefinition.CreateDefaultLiteralField(Span);
                        Fields.Add(field);
                    }
                }
            }

            return type;
        }

        public void MapOffsetsFromDefinition()
        {
            foreach (var fieldDefinition in StructDefinition!.Fields)
            {
                var field = Fields.Where(f => f.Id.Value == fieldDefinition.Id.Value).FirstOrDefault();
                field!.Offset = fieldDefinition.Offset;
            }
        }

        public override Expression Copy(Span span)
        {
            var structLiteral = new StructLiteralExpression(span, Fields);
            structLiteral.StructDefinition = StructDefinition;
            return structLiteral;
        }
    }
}
