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
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using SilverSim.Types.Experience;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.ExperienceService
{
    public static class MySQLExperienceExtensionMethods
    {
        public static ExperienceInfo ToExperienceInfo(this SQLiteDataReader reader) => new ExperienceInfo
        {
            ID = new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI")),
            Description = (string)reader["Description"],
            Properties = reader.GetEnum<ExperiencePropertyFlags>("Properties"),
            Owner = reader.GetUGUI("Owner"),
            Creator = reader.GetUGUI("Creator"),
            Group = reader.GetUGI("Group"),
            Maturity = reader.GetEnum<RegionAccess>("Maturity"),
            Marketplace = (string)reader["Marketplace"],
            LogoID = reader.GetUUID("LogoID"),
            SlUrl = (string)reader["SlUrl"]
        };
    }

    [Description("SQLite Experience Backend")]
    [PluginName("Experience")]
    public sealed partial class SQLiteExperienceService : ExperienceServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL EXPERIENCE");
        private string m_HomeURI;

        public SQLiteExperienceService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HomeURI = loader.HomeURI;
        }

        public override IExperiencePermissionsInterface Permissions => this;

        public override IExperienceAdminInterface Admins => this;

        public override IExperienceKeyValueInterface KeyValueStore => this;

        public override void Add(ExperienceInfo info)
        {
            if (info.ID.HomeURI == null)
            {
                info.ID.HomeURI = new Uri(m_HomeURI);
            }
            var vals = new Dictionary<string, object>
            {
                { "ID", info.ID.ID },
                { "Name", info.ID.ExperienceName },
                { "HomeURI", info.ID.HomeURI },
                { "Description", info.Description },
                { "Properties", info.Properties },
                { "Owner", info.Owner },
                { "Creator", info.Creator },
                { "Group", info.Group },
                { "Maturity", info.Maturity },
                { "Marketplace", info.Marketplace },
                { "LogoID", info.LogoID },
                { "SlUrl", info.SlUrl }
            };

            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("experiences", vals);
            }
        }

        public override List<UEI> FindExperienceByName(string query)
        {
            var result = new List<UEI>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT ID, Name, HomeURI FROM experiences WHERE Name LIKE @name", conn))
                {
                    cmd.Parameters.AddParameter("@name", "%" + query + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI")));
                        }
                    }
                }
            }
            return result;
        }

        public override List<ExperienceInfo> FindExperienceInfoByName(string query)
        {
            var result = new List<ExperienceInfo>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM experiences WHERE Name LIKE @name", conn))
                {
                    cmd.Parameters.AddParameter("@name", "%" + query + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(reader.ToExperienceInfo());
                        }
                    }
                }
            }
            return result;
        }

        public override List<UEI> GetCreatorExperiences(UGUI creator)
        {
            var result = new List<UEI>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Creator, ID, Name, HomeURI FROM experiences WHERE Creator LIKE @creator", conn))
                {
                    cmd.Parameters.AddParameter("@creator", creator.ID.ToString() + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetUGUI("Creator").EqualsGrid(creator))
                            {
                                result.Add(new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI")));
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override List<UEI> GetGroupExperiences(UGI group)
        {
            var result = new List<UEI>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Group, ID, Name, HomeURI FROM experiences WHERE Group LIKE @group", conn))
                {
                    cmd.Parameters.AddParameter("@group", group.ID.ToString() + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetUGI("Group").Equals(group))
                            {
                                result.Add(new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI")));
                            }
                        }
                    }
                }
            }
            return result;
        }

        public override List<UEI> GetOwnerExperiences(UGUI owner)
        {
            var result = new List<UEI>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Owner, ID, Name, HomeURI FROM experiences WHERE Owner LIKE @owner", conn))
                {
                    cmd.Parameters.AddParameter("@owner", owner.ID.ToString() + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetUGUI("Owner").EqualsGrid(owner))
                            {
                                result.Add(new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI")));
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static readonly string[] m_RemoveFromTables = new string[] { "experiencekeyvalues", "experienceadmins", "experienceusers" };
        public override bool Remove(UGUI requestingAgent, UEI id)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                return conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT Owner FROM experiences WHERE ID = @experienceid LIMIT 1", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@experienceid", id.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return false;
                            }

                            if (!reader.GetUGUI("Owner").EqualsGrid(requestingAgent))
                            {
                                return false;
                            }
                        }
                    }

                    foreach (string table in m_RemoveFromTables)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM " + table + " WHERE ExperienceID = @experienceid", conn))
                        {
                            cmd.Parameters.AddParameter("@experienceid", id.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (var cmd = new SQLiteCommand("DELETE FROM experiences WHERE ID = @experienceid", conn))
                    {
                        cmd.Parameters.AddParameter("@experienceid", id.ID);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                });
            }
        }

        void IUserAccountDeleteServiceInterface.Remove(UUID accountID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM experienceadmins WHERE Admin LIKE @admin", conn))
                {
                    cmd.Parameters.AddParameter("@admin", accountID.ToString() + "%");
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand("DELETE FROM experienceusers WHERE User LIKE @user", conn))
                {
                    cmd.Parameters.AddParameter("@user", accountID.ToString() + "%");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool TryGetValue(UUID experienceID, out UEI uei)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM experiences WHERE ID = @id LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@id", experienceID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uei = new UEI(reader.GetUUID("ID"), (string)reader["Name"], reader.GetUri("HomeURI"));
                            return true;
                        }
                    }
                }
            }
            uei = default(UEI);
            return false;
        }

        public override bool TryGetValue(UEI experienceID, out ExperienceInfo experienceInfo)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM experiences WHERE ID = @id LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@id", experienceID.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            experienceInfo = reader.ToExperienceInfo();
                            return true;
                        }
                    }
                }
            }
            experienceInfo = default(ExperienceInfo);
            return false;
        }

        public override void Update(UGUI requestingAgent, ExperienceInfo info)
        {
            var vals = new Dictionary<string, object>
            {
                { "Name", info.ID.ExperienceName },
                { "HomeURI", info.ID.HomeURI },
                { "Description", info.Description },
                { "Properties", info.Properties },
                { "Owner", info.Owner },
                { "Group", info.Group },
                { "Maturity", info.Maturity },
                { "Marketplace", info.Marketplace },
                { "LogoID", info.LogoID },
                { "SlUrl", info.SlUrl }
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction((transaction) =>
                {
                    bool isallowed = false;
                    using (var cmd = new SQLiteCommand("SELECT Admin FROM experienceadmins WHERE ExperienceID = @experienceid AND Admin LIKE @admin", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@experienceid", info.ID.ID);
                        cmd.Parameters.AddParameter("@admin", requestingAgent.ID.ToString() + "%");
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetUGUI("Admin").EqualsGrid(requestingAgent))
                                {
                                    isallowed = true;
                                }
                            }
                        }
                    }
                    if (!isallowed)
                    {
                        using (var cmd = new SQLiteCommand("SELECT Owner FROM experiences WHERE ID = @id LIMIT 1", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@id", info.ID.ID);
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    isallowed = reader.GetUGUI("Owner").EqualsGrid(requestingAgent);
                                }
                            }
                        }
                    }
                    if (!isallowed)
                    {
                        throw new InvalidOperationException("requesting agent is not allowed to edit experience");
                    }
                    conn.UpdateSet("experiences", vals, "ID = \"" + info.ID.ID.ToString() + "\"", transaction);
                });
            }
        }

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
                conn.MigrateTables(m_Migrations, m_Log);
            }
        }

        private static readonly IMigrationElement[] m_Migrations = new IMigrationElement[]
        {
            new SqlTable("experiences"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 255, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 255, Default = string.Empty },
            new AddColumn<ExperiencePropertyFlags>("Properties") { IsNullAllowed = false, Default = ExperiencePropertyFlags.None },
            new AddColumn<UGUI>("Owner") { IsNullAllowed = false, Default = UGUI.Unknown },
            new AddColumn<UGUI>("Creator") { IsNullAllowed = false, Default = UGUI.Unknown },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UGI.Unknown },
            new AddColumn<RegionAccess>("Maturity") { IsNullAllowed = false, Default = RegionAccess.Mature },
            new AddColumn<string>("Marketplace") { IsNullAllowed = false, Cardinality = 255, Default = string.Empty },
            new AddColumn<UUID>("LogoID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("SlUrl") {IsNullAllowed = false, Cardinality = 255, Default = string.Empty },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("NameKey", "Name"),
            new TableRevision(2),
            new AddColumn<Uri>("HomeURI"),

            new SqlTable("experienceadmins"),
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false },
            new AddColumn<UGUI>("Admin") { IsNullAllowed = false },
            new PrimaryKeyInfo("ExperienceID", "Admin"),
            new NamedKeyInfo("ExperienceID", "ExperienceID"),

            new SqlTable("experienceusers"),
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false },
            new AddColumn<UGUI>("User") { IsNullAllowed = false },
            new PrimaryKeyInfo("ExperienceID", "User"),
            new NamedKeyInfo("ExperienceID", "ExperienceID"),
            new NamedKeyInfo("User", "User"),
            new TableRevision(2),
            new AddColumn<bool>("IsAllowed") { IsNullAllowed = false, Default = false },

            new SqlTable("experiencekeyvalues"),
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false },
            new AddColumn<string>("Key") { IsNullAllowed = false, Cardinality = 255 },
            new AddColumn<string>("Value"),
            new PrimaryKeyInfo("ExperienceID", "Key"),
            new NamedKeyInfo("ExperienceID", "ExperienceID")
        };
    }
}
