﻿using EVESharp.EVE.Packets.Exceptions;
using EVESharp.PythonTypes.Types.Collections;

namespace EVESharp.Node.Exceptions.inventory
{
    public class ItemNotContainer : UserError
    {
        public ItemNotContainer(string itemInfo) : base("ItemNotContainer", new PyDictionary{["item"] = itemInfo})
        {
        }

        public ItemNotContainer(int itemID) : this(itemID.ToString())
        {
        }
    }
}