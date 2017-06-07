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
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.OfflineIM
{
    [Description("SQLite OfflineIM Backend")]
    [PluginName("OfflineIM")]
    public sealed class SQLiteOfflineIMService : OfflineIMServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE OFFLINEIM SERVICE");
        private readonly string m_ConnectionString;

        public SQLiteOfflineIMService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public override void DeleteOfflineIM(ulong offlineImID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("DELETE FROM offlineim WHERE ID = @id", connection))
                {
                    command.Parameters.AddParameter("@id", offlineImID);
                    command.ExecuteNonQuery();
                }
            }
        }

        public override List<GridInstantMessage> GetOfflineIMs(UUID principalID)
        {
            var ims = new List<GridInstantMessage>();
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM offlineim WHERE ToAgentID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", principalID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var im = new GridInstantMessage()
                            {
                                ID = (ulong)(long)reader["ID"],
                                FromAgent = reader.GetUUI("FromAgent"),
                                FromGroup = reader.GetUGI("FromGroup"),
                                ToAgent = new UUI(reader.GetUUID("ToAgentID")),
                                Dialog = reader.GetEnum<GridInstantMessageDialog>("Dialog"),
                                IsFromGroup = reader.GetBool("IsFromGroup"),
                                Message = (string)reader["Message"],
                                IMSessionID = reader.GetUUID("IMSessionID"),
                                Position = reader.GetVector3("Position"),
                                BinaryBucket = reader.GetBytes("BinaryBucket"),
                                ParentEstateID = (uint)(int)reader["ParentEstateID"],
                                RegionID = reader.GetUUID("RegionID"),
                                Timestamp = reader.GetDate("Timestamp"),
                                IsOffline = true
                            };
                            ims.Add(im);
                        }
                    }
                }
            }
            return ims;
        }

        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("offlineim"),
            new AddColumn<ulong>("ID") { IsNullAllowed = false },
            new AddColumn<UUI>("FromAgent") { IsNullAllowed = false },
            new AddColumn<UGI>("FromGroup") { IsNullAllowed = false },
            new AddColumn<UUID>("ToAgentID") { IsNullAllowed = false },
            new AddColumn<GridInstantMessageDialog>("Dialog") { IsNullAllowed = false },
            new AddColumn<bool>("IsFromGroup") { IsNullAllowed = false },
            new AddColumn<string>("Message") { IsLong = true },
            new AddColumn<UUID>("IMSessionID") { IsNullAllowed = false },
            new AddColumn<Vector3>("Position") { IsNullAllowed = false },
            new AddColumn<byte[]>("BinaryBucket") { IsNullAllowed = false },
            new AddColumn<uint>("ParentEstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") {IsNullAllowed = false }
        };

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("DELETE FROM offlineim WHERE ToAgentID = @id", connection))
                {
                    command.Parameters.AddParameter("@id", accountID);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void StoreOfflineIM(GridInstantMessage im)
        {
            var vals = new Dictionary<string, object>
            {
                ["ID"] = im.ID,
                ["FromAgent"] = im.FromAgent,
                ["FromGroup"] = im.FromGroup,
                ["ToAgentID"] = im.ToAgent.ID,
                ["Dialog"] = im.Dialog,
                ["IsFromGroup"] = im.IsFromGroup,
                ["Message"] = im.Message,
                ["IMSessionID"] = im.IMSessionID,
                ["Position"] = im.Position,
                ["BinaryBucket"] = im.BinaryBucket,
                ["ParentEstateID"] = im.ParentEstateID,
                ["RegionID"] = im.RegionID,
                ["Timestamp"] = im.Timestamp
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("offlineim", vals);
            }
        }

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }
    }
}
