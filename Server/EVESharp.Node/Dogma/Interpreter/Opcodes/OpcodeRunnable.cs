﻿namespace EVESharp.Node.Dogma.Interpreter.Opcodes
{
    public abstract class OpcodeRunnable : Opcode
    {
        protected OpcodeRunnable(Interpreter interpreter) : base(interpreter)
        {
        }

        public abstract void Execute();
    }
}