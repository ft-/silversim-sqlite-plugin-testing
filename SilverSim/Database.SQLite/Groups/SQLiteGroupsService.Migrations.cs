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

using SilverSim.Database.SQLite._Migration;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    public sealed partial class SQLiteGroupsService
    {
        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("groups"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("Location") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Charter") { IsLong = true },
            new AddColumn<UUID>("InsigniaID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("FounderID") { IsNullAllowed = false },
            new AddColumn<int>("MembershipFee") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("OpenEnrollment") {IsNullAllowed = false, Default = false },
            new AddColumn<bool>("ShowInList") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowPublish") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("MaturePublish") { IsNullAllowed = false, Default = false },
            new AddColumn<UUID>("OwnerRoleID") { IsNullAllowed = false },
            new PrimaryKeyInfo("GroupID"),
            new NamedKeyInfo("Name", "Name") {IsUnique = true },

            new SqlTable("groupinvites"),
            new AddColumn<UUID>("InviteID") { IsNullAllowed = false },
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false },
            new PrimaryKeyInfo("InviteID"),
            new NamedKeyInfo("PrincipalGroup", "GroupID", "PrincipalID") { IsUnique = true },

            new SqlTable("groupmemberships"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false },
            new AddColumn<UUID>("SelectedRoleID") { IsNullAllowed = false },
            new AddColumn<int>("Contribution") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("ListInProfile") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("AcceptNotices") { IsNullAllowed = false, Default = true },
            new AddColumn<string>("AccessToken") { Cardinality = 36, IsFixed = true },
            new PrimaryKeyInfo("GroupID", "PrincipalID"),
            new NamedKeyInfo("Principal", "PrincipalID"),
            new NamedKeyInfo("GroupID", "GroupID"),

            new SqlTable("groupnotices"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("NoticeID") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false },
            new AddColumn<string>("FromName") { Cardinality = 255 },
            new AddColumn<string>("Subject") { Cardinality = 255 },
            new AddColumn<string>("Message") { IsNullAllowed = false },
            new AddColumn<bool>("HasAttachment") { IsNullAllowed = false, Default = false },
            new AddColumn<AssetType>("AttachmentType") { IsNullAllowed = false , Default = AssetType.Unknown },
            new AddColumn<string>("AssetName") { IsNullAllowed = false, Default = string.Empty, Cardinality = 128 },
            new AddColumn<UUID>("AttachmentItemID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("AttachmentOwnerID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("NoticeID"),
            new NamedKeyInfo("GroupID", "GroupID"),
            new NamedKeyInfo("Timestamp", "Timestamp"),

            new SqlTable("activegroup"),
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false },
            new AddColumn<UUID>("ActiveGroupID") { IsNullAllowed = false },
            new PrimaryKeyInfo("PrincipalID"),

            new SqlTable("grouprolememberships"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false },
            new PrimaryKeyInfo("GroupID", "RoleID", "PrincipalID"),
            new NamedKeyInfo("Principal", "PrincipalID"),
            new NamedKeyInfo("RoleID", "RoleID"),

            new SqlTable("grouproles"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("Description") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Title") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<ulong>("Powers") { IsNullAllowed = false, Default = (ulong)0 }
        };

        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }
    }
}
