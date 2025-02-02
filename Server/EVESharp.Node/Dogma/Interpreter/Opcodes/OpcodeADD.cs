﻿using System.IO;
using EVESharp.Node.Dogma.Exception;

namespace EVESharp.Node.Dogma.Interpreter.Opcodes
{
    public class OpcodeADD : OpcodeWithDoubleOutput
    {
        public OpcodeWithDoubleOutput LeftSide { get; private set; }
        public OpcodeWithDoubleOutput RightSide { get; private set; }
        
        public OpcodeADD(Interpreter interpreter) : base(interpreter)
        {
        }

        public override Opcode LoadOpcode(BinaryReader reader)
        {
            // get the right side of the operation
            Opcode leftSide = this.Interpreter.Step(reader);
            Opcode rightSide = this.Interpreter.Step(reader);

            if (leftSide is not OpcodeWithDoubleOutput left)
                throw new DogmaMachineException("The left side of an ADD operand must return a double value");
            if (rightSide is not OpcodeWithDoubleOutput right)
                throw new DogmaMachineException("The right side of an ADD operand must return a double value");

            this.LeftSide = left;
            this.RightSide = right;
            
            return this;
        }

        public override double Execute()
        {
            return this.LeftSide.Execute() + this.RightSide.Execute();
        }
    }
}