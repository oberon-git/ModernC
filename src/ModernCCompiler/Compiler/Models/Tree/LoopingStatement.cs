﻿namespace Compiler.Models.Tree
{
    public abstract class LoopingStatement : Statement
    {
        private int? _labelId = null;
        public int LabelId
        {
            protected get
            {
                if (!_labelId.HasValue) 
                {
                    throw new Exception("_labelId was null");
                }

                return _labelId.Value;
            }
            set
            {
                _labelId = value;
            }
        }

        protected LoopingStatement(Span span) : base(span)
        {
        }

        public abstract string GetLoopLabel();
        public abstract string GetExitLabel();
    }
}