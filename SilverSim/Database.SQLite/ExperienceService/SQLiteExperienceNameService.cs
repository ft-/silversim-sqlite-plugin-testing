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
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.ExperienceService
{
    [Description("SQLite ExperienceName Backend")]
    [PluginName("ExperienceNames")]
    public sealed class SQLiteExperienceNameService : ExperienceNameServiceInterface, IDBServiceInterface, IPlugin
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE EXPERIENCE NAMES SERVICE");

        #region Constructor
        public SQLiteExperienceNameService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Accessors
        public override bool TryGetValue(UUID experienceID, out UEI uei)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM experiencenames WHERE ExperienceID = @experienceid LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            uei = ToUEI(dbReader);
                            return true;
                        }
                    }
                }
            }
            uei = default(UEI);
            return false;
        }

        private static UEI ToUEI(SQLiteDataReader dbReader) =>
            new UEI(dbReader.GetUUID("ExperienceID"), (string)dbReader["ExperienceName"], dbReader.GetUri("HomeURI"))
            {
                AuthorizationToken = dbReader.GetBytesOrNull("AuthorizationData")
            };

        public override List<UEI> GetExperiencesByName(string experienceName, int limit)
        {
            var experiences = new List<UEI>();
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM experiencenames WHERE ExperienceName = @experienceName LIMIT @limit", connection))
                {
                    cmd.Parameters.AddParameter("@experienceName", experienceName);
                    cmd.Parameters.AddParameter("@limit", limit);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            experiences.Add(ToUEI(dbReader));
                        }
                    }
                }
            }
            return experiences;
        }

        public override void Store(UEI experience)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                var vars = new Dictionary<string, object>
                {
                    { "ExperienceID", experience.ID },
                    { "HomeURI", experience.HomeURI },
                    { "ExperienceName", experience.ExperienceName }
                };
                if (experience.AuthorizationToken != null)
                {
                    vars.Add("AuthorizationData", experience.AuthorizationToken);
                }
                connection.ReplaceInto("experiencenames", vars);
            }
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
            new SqlTable("experiencenames"),
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("HomeURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("ExperienceName") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new PrimaryKeyInfo("ExperienceID"),
            new AddColumn<byte[]>("AuthorizationData"),
        };
    }
}
