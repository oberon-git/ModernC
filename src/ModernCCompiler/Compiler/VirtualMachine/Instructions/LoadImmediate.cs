﻿using static Compiler.Utils.FloatConvert;

namespace Compiler.VirtualMachine.Instructions
{
    public class LoadImmediate : IInstruction
    {
        private readonly string _dst;
        private readonly int _val;
        private readonly float? _oval = null;

        public LoadImmediate(string dst, int val)
        {
            _dst = dst;
            _val = val;
        }

        public LoadImmediate(string dst, float val)
        {
            _dst = dst;
            _val = ToInt(val);
            _oval = val;
        }

        public void Execute(Memory memory, Registers registers, Dictionary<string, int> labels, TextReader inStream, TextWriter outStream)
        {
            registers[_dst] = _val;
        }

        public string ToCode()
        {
            if (_oval == null)
            {
                return $"li {_dst} {_val}";
            }
            else
            {
                return $"li {_dst} {_oval}";
            }
        }
    }
}
