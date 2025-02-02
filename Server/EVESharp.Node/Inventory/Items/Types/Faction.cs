using System;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Database;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Inventory.Items.Types
{
    public class Faction : ItemEntity
    {
        public Faction(ItemEntity @from, string description, int raceIDs, int solarSystemId, int corporationId,
            double sizeFactor, int stationCount, int stationSystemCount, int militiaCorporationId) : base(@from)
        {
            this.mDescription = description;
            this.mRaceIDs = raceIDs;
            this.mSolarSystemID = solarSystemId;
            this.mCorporationID = corporationId;
            this.mSizeFactor = sizeFactor;
            this.mStationCount = stationCount;
            this.mStationSystemCount = stationSystemCount;
            this.mMilitiaCorporationID = militiaCorporationId;
        }

        private string mDescription;
        private int mRaceIDs;
        private int mSolarSystemID;
        private int mCorporationID;
        private double mSizeFactor;
        private int mStationCount;
        private int mStationSystemCount;
        private int mMilitiaCorporationID;

        public string Description => mDescription;
        public int RaceIDs => mRaceIDs;
        public int SolarSystemId => mSolarSystemID;
        public int CorporationId => mCorporationID;
        public double SizeFactor => mSizeFactor;
        public int StationCount => mStationCount;
        public int StationSystemCount => mStationSystemCount;
        public int MilitiaCorporationId => mMilitiaCorporationID;

        protected override void SaveToDB()
        {
            // factions cannot be updated
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            throw new NotImplementedException("Stations cannot be destroyed as they're regarded as static data!");
        }

        public PyDataType GetKeyVal()
        {
            return KeyVal.FromDictionary(
                new PyDictionary()
                {
                    ["factionID"] = this.ID,
                    ["factionName"] = this.Name,
                    ["description"] = this.Description,
                    ["solarSystemID"] = this.SolarSystemId,
                    ["corporationID"] = this.CorporationId,
                    ["militiaID"] = this.MilitiaCorporationId
                }
            );
        }
    }
}