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
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.UserAccounts
{
    [Description("SQLite UserAccount AvatarName backend")]
    [PluginName("UserAccountNames")]
    public sealed class SQLiteUserAccountNameService : AvatarNameServiceInterface, IPlugin, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE USERACCOUNTNAME SERVICE");

        private readonly string m_ConnectionString;

        public SQLiteUserAccountNameService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public override UUI this[UUID key]
        {
            get
            {
                UUI uui;
                if (!TryGetValue(key, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public override void Store(UUI uui)
        {
            /* intentionally ignored */
        }

        public override bool Remove(UUID key) => false;

        public override UUI this[string firstName, string lastName]
        {
            get
            {
                UUI uui;
                if (!TryGetValue(firstName, lastName, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public void ProcessMigrations()
        {
            /* intentionally left empty */
        }

        public override List<UUI> Search(string[] names)
        {
            var list = new List<UUI>();

            if (names.Length == 1)
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts WHERE FirstName LIKE @name AND LastName LIKE @name", connection))
                    {
                        cmd.Parameters.AddParameter("@name", "%" + names[0] + "%");
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(GetUUIFromReader(reader));
                            }
                        }
                    }
                }
            }
            else if (names.Length == 2)
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM useraccounts WHERE FirstName LIKE @name0 AND LastName LIKE @name1", connection))
                    {
                        cmd.Parameters.AddParameter("@name0", "%" + names[0] + "%");
                        cmd.Parameters.AddParameter("@name1", "%" + names[1] + "%");
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(GetUUIFromReader(reader));
                            }
                        }
                    }
                }
            }
            return list;
        }

        private static UUI GetUUIFromReader(SQLiteDataReader reader) => new UUI()
        {
            FirstName = (string)reader["FirstName"],
            LastName = (string)reader["LastName"],
            ID = reader.GetUUID("ID"),
            IsAuthoritative = true
        };

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override bool TryGetValue(UUID key, out UUI uui)
        {
            uui = null;
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT ID, FirstName, LastName FROM useraccounts WHERE ID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", key);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uui = GetUUIFromReader(reader);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            uui = null;
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT ID, FirstName, LastName FROM useraccounts WHERE FirstName = @first AND LastName = @last", connection))
                {
                    cmd.Parameters.AddParameter("@first", firstName);
                    cmd.Parameters.AddParameter("@last", lastName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uui = GetUUIFromReader(reader);
                            return true;
                        }
                    }
                }
            }
            return false;
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
