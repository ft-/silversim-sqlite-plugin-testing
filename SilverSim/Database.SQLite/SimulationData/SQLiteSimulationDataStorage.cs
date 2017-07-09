// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System;

namespace SilverSim.Database.SQLite.SimulationData
{
    [Description("SQLite Simulation Data Backend")]
    [PluginName("SimulationData")]
    public sealed partial class SQLiteSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin, IQueueStatsAccess
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE SIMULATION STORAGE");
        private readonly string m_ConnectionString;

        #region Constructor
        public SQLiteSimulationDataStorage(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
            m_WhiteListStorage = new SQLiteSimulationDataParcelAccessListStorage(m_ConnectionString, "parcelaccesswhitelist");
            m_BlackListStorage = new SQLiteSimulationDataParcelAccessListStorage(m_ConnectionString, "parcelaccessblacklist");
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Properties
        public override ISimulationDataPhysicsConvexStorageInterface PhysicsConvexShapes => this;

        public override ISimulationDataEnvControllerStorageInterface EnvironmentController => this;

        public override ISimulationDataLightShareStorageInterface LightShare => this;

        public override ISimulationDataSpawnPointStorageInterface Spawnpoints => this;

        public override ISimulationDataEnvSettingsStorageInterface EnvironmentSettings => this;

        public override ISimulationDataObjectStorageInterface Objects => this;

        public override ISimulationDataParcelStorageInterface Parcels => this;
        public override ISimulationDataScriptStateStorageInterface ScriptStates => this;

        public override ISimulationDataTerrainStorageInterface Terrains => this;

        public override ISimulationDataRegionSettingsStorageInterface RegionSettings => this;

        public override ISimulationDataRegionExperiencesStorageInterface RegionExperiences => this;

        public override ISimulationDataRegionTrustedExperiencesStorageInterface TrustedExperiences => this;
        #endregion

        #region IDBServiceInterface
        public void VerifyConnection()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }
        #endregion

        private static readonly string[] Tables = new string[]
        {
            "primitems",
            "prims",
            "objects",
            "scriptstates",
            "terrains",
            "parcels",
            "environmentsettings",
            "environmentcontroller",
            "regionsettings",
            "lightshare",
            "spawnpoints",
            "regionexperiences",
            "regiontrustedexperiences",
            "parcelexperiences"
        };

        public override void RemoveRegion(UUID regionID)
        {
            foreach (string table in Tables)
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM " + table + " WHERE RegionID = @regionid", connection))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        IList<QueueStatAccessor> IQueueStatsAccess.QueueStats
        {
            get
            {
                var statFuncs = new List<QueueStatAccessor>();
                foreach (SQLiteTerrainListener terListener in m_TerrainListenerThreads)
                {
                    statFuncs.Add(new QueueStatAccessor("TerrainStore." + terListener.RegionID.ToString(), terListener.GetStats));
                }

                foreach (SQLiteSceneListener sceneListener in m_SceneListenerThreads)
                {
                    statFuncs.Add(new QueueStatAccessor("SceneStore." + sceneListener.RegionID.ToString(), sceneListener.GetStats));
                }

                return statFuncs;
            }
        }
    }
}
