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
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.TravelingData
{
    internal static class SQLiteTravelingDataExtensionMethods
    {
        public static TravelingDataInfo ToTravelingData(this SQLiteDataReader reader) => new TravelingDataInfo
        {
            SessionID = reader.GetUUID("SessionID"),
            UserID = reader.GetUUID("UserID"),
            GridExternalName = (string)reader["GridExternalName"],
            ServiceToken = (string)reader["ServiceToken"],
            ClientIPAddress = (string)reader["ClientIPAddress"],
            Timestamp = reader.GetDate("Timestamp")
        };
    }

    [Description("SQLite TravelingData Backend")]
    [PluginName("TravelingData")]
    public sealed class SQLiteTravelingDataService : TravelingDataServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE TRAVELINGDATA SERVICE");
        private readonly string m_ConnectionString;

        public SQLiteTravelingDataService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override TravelingDataInfo GetTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE UserID = @id AND (NOT GridExternalName = @homeuri) LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@id", agentID);
                    cmd.Parameters.AddParameter("@homeuri", homeURI);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override TravelingDataInfo GetTravelingData(UUID sessionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE SessionID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", sessionID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override TravelingDataInfo GetTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE UserID = @id AND ClientIPAddress = @ip", connection))
                {
                    cmd.Parameters.AddParameter("@id", agentID);
                    cmd.Parameters.AddParameter("@ip", ipAddress);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override List<TravelingDataInfo> GetTravelingDatasByAgentUUID(UUID agentID)
        {
            var infos = new List<TravelingDataInfo>();
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE UserID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", agentID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            infos.Add(reader.ToTravelingData());
                        }
                    }
                }
            }
            return infos;
        }

        public override bool Remove(UUID sessionID, out TravelingDataInfo info)
        {
            var outinfo = default(TravelingDataInfo);

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                bool res = connection.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE SessionID = @id", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", sessionID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                outinfo = reader.ToTravelingData();
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM travelingdata WHERE SessionID = @id", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", sessionID);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                });
                info = outinfo;
                return res;
            }
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM travelingdata WHERE UserID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool RemoveByAgentUUID(UUID agentID, out TravelingDataInfo info)
        {
            var outinfo = default(TravelingDataInfo);
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                bool res = connection.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT * FROM travelingdata WHERE UserID = @id LIMIT 1", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", agentID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                outinfo = reader.ToTravelingData();
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM travelingdata WHERE UserID = @id", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", agentID);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                });
                info = outinfo;
                return res;
            }
        }

        public override void Store(TravelingDataInfo data)
        {
            var insertVals = new Dictionary<string, object>
            {
                ["SessionID"] = data.SessionID.ToString(),
                ["UserID"] = data.UserID.ToString(),
                ["GridExternalName"] = data.GridExternalName,
                ["ServiceToken"] = data.ServiceToken,
                ["ClientIPAddress"] = data.ClientIPAddress,
                ["Timestamp"] = data.Timestamp
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.ReplaceInto("travelingdata", insertVals);
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("travelingdata"),
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("GridExternalName") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("ServiceToken") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("ClientIPAddress") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo(new string[] {"SessionID"}),
            new NamedKeyInfo("UserIDSessionID", new string[] { "UserID", "SessionID" }) { IsUnique = true }
        };

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
    }
}
