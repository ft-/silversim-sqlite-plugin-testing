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
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.GridUser
{
    [Description("SQLite GridUser Backend")]
    [PluginName("GridUser")]
    public sealed class SQLiteGridUserService : GridUserServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE GRIDUSER SERVICE");

        #region Constructor
        public SQLiteGridUserService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("griduser"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("HomeRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("HomePosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("HomeLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<UUID>("LastRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("LastPosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("LastLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<bool>("IsOnline") { IsNullAllowed = false, Default = false },
            new AddColumn<Date>("LastLogin") {IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<Date>("LastLogout") {IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("LastRegionID", new string[] { "LastRegionID" }),
        };

        #region GridUserServiceInterface
        public override bool TryGetValue(UUID userID, out GridUserInfo userInfo)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM griduser WHERE ID = @id LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@id", userID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            userInfo = dbReader.ToGridUser();
                            return true;
                        }
                    }
                }
            }
            userInfo = default(GridUserInfo);
            return false;
        }

        public override GridUserInfo this[UUID userID]
        {
            get
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM griduser WHERE ID = @id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("@id", userID);
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return dbReader.ToGridUser();
                            }
                        }
                    }
                }
                throw new GridUserNotFoundException();
            }
        }

        public override bool TryGetValue(UGUI userID, out GridUserInfo gridUserInfo) =>
            TryGetValue(userID.ID, out gridUserInfo);

        public override GridUserInfo this[UGUI userID] => this[userID.ID];

        public override void LoggedInAdd(UGUI userID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE griduser SET IsOnline = 1, LastLogin = @curtime WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddParameter("@id", userID.ID);
                    cmd.Parameters.AddParameter("@curtime", Date.Now);
                    if (cmd.ExecuteNonQuery() >= 1)
                    {
                        return;
                    }
                }

                var param = new Dictionary<string, object>
                {
                    ["ID"] = userID.ID,
                    ["LastLogin"] = Date.Now,
                    ["IsOnline"] = true
                };
                conn.ReplaceInto("griduser", param);
            }
        }

        public override void LoggedIn(UGUI userID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE griduser SET IsOnline = 1, LastLogin = @curtime WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddParameter("@id", userID.ID);
                    cmd.Parameters.AddParameter("@curtime", Date.Now);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new GridUserUpdateFailedException();
                    }
                }
            }
        }

        public override void LoggedOut(UGUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                var data = new Dictionary<string, object>
                {
                    ["IsOnline"] = false,
                    ["LastLogout"] = Date.Now,
                    ["LastRegionID"] = lastRegionID,
                    ["LastPosition"] = lastPosition,
                    ["LastLookAt"] = lastLookAt
                };
                var where = new Dictionary<string, object>
                {
                    ["ID"] = userID.ID
                };
                conn.UpdateSet("griduser", data, where);
            }
        }

        public override void SetHome(UGUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                var data = new Dictionary<string, object>
                {
                    ["HomeRegionID"] = homeRegionID,
                    ["HomePosition"] = homePosition,
                    ["HomeLookAt"] = homeLookAt
                };
                var where = new Dictionary<string, object>
                {
                    ["ID"] = userID.ID
                };
                conn.UpdateSet("griduser", data, where);
            }
        }

        public override void SetPosition(UGUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                var data = new Dictionary<string, object>
                {
                    ["LastRegionID"] = lastRegionID,
                    ["LastPosition"] = lastPosition,
                    ["LastLookAt"] = lastLookAt
                };
                var where = new Dictionary<string, object>
                {
                    ["ID"] = userID.ID
                };
                conn.UpdateSet("griduser", data, where);
            }
        }
        #endregion

        public void Remove(UUID scopeID, UUID userAccount)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM griduser WHERE ID = @id", conn))
                {
                    cmd.Parameters.AddParameter("@id", userAccount);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
