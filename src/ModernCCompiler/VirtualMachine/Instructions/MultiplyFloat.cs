﻿using static VirtualMachine.Utils.FloatConvert;

namespace VirtualMachine.Instructions
{
    public class MultiplyFloat : IInstruction
    {
        private readonly string _dst;
        private readonly string _left;
        private readonly string _right;

        public MultiplyFloat(string dst, string left, string right)
        {
            _dst = dst;
            _left = left;
            _right = right;
        }

        public void Execute(Memory memory, Registers registers, Dictionary<string, int> labels, TextReader inStream, TextWriter outStream)
        {
            registers[_dst] = ToInt(ToFloat(registers[_left]) * ToFloat(registers[_right]));
        }

        public string ToCode()
        {
            return $"mulf {_dst} {_left} {_right}";
        }
    }
}
