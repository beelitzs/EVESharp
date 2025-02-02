﻿using EVESharp.EVE.Packets.Exceptions;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Exceptions.marketProxy
{
    public class MktCantSellItem2 : UserError
    {
        public MktCantSellItem2(long jumps, long maximumJumps) : base("MktCantSellItem2",
            new PyDictionary
            {
                ["numJumps"] = jumps, ["jumpText"] = ("jump" + (jumps == 1 ? "" : "s")),
                ["numLimit"] = maximumJumps, ["jumpText2"] = ("jump" + (maximumJumps == 1 ? "" : "s"))
            })
        {
        }
    }
}