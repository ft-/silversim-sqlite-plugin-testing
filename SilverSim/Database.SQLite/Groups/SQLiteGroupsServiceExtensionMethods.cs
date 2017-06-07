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

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Groups;
using System;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    public static class SQLiteGroupsServiceExtensionMethods
    {
        public static GroupInfo ToGroupInfo(this SQLiteDataReader reader, string memberCount = "MemberCount")
        {
            var info = new GroupInfo();
            info.ID.ID = reader.GetUUID("GroupID");
            string uri = (string)reader["Location"];
            if (!string.IsNullOrEmpty(uri))
            {
                info.ID.HomeURI = new Uri(uri, UriKind.Absolute);
            }
            info.ID.GroupName = (string)reader["Name"];
            info.Charter = (string)reader["Charter"];
            info.InsigniaID = reader.GetUUID("InsigniaID");
            info.Founder.ID = reader.GetUUID("FounderID");
            info.MembershipFee = (int)(long)reader["MembershipFee"];
            info.IsOpenEnrollment = reader.GetBool("OpenEnrollment");
            info.IsShownInList = reader.GetBool("ShowInList");
            info.IsAllowPublish = reader.GetBool("AllowPublish");
            info.IsMaturePublish = reader.GetBool("MaturePublish");
            info.OwnerRoleID = reader.GetUUID("OwnerRoleID");
            info.MemberCount = (int)(long)reader[memberCount];
            info.RoleCount = (int)(long)reader["RoleCount"];

            return info;
        }

        public static GroupRole ToGroupRole(this SQLiteDataReader reader, string prefix = "")
        {
            var role = new GroupRole()
            {
                Group = new UGI(reader.GetUUID("GroupID")),
                ID = reader.GetUUID("RoleID"),
                Name = (string)reader[prefix + "Name"],
                Description = (string)reader[prefix + "Description"],
                Title = (string)reader[prefix + "Title"],
                Powers = reader.GetEnum<GroupPowers>(prefix + "Powers")
            };
            if (role.ID == UUID.Zero)
            {
                role.Members = (uint)(long)reader["GroupMembers"];
            }
            else
            {
                role.Members = (uint)(long)reader["RoleMembers"];
            }

            return role;
        }

        public static GroupMember ToGroupMember(this SQLiteDataReader reader) => new GroupMember()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            SelectedRoleID = reader.GetUUID("SelectedRoleID"),
            Contribution = (int)(long)reader["Contribution"],
            IsListInProfile = reader.GetBool("ListInProfile"),
            IsAcceptNotices = reader.GetBool("AcceptNotices"),
            AccessToken = (string)reader["AccessToken"]
        };

        public static GroupRolemember ToGroupRolemember(this SQLiteDataReader reader) => new GroupRolemember()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            RoleID = reader.GetUUID("RoleID"),
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            Powers = reader.GetEnum<GroupPowers>("Powers")
        };

        public static GroupRolemember ToGroupRolememberEveryone(this SQLiteDataReader reader, GroupPowers powers) => new GroupRolemember()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            RoleID = UUID.Zero,
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            Powers = powers
        };

        public static GroupRolemembership ToGroupRolemembership(this SQLiteDataReader reader) => new GroupRolemembership()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            RoleID = reader.GetUUID("RoleID"),
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            Powers = reader.GetEnum<GroupPowers>("Powers"),
            GroupTitle = (string)reader["Title"]
        };

        public static GroupRolemembership ToGroupRolemembershipEveryone(this SQLiteDataReader reader, GroupPowers powers) => new GroupRolemembership()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            RoleID = UUID.Zero,
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            Powers = powers
        };

        public static GroupInvite ToGroupInvite(this SQLiteDataReader reader) => new GroupInvite()
        {
            ID = reader.GetUUID("InviteID"),
            Group = new UGI(reader.GetUUID("GroupID")),
            RoleID = reader.GetUUID("RoleID"),
            Principal = new UUI(reader.GetUUID("PrincipalID")),
            Timestamp = reader.GetDate("Timestamp")
        };

        public static GroupNotice ToGroupNotice(this SQLiteDataReader reader) => new GroupNotice()
        {
            Group = new UGI(reader.GetUUID("GroupID")),
            ID = reader.GetUUID("NoticeID"),
            Timestamp = reader.GetDate("Timestamp"),
            FromName = (string)reader["FromName"],
            Subject = (string)reader["Subject"],
            Message = (string)reader["Message"],
            HasAttachment = reader.GetBool("HasAttachment"),
            AttachmentType = reader.GetEnum<AssetType>("AttachmentType"),
            AttachmentName = (string)reader["AttachmentName"],
            AttachmentItemID = reader.GetUUID("AttachmentItemID"),
            AttachmentOwner = new UUI(reader.GetUUID("AttachmentOwnerID"))
        };
    }
}
