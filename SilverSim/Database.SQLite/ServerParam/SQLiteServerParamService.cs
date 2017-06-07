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
using SilverSim.Database.SQLite._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.ServerParam
{
    [Description("SQLite ServerParam Backend")]
    [PluginName("ServerParams")]
    public sealed class SQLiteServerParamService : ServerParamServiceInterface, IDBServiceInterface, IPlugin, IPluginShutdown
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE SERVER PARAM SERVICE");

        #region Cache
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Cache = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(() => new RwLockedDictionary<string, string>());
        #endregion

        #region Constructor
        public SQLiteServerParamService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM serverparams", connection))
                {
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            UUID regionid = dbReader.GetUUID("regionid");
                            m_Cache[regionid].Add((string)dbReader["parametername"], (string)dbReader["parametervalue"]);
                        }
                    }
                }
            }
        }
        #endregion

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        public override List<string> this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<string, string> regParams;
                if (m_Cache.TryGetValue(regionID, out regParams))
                {
                    var list = new List<string>(regParams.Keys);
                    if (m_Cache.TryGetValue(regionID, out regParams) && regionID != UUID.Zero)
                    {
                        foreach (string k in regParams.Keys)
                        {
                            if (!list.Exists((string p) => p == k))
                            {
                                list.Add(k);
                            }
                        }
                    }
                    return list;
                }

                return new List<string>();
            }
        }

        public override List<KeyValuePair<UUID, string>> this[string parametername]
        {
            get
            {
                var resultSet = new List<KeyValuePair<UUID, string>>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new SQLiteCommand("SELECT * FROM serverparams WHERE parametername = @parametername", connection))
                    {
                        cmd.Parameters.AddParameter("@parametername", parametername);
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                UUID regionID = dbReader.GetUUID("regionid");
                                string value = (string)dbReader["parametervalue"];
                                m_Cache[regionID][parametername] = value;
                                resultSet.Add(new KeyValuePair<UUID, string>(regionID, value));
                            }
                        }
                    }
                }
                return resultSet;
            }
        }

        public override bool TryGetValue(UUID regionID, string parameter, out string value)
        {
            if (TryGetExplicitValue(regionID, parameter, out value))
            {
                return true;
            }

            if (UUID.Zero != regionID &&
                TryGetValue(UUID.Zero, parameter, out value))
            {
                return true;
            }

            value = string.Empty;
            return false;
        }

        public override bool TryGetExplicitValue(UUID regionID, string parameter, out string value)
        {
            RwLockedDictionary<string, string> regParams;
            if (m_Cache.TryGetValue(regionID, out regParams) &&
                regParams.TryGetValue(parameter, out value))
            {
                return true;
            }

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM serverparams WHERE regionid = @regionid AND parametername = @parametername", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parametername", parameter);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            value = (string)dbReader["parametervalue"];
                            m_Cache[regionID][parameter] = value;
                            return true;
                        }
                    }
                }
            }

            value = string.Empty;
            return false;
        }

        public override bool Contains(UUID regionID, string parameter)
        {
            RwLockedDictionary<string, string> regParams;
            if (m_Cache.TryGetValue(regionID, out regParams) &&
                regParams.ContainsKey(parameter))
            {
                return true;
            }

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM serverparams WHERE regionid = @regionid AND parametername = @parametername", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parametername", parameter);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            m_Cache[regionID][parameter] = (string)dbReader["parametervalue"];
                            return true;
                        }
                    }
                }
            }

            if (UUID.Zero != regionID &&
                Contains(UUID.Zero, parameter))
            {
                return true;
            }

            return false;
        }

        protected override void Store(UUID regionID, string parameter, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(regionID, parameter);
            }
            else
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    var param = new Dictionary<string, object>
                    {
                        ["regionid"] = regionID,
                        ["parametername"] = parameter,
                        ["parametervalue"] = value
                    };
                    connection.ReplaceInto("serverparams", param);
                    m_Cache[regionID][parameter] = value;
                }
            }
        }

        public override List<KeyValuePair<UUID, string>> KnownParameters
        {
            get
            {
                var result = new List<KeyValuePair<UUID, string>>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT regionid, parametername FROM serverparams", connection))
                    {
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                result.Add(new KeyValuePair<UUID, string>(dbReader.GetUUID("regionid"), (string)dbReader["parametername"]));
                            }
                        }
                    }
                }
                return result;
            }
        }

        public override bool Remove(UUID regionID, string parameter)
        {
            bool result = false;
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM serverparams WHERE regionid = @regionid AND parametername = @parametername", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parametername", parameter);
                    if (cmd.ExecuteNonQuery() >= 1)
                    {
                        result = true;
                    }
                }
                m_Cache[regionID].Remove(parameter);
            }

            return result;
        }

        public void Shutdown()
        {
            AnyServerParamListeners.Clear();
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("serverparams"),
            new AddColumn<UUID>("regionid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("parametername") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("parametervalue"),
            new PrimaryKeyInfo("regionid", "parametername"),
            new NamedKeyInfo("regionid", "regionid"),
            new NamedKeyInfo("parametername", "parametername")
        };

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutDatabase;
    }
}
