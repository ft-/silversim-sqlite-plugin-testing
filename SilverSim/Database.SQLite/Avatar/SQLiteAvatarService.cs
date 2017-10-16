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
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Avatar
{
    [Description("SQLite Avatar Backend")]
    [PluginName("Avatar")]
    public sealed class SQLiteAvatarService : AvatarServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE AVATAR SERVICE");

        public SQLiteAvatarService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override Dictionary<string, string> this[UUID avatarID]
        {
            get
            {
                var result = new Dictionary<string, string>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT Name,Value FROM avatars WHERE PrincipalID = @principalid", connection))
                    {
                        cmd.Parameters.AddParameter("@principalid", avatarID);
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                result.Add((string)dbReader["Name"], (string)dbReader["Value"]);
                            }
                        }
                    }
                }

                return result;
            }
            set
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (value == null)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM avatars WHERE PrincipalID = @principalid", connection))
                        {
                            cmd.Parameters.AddParameter("@principalid", avatarID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        connection.InsideTransaction((transaction) =>
                        {
                            using (var cmd = new SQLiteCommand("DELETE FROM avatars WHERE PrincipalID = @principalid", connection)
                            {
                                Transaction = transaction
                            })
                            {
                                cmd.Parameters.AddParameter("@principalid", avatarID);
                                cmd.ExecuteNonQuery();
                            }

                            var vals = new Dictionary<string, object>
                            {
                                ["PrincipalID"] = avatarID
                            };
                            foreach (KeyValuePair<string, string> kvp in value)
                            {
                                vals["Name"] = kvp.Key;
                                vals["Value"] = kvp.Value;
                                connection.InsertInto("avatars", vals, transaction);
                            }
                        });
                    }
                }
            }
        }

        public override List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get
            {
                var result = new List<string>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();

                    connection.InsideTransaction((transaction) =>
                    {
                        foreach (string key in itemKeys)
                        {
                            using (var cmd = new SQLiteCommand("SELECT Value FROM avatars WHERE PrincipalID = @principalid AND Name = @name", connection)
                            {
                                Transaction = transaction
                            })
                            {
                                cmd.Parameters.AddParameter("@principalid", avatarID);
                                cmd.Parameters.AddParameter("@name", key);
                                using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                                {
                                    result.Add(dbReader.Read() ? (string)dbReader["Value"] : string.Empty);
                                }
                            }
                        }
                    });
                }
                return result;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                else if (itemKeys == null)
                {
                    throw new ArgumentNullException(nameof(itemKeys));
                }
                if (itemKeys.Count != value.Count)
                {
                    throw new ArgumentException("value and itemKeys must have identical Count");
                }

                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();

                    var vals = new Dictionary<string, object>
                    {
                        ["PrincipalID"] = avatarID
                    };
                    connection.InsideTransaction((transaction) =>
                    {
                        for (int i = 0; i < itemKeys.Count; ++i)
                        {
                            vals["Name"] = itemKeys[i];
                            vals["Value"] = value[i];
                            connection.ReplaceInto("avatars", vals, transaction);
                        }
                    });
                }
            }
        }

        public override bool TryGetValue(UUID avatarID, string itemKey, out string value)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT Value FROM avatars WHERE PrincipalID = @principalid AND Name = @name", connection))
                {
                    cmd.Parameters.AddParameter("@principalid", avatarID);
                    cmd.Parameters.AddParameter("@name", itemKey);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            value = (string)dbReader["Value"];
                            return true;
                        }
                    }
                }
            }

            value = string.Empty;
            return false;
        }

        public override string this[UUID avatarID, string itemKey]
        {
            get
            {
                string s;
                if (!TryGetValue(avatarID, itemKey, out s))
                {
                    throw new KeyNotFoundException(string.Format("{0},{1} not found", avatarID, itemKey));
                }
                return s;
            }
            set
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    var vals = new Dictionary<string, object>
                    {
                        ["PrincipalID"] = avatarID,
                        ["Name"] = itemKey,
                        ["Value"] = value
                    };
                    connection.ReplaceInto("avatars", vals);
                }
            }
        }

        public override void Remove(UUID avatarID, IList<string> nameList)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    foreach (string name in nameList)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM avatars WHERE PrincipalID = @principalid AND Name = @name", connection)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@principalid", avatarID);
                            cmd.Parameters.AddParameter("@name", name);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        public override void Remove(UUID avatarID, string name)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM avatars WHERE PrincipalID = @principalid AND Name = @name", connection))
                {
                    cmd.Parameters.AddParameter("@principalid", avatarID);
                    cmd.Parameters.AddParameter("@name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations, m_Log);
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("avatars"),
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 32, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Value"),
            new PrimaryKeyInfo("PrincipalID", "Name"),
            new NamedKeyInfo("avatars_principalid", new string[] { "PrincipalID" })
        };

        public void Remove(UUID scopeID, UUID userAccount)
        {
            this[userAccount] = null;
        }
    }
}
