﻿using System.Collections.Generic;
using EVESharp.EVE.Packets.Complex;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Notifications.Client.Corporations
{
    public class OnCorporationApplicationChanged : ClientNotification
    {
        private const string NOTIFICATION_NAME = "OnCorporationApplicationChanged";
        
        public int CorporationID { get; init; }
        public int ApplicantID { get; init; }
        private PyDictionary<PyString,PyTuple> Changes { get; init; }
        
        public OnCorporationApplicationChanged(int corporationID, int applicantID) : base(NOTIFICATION_NAME)
        {
            this.CorporationID = corporationID;
            this.ApplicantID = applicantID;
            this.Changes = new PyDictionary<PyString, PyTuple>();
        }

        public OnCorporationApplicationChanged AddValue(string columnName, PyDataType oldValue, PyDataType newValue)
        {
            this.Changes[columnName] = new PyTuple(2)
            {
                [0] = oldValue,
                [1] = newValue
            };

            return this;
        }

        public override List<PyDataType> GetElements()
        {
            return new List<PyDataType>()
            {
                this.ApplicantID,
                this.CorporationID,
                this.Changes
            };
        }
    }
}