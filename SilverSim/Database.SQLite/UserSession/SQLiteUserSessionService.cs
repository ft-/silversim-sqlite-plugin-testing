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
using SilverSim.Database.SQLite;
using SilverSim.Database.SQLite._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.UserSession;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.MySQL.UserSession
{
    [Description("SQLite UserSession Backend")]
    [PluginName("UserSession")]
    public sealed class MySQLUserSessionService : UserSessionServiceInterface, IPlugin, IDBServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE USERSESSION SERVICE");

        #region Constructor
        public MySQLUserSessionService(IConfig ownSection)
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
            new SqlTable("usersessions"),
            new AddColumn<UUID>("sessionid") { IsNullAllowed = false },
            new AddColumn<UUID>("securesessionid") { IsNullAllowed = false },
            new AddColumn<UGUI>("user") { IsNullAllowed = false },
            new AddColumn<string>("clientipaddress") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<Date>("timestamp") { IsNullAllowed = false },
            new PrimaryKeyInfo("sessionid"),
            new NamedKeyInfo("securesessionid", "securesessionid") { IsUnique = true },
            new NamedKeyInfo("user", "user"),
            new NamedKeyInfo("clientipaddress", "clientipaddress"),

            new SqlTable("usersessiondata"),
            new AddColumn<UUID>("sessionid") { IsNullAllowed = false },
            new AddColumn<string>("assoc") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("varname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("value") { IsNullAllowed = false },
            new AddColumn<bool>("isexpiring") { IsNullAllowed = false, Default = false },
            new AddColumn<Date>("expirydate"),
            new PrimaryKeyInfo("sessionid", "assoc", "varname"),
            new NamedKeyInfo("sessionid", "sessionid")
        };

        public override List<UserSessionInfo> this[UGUI user]
        {
            get
            {
                var list = new List<UserSessionInfo>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM usersessions WHERE user=@user", conn))
                    {
                        cmd.Parameters.AddParameter("@user", new UGUI(user));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new UserSessionInfo
                                {
                                    SessionID = reader.GetUUID("sessionid"),
                                    SecureSessionID = reader.GetUUID("securesessionid"),
                                    User = reader.GetUGUI("user"),
                                    ClientIPAddress = (string)reader["clientipaddress"],
                                    Timestamp = reader.GetDate("timestamp")
                                });
                            }
                        }
                    }

                    foreach(UserSessionInfo info in list)
                    {
                        RetrieveData(conn, info);
                    }
                }
                return list;
            }
        }

        public override string this[UUID sessionID, string assoc, string varname]
        {
            get
            {
                string value;
                if (!TryGetValue(sessionID, assoc, varname, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }
            set
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.InsideTransaction((transaction) =>
                    {
                        using (var cmd = new SQLiteCommand("SELECT NULL FROM usersessions WHERE sessionid = @sessionid", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@sessionid", sessionID);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new KeyNotFoundException();
                                }
                            }
                        }

                        var vals = new Dictionary<string, object>
                        {
                            ["sessionid"] = sessionID,
                            ["assoc"] = assoc,
                            ["varname"] = varname,
                            ["value"] = value,
                            ["isexpiring"] = false
                        };
                        conn.ReplaceInto("usersessiondata", vals, transaction);
                    });
                }
            }
        }

        public override bool ContainsKey(UUID sessionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM usersessions WHERE sessionid=@sessionid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@sessionid", sessionID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public override bool ContainsKey(UGUI user)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM usersessions WHERE user=@user LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@user", new UGUI(user));
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public override bool ContainsKey(UUID sessionID, string assoc, string varname)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM usersessiondata WHERE sessionid=@sessionid AND assoc=@assoc AND varname=@varname", conn))
                {
                    cmd.Parameters.AddParameter("@sessionid", sessionID);
                    cmd.Parameters.AddParameter("@assoc", assoc);
                    cmd.Parameters.AddParameter("@varname", varname);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public override UserSessionInfo CreateSession(UGUI user, string clientIPAddress, UUID sessionID, UUID secureSessionID)
        {
            UserSessionInfo userSession = new UserSessionInfo
            {
                SessionID = sessionID,
                SecureSessionID = secureSessionID,
                ClientIPAddress = clientIPAddress,
                User = user
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                var vals = new Dictionary<string, object>
                {
                    ["sessionid"] = sessionID,
                    ["securesessionid"] = secureSessionID,
                    ["clientipaddress"] = clientIPAddress,
                    ["user"] = new UGUI(user),
                    ["timestamp"] = userSession.Timestamp
                };
                conn.InsertInto("usersessions", vals);
            }
            return userSession;
        }

        public override bool Remove(UUID sessionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                return conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM usersessiondata WHERE sessionid=@sessionid", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@sessionid", sessionID);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM usersessions WHERE sessionid=@sessionid", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@sessionid", sessionID);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                });
            }
        }

        public override bool Remove(UGUI user)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                return conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM usersessiondata WHERE sessionid IN (SELECT sessionid FROM usersessions WHERE user=@user)", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@user", new UGUI(user));
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM usersessions WHERE user=@user", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@user", user);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                });
            }
        }

        public override bool Remove(UUID sessionID, string assoc, string varname)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM usersessiondata WHERE sessionid=@sessionid AND assoc=@assoc AND varname=@varname", conn))
                {
                    cmd.Parameters.AddParameter("@sessionid", sessionID);
                    cmd.Parameters.AddParameter("@assoc", assoc);
                    cmd.Parameters.AddParameter("@varname", varname);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override void SetExpiringValue(UUID sessionID, string assoc, string varname, string value, TimeSpan span)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT NULL FROM usersessions WHERE sessionid = @sessionid", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@sessionid", sessionID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                throw new KeyNotFoundException();
                            }
                        }
                    }

                    var vals = new Dictionary<string, object>
                    {
                        ["sessionid"] = sessionID,
                        ["assoc"] = assoc,
                        ["varname"] = varname,
                        ["value"] = value,
                        ["isexpiring"] = true,
                        ["expirydata"] = Date.Now.Add(span)
                    };
                    conn.ReplaceInto("usersessiondata", vals, transaction);
                });
            }
        }

        private void RetrieveData(SQLiteConnection conn, UserSessionInfo sessionInfo)
        {
            using (var cmd = new SQLiteCommand("SELECT * FROM usersessiondata WHERE sessionid=@sessionid AND (NOT isexpiring OR expirydate >= @now)", conn))
            {
                cmd.Parameters.AddParameter("@sessionid", sessionInfo.SessionID);
                cmd.Parameters.AddParameter("@now", Date.Now);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Date expirydate = null;
                        if (reader.GetBool("isexpiring"))
                        {
                            expirydate = reader.GetDate("expirydate");
                        }

                        sessionInfo.DynamicData.Add($"{(string)reader["assoc"]}/{(string)reader["varname"]}",
                            new UserSessionInfo.Entry
                            {
                                ExpiryDate = expirydate,
                                Value = (string)reader["value"]
                            });
                    }
                }
            }
        }

        public override bool TryGetSecureValue(UUID secureSessionID, out UserSessionInfo sessionInfo)
        {
            sessionInfo = default(UserSessionInfo);
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM usersessions WHERE securesessionid=@securesessionid", conn))
                {
                    cmd.Parameters.AddParameter("@securesessionid", secureSessionID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }
                        sessionInfo = new UserSessionInfo
                        {
                            SessionID = reader.GetUUID("sessionid"),
                            SecureSessionID = reader.GetUUID("securesessionid"),
                            User = reader.GetUGUI("user"),
                            Timestamp = reader.GetDate("timestamp"),
                            ClientIPAddress = (string)reader["clientipaddress"]
                        };
                    }
                }

                RetrieveData(conn, sessionInfo);
            }
            return true;
        }

        public override bool TryGetValue(UUID sessionID, out UserSessionInfo sessionInfo)
        {
            sessionInfo = default(UserSessionInfo);
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM usersessions WHERE sessionid=@sessionid", conn))
                {
                    cmd.Parameters.AddParameter("@sessionid", sessionID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }
                        sessionInfo = new UserSessionInfo
                        {
                            SessionID = reader.GetUUID("sessionid"),
                            SecureSessionID = reader.GetUUID("securesessionid"),
                            User = reader.GetUGUI("user"),
                            Timestamp = reader.GetDate("timestamp"),
                            ClientIPAddress = (string)reader["clientipaddress"]
                        };
                    }
                }

                RetrieveData(conn, sessionInfo);
            }
            return true;
        }

        public override bool TryGetValue(UUID sessionID, string assoc, string varname, out UserSessionInfo.Entry value)
        {
            value = default(UserSessionInfo.Entry);
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM usersessiondata WHERE sessionid=@sessionid AND assoc=@assoc AND varname=@varname AND (NOT isexpiring OR expirydate > @now)", conn))
                {
                    cmd.Parameters.AddParameter("@sessionid", sessionID);
                    cmd.Parameters.AddParameter("@assoc", assoc);
                    cmd.Parameters.AddParameter("@varname", varname);
                    cmd.Parameters.AddParameter("@now", Date.Now);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }
                        Date expirydate = null;
                        if (reader.GetBool("isexpiring"))
                        {
                            expirydate = reader.GetDate("expirydate");
                        }

                        value = new UserSessionInfo.Entry
                        {
                            Value = (string)reader["value"],
                            ExpiryDate = expirydate
                        };
                        return true;
                    }
                }
            }
        }

        public override bool TryGetValueExtendLifetime(UUID sessionID, string assoc, string varname, TimeSpan span, out UserSessionInfo.Entry value)
        {
            UserSessionInfo.Entry val = default(UserSessionInfo.Entry);
            bool success;
            value = val;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                success = conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT * FROM usersessiondata WHERE sessionid = @sessionid AND assoc = @assoc AND varname = @varname AND (NOT isexpiring OR expirydate >= @now)", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@sessionid", sessionID);
                        cmd.Parameters.AddParameter("@assoc", assoc);
                        cmd.Parameters.AddParameter("@varname", varname);
                        cmd.Parameters.AddParameter("@now", Date.Now);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return false;
                            }
                            val = new UserSessionInfo.Entry
                            {
                                Value = (string)reader["value"]
                            };

                            if (reader.GetBool("isexpiring"))
                            {
                                val.ExpiryDate = reader.GetDate("expirydate");
                            }
                        }
                    }

                    if (val.ExpiryDate != null)
                    {
                        using (var cmd = new SQLiteCommand("UPDATE usersessiondata SET expirydate = @expirydate WHERE sessionid = @sessionid AND assoc = @assoc AND varname = @varname", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@expirydate", val.ExpiryDate.Add(span));
                            cmd.Parameters.AddParameter("@assoc", assoc);
                            cmd.Parameters.AddParameter("@varname", varname);
                        }
                    }
                    return true;
                });
            }
            value = val;
            return success;
        }
    }
}
