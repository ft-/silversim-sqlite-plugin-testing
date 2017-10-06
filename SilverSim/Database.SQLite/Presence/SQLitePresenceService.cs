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
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Presence
{
    [Description("SQLite Presence Backend")]
    [PluginName("Presence")]
    public sealed class SQLitePresenceService : PresenceServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLite PRESENCE SERVICE");

        #region Constructor
        public SQLitePresenceService(IConfig ownSection)
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
            new SqlTable("presence"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SecureSessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("LastSeen") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo("UserID"),
            new NamedKeyInfo("UserID", "UserID"),
            new NamedKeyInfo("SecureSessionID", "SecureSessionID"),
            new NamedKeyInfo("RegionID", "RegionID"),
        };

        #region PresenceServiceInterface
        public override List<PresenceInfo> GetPresencesInRegion(UUID regionId)
        {
            var presences = new List<PresenceInfo>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM presence WHERE RegionID = @regionID", conn))
                {
                    cmd.Parameters.AddParameter("@regionID", regionId);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pi = new PresenceInfo();
                            pi.UserID.ID = reader.GetUUID("UserID");
                            pi.RegionID = reader.GetUUID("RegionID");
                            pi.SessionID = reader.GetUUID("SessionID");
                            pi.SecureSessionID = reader.GetUUID("SecureSessionID");
                            presences.Add(pi);
                        }
                    }
                }
            }
            return presences;
        }

        public override List<PresenceInfo> this[UUID userID]
        {
            get
            {
                var presences = new List<PresenceInfo>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM presence WHERE UserID = @userID", conn))
                    {
                        cmd.Parameters.AddParameter("@userID", userID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var pi = new PresenceInfo();
                                pi.UserID.ID = reader.GetUUID("UserID");
                                pi.RegionID = reader.GetUUID("RegionID");
                                pi.SessionID = reader.GetUUID("SessionID");
                                pi.SecureSessionID = reader.GetUUID("SecureSessionID");
                                presences.Add(pi);
                            }
                        }
                    }
                }
                return presences;
            }
        }

        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM presence WHERE SessionID = @sessionID", conn))
                    {
                        cmd.Parameters.AddParameter("@sessionID", sessionID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var pi = new PresenceInfo();
                                pi.UserID.ID = reader.GetUUID("UserID");
                                pi.RegionID = reader.GetUUID("RegionID");
                                pi.SessionID = reader.GetUUID("SessionID");
                                pi.SecureSessionID = reader.GetUUID("SecureSessionID");
                                return pi;
                            }
                        }
                    }
                }
                throw new PresenceNotFoundException();
            }
        }

        public override void Logout(UUID sessionID, UUID userID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM presence WHERE SessionID = @sessionID", conn))
                {
                    cmd.Parameters.AddParameter("@sessionID", sessionID);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new PresenceUpdateFailedException();
                    }
                }
            }
        }

        public override void Login(PresenceInfo pInfo)
        {
            var post = new Dictionary<string, object>
            {
                ["UserID"] = pInfo.UserID.ID,
                ["SessionID"] = pInfo.SessionID,
                ["SecureSessionID"] = pInfo.SecureSessionID,
                ["RegionID"] = UUID.Zero,
                ["LastSeen"] = Date.Now
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                try
                {
                    conn.InsertInto("presence", post);
                }
                catch (Exception e)
                {
                    m_Log.Debug("Presence update failed", e);
                    throw new PresenceUpdateFailedException();
                }
            }
        }

        public override void Report(PresenceInfo pInfo)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE presence SET RegionID = @regionID WHERE SessionID = @sessionID", conn))
                {
                    cmd.Parameters.AddParameter("@regionID", pInfo.RegionID);
                    cmd.Parameters.AddParameter("@sessionID", pInfo.SessionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void LogoutRegion(UUID regionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM presence WHERE RegionID = @regionid", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM presence WHERE UserID = @userid", conn))
                {
                    cmd.Parameters.AddParameter("@userid", userAccount);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
