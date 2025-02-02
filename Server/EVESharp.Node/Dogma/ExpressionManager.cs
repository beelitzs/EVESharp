﻿using System.Collections.Generic;
using EVESharp.Common.Logging;
using EVESharp.Node.Database;
using EVESharp.Node.Inventory.Items.Dogma;

namespace EVESharp.Node.Dogma
{
    public class ExpressionManager
    {
        private Channel Log { get; }
        private DogmaDB DB { get; }
        private Dictionary<int, Expression> Expressions { get; }
        
        public ExpressionManager(DogmaDB db, Logger logger)
        {
            this.DB = db;
            this.Log = logger.CreateLogChannel("DogmaExpressionManager");
            this.Expressions = this.DB.LoadDogmaExpressions();

            Log.Debug($"Loaded {this.Expressions.Count} expressions for Dogma");
        }

        public Expression this[int index] => this.Expressions[index];
    }
}