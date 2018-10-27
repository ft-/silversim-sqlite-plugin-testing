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
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.UserAccounts
{
    [Description("SQLite UserAccount Backend")]
    [PluginName("UserAccounts")]
    public sealed class SQLiteUserAccountService : UserAccountServiceInterface, IDBServiceInterface, IPlugin, IUserAccountSerialNoInterface
    {
        private readonly string m_ConnectionString;
        private Uri m_HomeURI;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE USERACCOUNT SERVICE");

        #region Constructor
        public SQLiteUserAccountService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HomeURI = new Uri(loader.HomeURI);
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

            ulong serno;
            if (!TryGetSerialNumber(out serno))
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new SQLiteCommand("SELECT COUNT(ID) FROM useraccounts", connection))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                throw new ConfigurationLoader.ConfigurationErrorException("Failed to read number of accounts");
                            }
                            serno = (ulong)reader.GetInt32(0);
                        }
                    }

                    var vals = new Dictionary<string, object>
                    {
                        { "SerialNumber", serno }
                    };
                    connection.InsertInto("useraccounts_serial", vals);
                }
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("useraccounts"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ScopeID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("FirstName") { Cardinality = 31, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("LastName") { Cardinality = 31, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Email") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<Date>("Created") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<int>("UserLevel") { IsNullAllowed = false, Default = 0 },
            new AddColumn<uint>("UserFlags") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<string>("UserTitle") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<bool>("IsEverLoggedIn") {IsNullAllowed = false, Default = false },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("Email", "Email"),
            new NamedKeyInfo("Name", "FirstName", "LastName") { IsUnique = true },
            new NamedKeyInfo("FirstName", "FirstName"),
            new NamedKeyInfo("LastName", "LastName"),
            new TableRevision(2),
            new ChangeColumn<UserFlags>("UserFlags") { IsNullAllowed = false, Default = UserFlags.None },
            new AddColumn<Date>("LastLogout"),
            new AddColumn<UUID>("LastRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("LastPosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("LastLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<string>("LastGatekeeperURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UUID>("HomeRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("HomePosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("HomeLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<string>("HomeGatekeeperURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },

            new SqlTable("useraccounts_serial"),
            new AddColumn<ulong>("SerialNumber") { IsNullAllowed = false, Default = (ulong)0 }
        };

        public override bool ContainsKey(UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT ID FROM useraccounts WHERE ID = @id LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID accountID, out UserAccount account)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts WHERE ID = @id LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = reader.ToUserAccount(m_HomeURI);
                            return true;
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        public override bool ContainsKey(string email)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM useraccounts WHERE Email = @email LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@email", email);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(string email, out UserAccount account)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts WHERE Email = @email LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@email", email);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = reader.ToUserAccount(m_HomeURI);
                            return true;
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        public override bool ContainsKey(string firstName, string lastName)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM useraccounts WHERE FirstName = @firstname AND LastName = @lastname LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@firstname", firstName);
                    cmd.Parameters.AddParameter("@lastname", lastName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(string firstName, string lastName, out UserAccount account)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts WHERE FirstName = @firstname AND LastName = @lastname LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@firstname", firstName);
                    cmd.Parameters.AddParameter("@lastname", lastName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = reader.ToUserAccount(m_HomeURI);
                            return true;
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        public override List<UserAccount> GetAccounts(string query)
        {
            string[] words = query.Split(new char[] { ' ' }, 2);
            var accounts = new List<UserAccount>();
            if (query.Trim().Length == 0)
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts", connection))
                    {
                        using (SQLiteDataReader dbreader = cmd.ExecuteReader())
                        {
                            while (dbreader.Read())
                            {
                                accounts.Add(dbreader.ToUserAccount(m_HomeURI));
                            }
                        }
                    }
                }
                return accounts;
            }

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                string cmdstr = "select * from useraccounts where (FirstName LIKE @word0 or LastName LIKE @word0)";
                if (words.Length == 2)
                {
                    cmdstr = "select * from useraccounts where (FirstName LIKE @word0 or LastName LIKE @word1)";
                }
                using (var cmd = new SQLiteCommand(cmdstr, connection))
                {
                    for (int i = 0; i < words.Length; ++i)
                    {
                        cmd.Parameters.AddParameter("@word" + i.ToString(), words[i]);
                    }
                    using (SQLiteDataReader dbreader = cmd.ExecuteReader())
                    {
                        while (dbreader.Read())
                        {
                            accounts.Add(dbreader.ToUserAccount(m_HomeURI));
                        }
                    }
                }
            }
            return accounts;
        }

        public override void Add(UserAccount userAccount)
        {
            var data = new Dictionary<string, object>
            {
                ["ID"] = userAccount.Principal.ID,
                ["FirstName"] = userAccount.Principal.FirstName,
                ["LastName"] = userAccount.Principal.LastName,
                ["Email"] = userAccount.Email,
                ["Created"] = userAccount.Created,
                ["UserLevel"] = userAccount.UserLevel,
                ["UserFlags"] = userAccount.UserFlags,
                ["UserTitle"] = userAccount.UserTitle,
                ["IsEverLoggedIn"] = userAccount.IsEverLoggedIn
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    connection.InsertInto("useraccounts", data, transaction);
                    using (var cmd = new SQLiteCommand("UPDATE useraccounts_serial SET SerialNumber = SerialNumber + 1", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        private bool TryGetSerialNumber(out ulong serialno)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT SerialNumber FROM useraccounts_serial LIMIT 1", connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            serialno = (ulong)(long)reader["SerialNumber"];
                            return true;
                        }
                    }
                }
            }
            serialno = 0;
            return false;
        }

        public ulong SerialNumber
        {
            get
            {
                ulong serno;
                if (!TryGetSerialNumber(out serno))
                {
                    throw new InvalidOperationException("Serial number access failed");
                }
                return serno;
            }
        }

        public override void Remove(UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM useraccounts WHERE ID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public List<UGUIWithName> AccountList
        {
            get
            {
                var list = new List<UGUIWithName>();

                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT ID, FirstName, LastName FROM useraccounts", conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new UGUIWithName
                                {
                                    ID = reader.GetUUID("ID"),
                                    FirstName = (string)reader["FirstName"],
                                    LastName = (string)reader["LastName"]
                                });
                            }
                        }
                    }
                }

                return list;
            }
        }

        #region Online Status
        public override void LoggedOut(UUID accountID, UserRegionData regionData = null)
        {
            var data = new Dictionary<string, object>
            {
                ["LastLogout"] = Date.Now,
            };
            if (regionData != null)
            {
                data["LastRegionID"] = regionData.RegionID;
                data["LastPosition"] = regionData.Position;
                data["LastLookAt"] = regionData.LookAt;
                data["LastGatekeeperURI"] = regionData.GatekeeperURI?.ToString() ?? string.Empty;
            }
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void SetHome(UUID accountID, UserRegionData regionData)
        {
            if (regionData == null)
            {
                throw new ArgumentNullException(nameof(regionData));
            }
            var data = new Dictionary<string, object>
            {
                ["HomeRegionID"] = regionData.RegionID,
                ["HomePosition"] = regionData.Position,
                ["HomeLookAt"] = regionData.LookAt,
                ["HomeGatekeeperURI"] = regionData.GatekeeperURI?.ToString() ?? string.Empty
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void SetPosition(UUID accountID, UserRegionData regionData)
        {
            if (regionData == null)
            {
                throw new ArgumentNullException(nameof(regionData));
            }
            var data = new Dictionary<string, object>
            {
                ["LastRegionID"] = regionData.RegionID,
                ["LastPosition"] = regionData.Position,
                ["LastLookAt"] = regionData.LookAt,
                ["LastGatekeeperURI"] = regionData.GatekeeperURI?.ToString() ?? string.Empty,
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }
        #endregion

        public override void SetEverLoggedIn(UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("UPDATE useraccounts SET IsEverLoggedIn=1 WHERE ID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }


        public override void SetEmail(UUID accountID, string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }
            var data = new Dictionary<string, object>
            {
                ["Email"] = email,
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void SetUserLevel(UUID accountID, int userLevel)
        {
            if (userLevel < -1 || userLevel > 255)
            {
                throw new ArgumentNullException(nameof(userLevel));
            }
            var data = new Dictionary<string, object>
            {
                ["UserLevel"] = userLevel,
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void SetUserFlags(UUID accountID, UserFlags userFlags)
        {
            var data = new Dictionary<string, object>
            {
                ["UserFlags"] = userFlags,
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void SetUserTitle(UUID accountID, string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }
            var data = new Dictionary<string, object>
            {
                ["UserTitle"] = title,
            };
            var w = new Dictionary<string, object>
            {
                ["ID"] = accountID
            };
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }
    }
}
