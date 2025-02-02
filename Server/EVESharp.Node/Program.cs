﻿/*
    ------------------------------------------------------------------------------------
    LICENSE:
    ------------------------------------------------------------------------------------
    This file is part of EVE#: The EVE Online Server Emulator
    Copyright 2021 - EVE# Team
    ------------------------------------------------------------------------------------
    This program is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by the Free Software
    Foundation; either version 2 of the License, or (at your option) any later
    version.

    This program is distributed in the hope that it will be useful, but WITHOUT
    ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
    FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License along with
    this program; if not, write to the Free Software Foundation, Inc., 59 Temple
    Place - Suite 330, Boston, MA 02111-1307, USA, or go to
    http://www.gnu.org/copyleft/lesser.txt.
    ------------------------------------------------------------------------------------
    Creator: Almamu
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using EVESharp.Common.Database;
using EVESharp.Common.Logging;
using EVESharp.Common.Logging.Streams;
using EVESharp.Node.Agents;
using EVESharp.Node.Chat;
using EVESharp.Node.Configuration;
using EVESharp.Node.Database;
using EVESharp.Node.Dogma;
using EVESharp.Node.Inventory;
using EVESharp.Node.Market;
using EVESharp.Node.Network;
using EVESharp.Node.Services.Account;
using EVESharp.Node.Services.Alliances;
using EVESharp.Node.Services.CacheSvc;
using EVESharp.Node.Services.Characters;
using EVESharp.Node.Services.Chat;
using EVESharp.Node.Services.Config;
using EVESharp.Node.Services.Contracts;
using EVESharp.Node.Services.Corporations;
using EVESharp.Node.Services.Data;
using EVESharp.Node.Services.Dogma;
using EVESharp.Node.Services.Inventory;
using EVESharp.Node.Services.Market;
using EVESharp.Node.Services.Navigation;
using EVESharp.Node.Services.Network;
using EVESharp.Node.Services.Stations;
using EVESharp.Node.Services.Tutorial;
using EVESharp.Node.Services.War;
using EVESharp.Node.Services;
using SimpleInjector;
using Container = SimpleInjector.Container;

namespace EVESharp.Node
{
    class Program
    {
        static async Task InitializeItemFactory(Channel logChannel, Container dependencies)
        {
            await Task.Run(() =>
            {
                logChannel.Info("Initializing item factory");
                dependencies.GetInstance<ItemFactory>().Init();
                logChannel.Debug("Item Factory Initialized");
            });
        }

        static async Task InitializeCache(Channel logChannel, Container dependencies)
        {
            await Task.Run(() =>
            {
                logChannel.Info("Initializing cache");
                CacheStorage cacheStorage = dependencies.GetInstance<CacheStorage>();
                // prime bulk data
                cacheStorage.Load(
                    CacheStorage.LoginCacheTable,
                    CacheStorage.LoginCacheQueries,
                    CacheStorage.LoginCacheTypes
                );
                // prime character creation cache
                cacheStorage.Load(
                    CacheStorage.CreateCharacterCacheTable,
                    CacheStorage.CreateCharacterCacheQueries,
                    CacheStorage.CreateCharacterCacheTypes
                );
                // prime character appearance cache
                cacheStorage.Load(
                    CacheStorage.CharacterAppearanceCacheTable,
                    CacheStorage.CharacterAppearanceCacheQueries,
                    CacheStorage.CharacterAppearanceCacheTypes
                );
                logChannel.Info("Cache Initialized");
            });
        }
        
        static void Main(string[] argv)
        {
            Channel logChannel = null;
            Logger log = null;
            Thread logFlushing = null;
            
            try
            {
                // create the dependency injector container
                Container dependencies = new Container();
                
                // register basic dependencies first
                dependencies.Register<Logger>(Lifestyle.Singleton);
                dependencies.Register<DatabaseConnection>(Lifestyle.Singleton);
                dependencies.Register<ClientManager>(Lifestyle.Singleton);
                dependencies.Register<NodeContainer>(Lifestyle.Singleton);
                dependencies.Register<CacheStorage>(Lifestyle.Singleton);
                dependencies.Register<ItemManager>(Lifestyle.Singleton);
                dependencies.Register<MetaInventoryManager>(Lifestyle.Singleton);
                dependencies.Register<AttributeManager>(Lifestyle.Singleton);
                dependencies.Register<TypeManager>(Lifestyle.Singleton);
                dependencies.Register<CategoryManager>(Lifestyle.Singleton);
                dependencies.Register<GroupManager>(Lifestyle.Singleton);
                dependencies.Register<StationManager>(Lifestyle.Singleton);
                dependencies.Register<ItemFactory>(Lifestyle.Singleton);
                dependencies.Register<TimerManager>(Lifestyle.Singleton);
                dependencies.Register<SystemManager>(Lifestyle.Singleton);
                dependencies.Register<ServiceManager>(Lifestyle.Singleton);
                dependencies.Register<BoundServiceManager>(Lifestyle.Singleton);
                dependencies.Register<ClusterConnection>(Lifestyle.Singleton);
                dependencies.Register<NotificationManager>(Lifestyle.Singleton);
                dependencies.Register<MachoNet>(Lifestyle.Singleton);
                dependencies.Register<ExpressionManager>(Lifestyle.Singleton);
                dependencies.Register<WalletManager>(Lifestyle.Singleton);
                dependencies.Register<MailManager>(Lifestyle.Singleton);
                dependencies.Register<AgentManager>(Lifestyle.Singleton);
                dependencies.Register<AncestryManager>(Lifestyle.Singleton);

                // register the database accessors dependencies
                dependencies.Register<AccountDB>(Lifestyle.Singleton);
                dependencies.Register<CertificatesDB>(Lifestyle.Singleton);
                dependencies.Register<CharacterDB>(Lifestyle.Singleton);
                dependencies.Register<ChatDB>(Lifestyle.Singleton);
                dependencies.Register<ConfigDB>(Lifestyle.Singleton);
                dependencies.Register<ContractDB>(Lifestyle.Singleton);
                dependencies.Register<CorporationDB>(Lifestyle.Singleton);
                dependencies.Register<GeneralDB>(Lifestyle.Singleton);
                dependencies.Register<ItemDB>(Lifestyle.Singleton);
                dependencies.Register<MarketDB>(Lifestyle.Singleton);
                dependencies.Register<MessagesDB>(Lifestyle.Singleton);
                dependencies.Register<SkillDB>(Lifestyle.Singleton);
                dependencies.Register<StandingDB>(Lifestyle.Singleton);
                dependencies.Register<StationDB>(Lifestyle.Singleton);
                dependencies.Register<LookupDB>(Lifestyle.Singleton);
                dependencies.Register<InsuranceDB>(Lifestyle.Singleton);
                dependencies.Register<SolarSystemDB>(Lifestyle.Singleton);
                dependencies.Register<DogmaDB>(Lifestyle.Singleton);
                dependencies.Register<RepairDB>(Lifestyle.Singleton);
                dependencies.Register<ReprocessingDB>(Lifestyle.Singleton);
                dependencies.Register<RAMDB>(Lifestyle.Singleton);
                dependencies.Register<FactoryDB>(Lifestyle.Singleton);
                dependencies.Register<TutorialsDB>(Lifestyle.Singleton);
                dependencies.Register<BillsDB>(Lifestyle.Singleton);

                // register all the services
                dependencies.Register<account>(Lifestyle.Singleton);
                dependencies.Register<machoNet>(Lifestyle.Singleton);
                dependencies.Register<objectCaching>(Lifestyle.Singleton);
                dependencies.Register<alert>(Lifestyle.Singleton);
                dependencies.Register<authentication>(Lifestyle.Singleton);
                dependencies.Register<character>(Lifestyle.Singleton);
                dependencies.Register<userSvc>(Lifestyle.Singleton);
                dependencies.Register<charmgr>(Lifestyle.Singleton);
                dependencies.Register<config>(Lifestyle.Singleton);
                dependencies.Register<dogmaIM>(Lifestyle.Singleton);
                dependencies.Register<invbroker>(Lifestyle.Singleton);
                dependencies.Register<warRegistry>(Lifestyle.Singleton);
                dependencies.Register<station>(Lifestyle.Singleton);
                dependencies.Register<map>(Lifestyle.Singleton);
                dependencies.Register<skillMgr>(Lifestyle.Singleton);
                dependencies.Register<contractMgr>(Lifestyle.Singleton);
                dependencies.Register<corpStationMgr>(Lifestyle.Singleton);
                dependencies.Register<bookmark>(Lifestyle.Singleton);
                dependencies.Register<LSC>(Lifestyle.Singleton);
                dependencies.Register<onlineStatus>(Lifestyle.Singleton);
                dependencies.Register<billMgr>(Lifestyle.Singleton);
                dependencies.Register<facWarMgr>(Lifestyle.Singleton);
                dependencies.Register<corporationSvc>(Lifestyle.Singleton);
                dependencies.Register<clientStatsMgr>(Lifestyle.Singleton);
                dependencies.Register<voiceMgr>(Lifestyle.Singleton);
                dependencies.Register<standing2>(Lifestyle.Singleton);
                dependencies.Register<tutorialSvc>(Lifestyle.Singleton);
                dependencies.Register<agentMgr>(Lifestyle.Singleton);
                dependencies.Register<corpRegistry>(Lifestyle.Singleton);
                dependencies.Register<marketProxy>(Lifestyle.Singleton);
                dependencies.Register<stationSvc>(Lifestyle.Singleton);
                dependencies.Register<certificateMgr>(Lifestyle.Singleton);
                dependencies.Register<jumpCloneSvc>(Lifestyle.Singleton);
                dependencies.Register<LPSvc>(Lifestyle.Singleton);
                dependencies.Register<lookupSvc>(Lifestyle.Singleton);
                dependencies.Register<insuranceSvc>(Lifestyle.Singleton);
                dependencies.Register<slash>(Lifestyle.Singleton);
                dependencies.Register<ship>(Lifestyle.Singleton);
                dependencies.Register<corpmgr>(Lifestyle.Singleton);
                dependencies.Register<repairSvc>(Lifestyle.Singleton);
                dependencies.Register<reprocessingSvc>(Lifestyle.Singleton);
                dependencies.Register<ramProxy>(Lifestyle.Singleton);
                dependencies.Register<factory>(Lifestyle.Singleton);
                dependencies.Register<petitioner>(Lifestyle.Singleton);
                dependencies.Register<allianceRegistry>(Lifestyle.Singleton);
                
                dependencies.RegisterInstance(General.LoadFromFile("configuration.conf", dependencies));
                // disable auto-verification on the container as it triggers creation of instances before they're needed
                dependencies.Options.EnableAutoVerification = false;
                
                General configuration = dependencies.GetInstance<General>();

                log = dependencies.GetInstance<Logger>();

                logChannel = log.CreateLogChannel("main");
                // add log streams
                log.AddLogStream(new ConsoleLogStream());

                if (configuration.LogLite.Enabled == true)
                    log.AddLogStream(new LogLiteStream("Node", log, configuration.LogLite));
                if (configuration.FileLog.Enabled == true)
                    log.AddLogStream(new FileLogStream(configuration.FileLog));

                // run a thread for log flushing
                logFlushing = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            log.Flush();
                            Thread.Sleep(1);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                });
                logFlushing.Start();

                logChannel.Info("Initializing EVESharp Node");
                logChannel.Fatal("Initializing EVESharp Node");
                logChannel.Error("Initializing EVESharp Node");
                logChannel.Warning("Initializing EVESharp Node");
                logChannel.Debug("Initializing EVESharp Node");
                logChannel.Trace("Initializing EVESharp Node");
                
                // connect to the database
                dependencies.GetInstance<DatabaseConnection>();
                // sDatabase.Query("SET global max_allowed_packet=1073741824");
                
                // create the node container
                dependencies.GetInstance<NodeContainer>();
                
                logChannel.Info("Initializing timer manager");
                dependencies.GetInstance<TimerManager>().Start();
                logChannel.Debug("Done");

                // do some parallel initialization, cache priming and static item loading can be performed in parallel
                // this makes the changes quicker
                Task cacheStorage = InitializeCache(logChannel, dependencies);
                Task itemFactory = InitializeItemFactory(logChannel, dependencies);
                
                // wait for all the tasks to be done
                Task.WaitAll(itemFactory, cacheStorage);
                
                logChannel.Info("Initializing solar system manager");
                dependencies.GetInstance<SystemManager>();
                logChannel.Debug("Done");

                dependencies.GetInstance<MachoNet>().ConnectToProxy();
                
                logChannel.Trace("Node startup done");

                while (true)
                    Thread.Sleep(1);
            }
            catch (Exception e)
            {
                if (log is null || logChannel is null)
                {
                    Console.WriteLine(e.ToString());
                }
                else
                {
                    logChannel?.Error(e.ToString());
                    logChannel?.Fatal("Node stopped...");
                    log?.Flush();
                    // stop the logging thread
                    logFlushing?.Interrupt();
                }
            }
        }
    }
}