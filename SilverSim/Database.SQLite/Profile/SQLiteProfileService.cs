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
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Profile
{
    [Description("SQLite Profile Backend")]
    [PluginName("Profile")]
    public sealed partial class SQLiteProfileService : ProfileServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE PROFILE SERVICE");

        private readonly string m_ConnectionString;

        public SQLiteProfileService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(() =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM classifieds where creatoruuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM userpicks where creatoruuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM usernotes where useruuid = @uuid OR targetuuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM usersettings where useruuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM userprofile where useruuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("UPDATE userprofile set profilePartner = \"00000000-0000-0000-0000-000000000000\" where profilePartner = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        public override IClassifiedsInterface Classifieds => this;

        public override IPicksInterface Picks => this;

        public override INotesInterface Notes => this;

        public override IUserPreferencesInterface Preferences => this;

        public override IPropertiesInterface Properties => this;

        public void VerifyConnection()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
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
            new SqlTable("classifieds"),
            new AddColumn<UUID>("classifieduuid") { IsNullAllowed = false },
            new AddColumn<UUID>("creatoruuid") { IsNullAllowed = false },
            new AddColumn<Date>("creationdate") { IsNullAllowed = false },
            new AddColumn<Date>("expirationdate") { IsNullAllowed = false },
            new AddColumn<int>("Category") { IsNullAllowed = false },
            new AddColumn<string>("name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("description") { IsNullAllowed = false },
            new AddColumn<ParcelID>("parceluuid") { IsNullAllowed = false },
            new AddColumn<int>("parentestate") { IsNullAllowed = false },
            new AddColumn<UUID>("snapshotuuid") { IsNullAllowed = false },
            new AddColumn<string>("simname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<Vector3>("posglobal") { IsNullAllowed = false },
            new AddColumn<string>("parcelname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<uint>("classifiedflags") { IsNullAllowed = false },
            new AddColumn<int>("priceforlisting") { IsNullAllowed = false },
            new PrimaryKeyInfo("classifieduuid"),
            new NamedKeyInfo("creatoruuid_index", new string[] { "creatoruuid" }),

            new SqlTable("usernotes"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false },
            new AddColumn<UUID>("targetuuid") { IsNullAllowed = false },
            new AddColumn<string>("notes") { IsNullAllowed = false },
            new PrimaryKeyInfo("useruuid", "targetuuid"),
            new NamedKeyInfo("useruuid", "useruuid"),

            new SqlTable("userpicks"),
            new AddColumn<UUID>("pickuuid") { IsNullAllowed = false },
            new AddColumn<UUID>("creatoruuid") { IsNullAllowed = false },
            new AddColumn<bool>("toppick") { IsNullAllowed = false },
            new AddColumn<ParcelID>("parceluuid") { IsNullAllowed = false },
            new AddColumn<string>("name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("description") { IsNullAllowed = false },
            new AddColumn<UUID>("snapshotuuid") { IsNullAllowed = false },
            new AddColumn<string>("parcelname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("originalname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("simname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<Vector3>("posglobal") { IsNullAllowed = false },
            new AddColumn<int>("sortorder") { IsNullAllowed = false },
            new AddColumn<bool>("enabled") { IsNullAllowed = false },
            new PrimaryKeyInfo("pickuuid"),
            new NamedKeyInfo("creatoruuid", "creatoruuid"),

            new SqlTable("userprofile"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false },
            new AddColumn<UUID>("profilePartner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("profileAllowPublish") { IsNullAllowed = false },
            new AddColumn<bool>("profileMaturePublish") { IsNullAllowed = false },
            new AddColumn<string>("profileURL") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<uint>("profileWantToMask") { IsNullAllowed = false },
            new AddColumn<string>("profileWantToText"),
            new AddColumn<uint>("profileSkillsMask") { IsNullAllowed = false },
            new AddColumn<string>("profileSkillsText"),
            new AddColumn<string>("profileLanguages"),
            new AddColumn<UUID>("profileImage") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("profileAboutText"),
            new AddColumn<UUID>("profileFirstImage") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("profileFirstText"),
            new PrimaryKeyInfo("useruuid"),

            new SqlTable("usersettings"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("imviaemail") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("visible") { IsNullAllowed = false, Default = true },
            new PrimaryKeyInfo("useruuid")
        };
    }
}
