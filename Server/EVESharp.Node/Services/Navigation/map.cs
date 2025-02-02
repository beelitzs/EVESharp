using System.Collections.Generic;
using EVESharp.Common.Services;
using EVESharp.EVE.Packets.Complex;
using EVESharp.Node.Inventory;
using EVESharp.Node.Inventory.Items.Types;
using EVESharp.Node.Network;
using EVESharp.Node.StaticData.Inventory.Station;
using EVESharp.Node.StaticData;
using EVESharp.PythonTypes.Types.Collections;
using EVESharp.PythonTypes.Types.Database;
using EVESharp.PythonTypes.Types.Primitives;

namespace EVESharp.Node.Services.Navigation
{
    public class map : IService
    {
        private enum HistoryType
        {
            PodKill = 3,
            MostDangerous = 5,
        };
        
        private ItemFactory ItemFactory { get; }
        private StationManager StationManager => this.ItemFactory.StationManager;
        private CacheStorage CacheStorage { get; }
        public map(ItemFactory itemFactory, CacheStorage cacheStorage)
        {
            this.ItemFactory = itemFactory;
            this.CacheStorage = cacheStorage;
        }

        public PyTuple GetStationExtraInfo(CallInformation call)
        {
            Rowset stations = new Rowset(new PyList<PyString>(5)
            {
                [0] = "stationID",
                [1] = "solarSystemID",
                [2] = "operationID",
                [3] = "stationTypeID",
                [4] = "ownerID"
            });
            Rowset operationServices = new Rowset(new PyList<PyString>(2)
            {
                [0] = "operationID",
                [1] = "serviceID"
            });
            Rowset services = new Rowset(new PyList<PyString>(2)
            {
                [0] = "serviceID",
                [1] = "serviceName"
            });
            
            foreach ((int _, Station station) in this.ItemFactory.Stations)
                stations.Rows.Add(new PyList(5)
                {
                    [0] = station.ID,
                    [1] = station.LocationID,
                    [2] = station.Operations.OperationID,
                    [3] = station.StationType.ID,
                    [4] = station.OwnerID
                });

            foreach ((int _, Operation operation) in this.StationManager.Operations)
                foreach (int serviceID in operation.Services)
                    operationServices.Rows.Add(new PyList(2)
                    {
                        [0] = operation.OperationID,
                        [1] = serviceID
                    });

            foreach ((int serviceID, string name) in this.StationManager.Services)
                services.Rows.Add(new PyList(2)
                {
                    [0] = serviceID,
                    [1] = name
                });
            
            return new PyTuple(3)
            {
                [0] = stations,
                [1] = operationServices,
                [2] = services
            };
        }

        public PyDataType GetSolarSystemPseudoSecurities(CallInformation call)
        {
            this.CacheStorage.Load(
                "map",
                "GetSolarSystemPseudoSecurities",
                "SELECT solarSystemID, security FROM mapSolarSystems",
                CacheStorage.CacheObjectType.Rowset
            );

            PyDataType cacheHint = this.CacheStorage.GetHint("map", "GetSolarSystemPseudoSecurities");

            return CachedMethodCallResult.FromCacheHint(cacheHint);
        }

        // TODO: PROPERLY IMPLEMENT THIS ONE
        public PyDataType GetMyExtraMapInfoAgents(CallInformation call)
        {
            return new Rowset(
                new PyList<PyString>(2)
                {
                    [0] = "fromID",
                    [1] = "rank"
                }
            );
        }

        public PyDictionary GetStuckSystems(CallInformation call)
        {
            return new PyDictionary();
        }

        public PyDataType GetHistory(PyInteger historyType, PyInteger maximumJumpDistance, CallInformation call)
        {
            // TODO: IMPLEMENT THIS
            // this should return list of keyvals with information about the closest systems within the maximumJumpDistance
            // depending on the first argument the data returned will be different, although the fields have the same name
            
            switch (historyType.Value)
            {
                case (int) HistoryType.MostDangerous:
                    break;
                case (int) HistoryType.PodKill:
                    break;
            }

            return new PyList(0);
        }
    }
}