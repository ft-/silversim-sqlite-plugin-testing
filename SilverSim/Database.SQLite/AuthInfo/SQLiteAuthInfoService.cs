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
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.AuthInfo
{
    [Description("SQLite AuthInfo Backend")]
    [PluginName("AuthInfo")]
    public sealed class SQLiteAuthInfoService : AuthInfoServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE AUTHINFO SERVICE");
        private readonly string m_ConnectionString;

        public SQLiteAuthInfoService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("auth"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("PasswordHash") { Cardinality = 32, IsFixed = true, IsNullAllowed = false },
            new AddColumn<string>("PasswordSalt") { Cardinality = 32, IsFixed = true, IsNullAllowed = false },
            new PrimaryKeyInfo("UserID"),
            new SqlTable("tokens"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false },
            new AddColumn<UUID>("Token") { IsNullAllowed = false },
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false },
            new AddColumn<Date>("Validity") { IsNullAllowed = false },
            new PrimaryKeyInfo("UserID", "Token"),
            new NamedKeyInfo("TokenIndex", "Token"),
            new NamedKeyInfo("UserIDIndex", "UserID"),
            new NamedKeyInfo("UserIDSessionID", "UserID", "SessionID") { IsUnique = true },
            new NamedKeyInfo("SessionIDIndex", "SessionID") { IsUnique = true },
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

        void IUserAccountDeleteServiceInterface.Remove(UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM auth WHERE UserID = @id", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM tokens WHERE UserID = @id", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        public override UserAuthInfo this[UUID accountid]
        {
            get
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM auth WHERE UserID = @id LIMIT 1", connection))
                    {
                        cmd.Parameters.AddParameter("@id", accountid);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserAuthInfo
                                {
                                    ID = reader.GetUUID("UserID"),
                                    PasswordHash = (string)reader["PasswordHash"],
                                    PasswordSalt = (string)reader["PasswordSalt"]
                                };
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Store(UserAuthInfo info)
        {
            var vals = new Dictionary<string, object>
            {
                ["UserID"] = info.ID,
                ["PasswordHash"] = info.PasswordHash,
                ["PasswordSalt"] = info.PasswordSalt
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.ReplaceInto("auth", vals);
            }
        }

        public override void SetPassword(UUID principalId, string password)
        {
            /* we use UserAuthInfo to calculate a new password */
            var ai = new UserAuthInfo
            {
                Password = password
            };
            var vals = new Dictionary<string, object>
            {
                ["PasswordHash"] = ai.PasswordHash,
                ["PasswordSalt"] = ai.PasswordSalt
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("auth", vals, "UserID = '" + principalId.ToString() + "'");
            }
        }

        public override UUID AddToken(UUID principalId, UUID sessionid, int lifetime_in_minutes)
        {
            UUID secureSessionID = UUID.Random;
            ulong d = Date.Now.AsULong + (ulong)lifetime_in_minutes * 30;
            var vals = new Dictionary<string, object>
            {
                ["UserID"] = principalId,
                ["SessionID"] = sessionid,
                ["Token"] = secureSessionID,
                ["Validity"] = Date.UnixTimeToDateTime(d)
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("tokens", vals);
            }
            return secureSessionID;
        }

        public override void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes)
        {
            bool valid;
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("UPDATE tokens SET Validity=@validity WHERE UserID = @id AND Token = @token AND Validity >= @current", connection))
                {
                    cmd.Parameters.AddParameter("@id", principalId);
                    cmd.Parameters.AddParameter("@validity", Date.UnixTimeToDateTime(Date.Now.AsULong + (ulong)lifetime_extension_in_minutes * 30));
                    cmd.Parameters.AddParameter("@token", token);
                    cmd.Parameters.AddParameter("@current", Date.Now);
                    valid = cmd.ExecuteNonQuery() > 0;
                }
                if (!valid)
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM tokens WHERE Validity <= @current", connection))
                    {
                        cmd.Parameters.AddParameter("@current", Date.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (!valid)
            {
                throw new VerifyTokenFailedException();
            }
        }

        public override void ReleaseToken(UUID accountId, UUID secureSessionId)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM tokens WHERE UserID = @id AND Token = @token", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountId);
                    cmd.Parameters.AddParameter("@token", secureSessionId);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override void ReleaseTokenBySession(UUID accountId, UUID sessionId)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM tokens WHERE UserID = @id AND SessionID = @sessionid", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountId);
                    cmd.Parameters.AddParameter("@sessionid", sessionId);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }
    }
}
