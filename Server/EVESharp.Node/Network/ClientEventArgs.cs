﻿using System;

namespace EVESharp.Node.Network
{
    public class ClientEventArgs : EventArgs
    {
        public Client Client { get; init; }
    }
}