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
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.MuteList;
using SilverSim.Types;
using SilverSim.Types.MuteList;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.MuteList
{
    [Description("SQLite MuteList Backend")]
    [PluginName("MuteList")]
    public sealed class SQLiteMuteListService : MuteListServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE MUTE SERVICE");
        private readonly string m_ConnectionString;

        public SQLiteMuteListService(IConfig ownConfig)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownConfig, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override List<MuteListEntry> GetList(UUID muteListOwnerID)
        {
            var res = new List<MuteListEntry>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM mutelists WHERE agentID = @agentid", conn))
                {
                    cmd.Parameters.AddParameter("@agentid", muteListOwnerID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            res.Add(new MuteListEntry
                            {
                                Flags = reader.GetEnum<MuteFlags>("flags"),
                                Type = reader.GetEnum<MuteType>("type"),
                                MuteID = reader.GetUUID("muteID"),
                                MuteName = (string)reader["muteName"]
                            });
                        }
                    }
                }
            }

            return res;
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM mutelists WHERE agentID = @agentid", conn))
                {
                    cmd.Parameters.AddParameter("@agentid", accountID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool Remove(UUID muteListOwnerID, UUID muteID, string muteName)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM mutelists WHERE agentID = @agentid AND muteID = @muteid AND muteName = @mutename", conn))
                {
                    cmd.Parameters.AddParameter("@agentid", muteListOwnerID);
                    cmd.Parameters.AddParameter("@muteid", muteID);
                    cmd.Parameters.AddParameter("@mutename", muteName);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override void Store(UUID muteListOwnerID, MuteListEntry mute)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                var vals = new Dictionary<string, object>
                {
                    ["agentID"] = muteListOwnerID,
                    ["muteID"] = mute.MuteID,
                    ["muteName"] = mute.MuteName,
                    ["flags"] = mute.Flags,
                    ["type"] = mute.Type
                };
                conn.ReplaceInto("mutelists", vals);
            }
        }

        public void VerifyConnection()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        private static readonly IMigrationElement[] m_Migrations = new IMigrationElement[]
        {
            new SqlTable("mutelists"),
            new AddColumn<UUID>("agentID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("muteID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("muteName") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<MuteFlags>("flags") { IsNullAllowed = false, Default = MuteFlags.None },
            new AddColumn<MuteType>("type") { IsNullAllowed = false, Default = MuteType.ByAgent },
            new PrimaryKeyInfo("agentID", "muteID", "muteName"),
            new NamedKeyInfo("agentID", "agentID")
        };

        public void ProcessMigrations()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(m_Migrations, m_Log);
            }
        }
    }
}
