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
using System.Data.SQLite;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Tests.Preconditions.SQLite
{
    [PluginName("ResetDatabase")]
    public class ResetSQLiteDatabase : IPlugin, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE DATABASE RESET");
        private readonly List<SQLiteConnectionStringBuilder> m_ConnectionStrings = new List<SQLiteConnectionStringBuilder>();

        public ResetSQLiteDatabase(ConfigurationLoader loader, IConfig config)
        {
            var processedDataSources = new List<string>();
            foreach (string service in config.GetString("Services").Split(','))
            {
                IConfig cfg = loader.Config.Configs[service.Trim()];
                if (cfg != null)
                {
                    SQLiteConnectionStringBuilder cfgString = BuildConnectionString(cfg, m_Log);
                    if (!processedDataSources.Contains(cfgString.DataSource))
                    {
                        m_ConnectionStrings.Add(cfgString);
                        processedDataSources.Add(cfgString.DataSource);
                    }
                }
            }
        }

        private static SQLiteConnectionStringBuilder BuildConnectionString(IConfig config, ILog log)
        {
            string configName = config.Name;
            bool containsDataSource = config.Contains("DataSource");
            if (!containsDataSource && !config.Contains("DataSourceDefault"))
            {
                log.FatalFormat("[SQLITE CONFIG]: Parameter 'DataSource' missing in [{0}]", configName);
                throw new ConfigurationLoader.ConfigurationErrorException();
            }
            return new SQLiteConnectionStringBuilder
            {
                DataSource = config.GetString(containsDataSource ? "DataSource" : "DataSourceDefault"),
                Password = config.GetString("Password", string.Empty)
            };
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public void VerifyConnection()
        {
            foreach (SQLiteConnectionStringBuilder connStr in m_ConnectionStrings)
            {
                var tables = new List<string>();

                using (var connection = new SQLiteConnection(connStr.ToString()))
                {
                    connection.Open();
                    m_Log.Info("Executing reset database");
                    using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tables.Add((string)reader.GetValue(0));
                            }
                        }
                    }

                    m_Log.InfoFormat("Deleting {0} tables", tables.Count);
                    foreach (string table in tables)
                    {
                        m_Log.InfoFormat("Deleting table {0}", table);
                        using (var cmd = new SQLiteCommand(string.Format("DROP TABLE {0}", table), connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public void ProcessMigrations()
        {
        }
    }
}
