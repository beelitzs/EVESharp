﻿using System.Collections.Generic;
using EVESharp.EVE.Packets.Complex;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Notifications.Client.Station
{
    public class OnCharNoLongerInStation : ClientNotification
    {
        private const string NOTIFICATION_NAME = "OnCharNoLongerInStation";
        
        public int? CharacterID { get; init; }
        public int? CorporationID { get; init; }
        public int? AllianceID { get; init; }
        public int? WarFactionID { get; init; }
        
        public OnCharNoLongerInStation(Network.Client client) : base(NOTIFICATION_NAME)
        {
            this.CharacterID = client.CharacterID;
            this.CorporationID = client.CorporationID;
            this.AllianceID = client.AllianceID;
            this.WarFactionID = client.WarFactionID;
        }

        public override List<PyDataType> GetElements()
        {
            return new List<PyDataType>()
            {
                new PyTuple(4)
                {
                    [0] = this.CharacterID,
                    [1] = this.CorporationID,
                    [2] = this.AllianceID,
                    [3] = this.WarFactionID
                }
            };
        }
    }
}