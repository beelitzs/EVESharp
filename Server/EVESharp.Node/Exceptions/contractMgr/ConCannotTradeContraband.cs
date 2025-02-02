﻿using EVESharp.EVE.Packets.Exceptions;
using EVESharp.Node.StaticData.Inventory;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Exceptions.contractMgr
{
    public class ConCannotTradeContraband : UserError
    {
        public ConCannotTradeContraband(Type example) : base("ConCannotTradeContraband", new PyDictionary {["example"] = FormatTypeIDAsName(example.ID)})
        {
        }
    }
}