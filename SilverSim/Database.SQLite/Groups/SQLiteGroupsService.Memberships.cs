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

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface.IGroupMembershipsInterface
    {
        private GroupMembership MembershipFromReader(SQLiteDataReader reader, UUI requestingAgent) => new GroupMembership
        {
            IsAcceptNotices = reader.GetBool("AcceptNotices"),
            Contribution = (int)(long)reader["Contribution"],
            GroupInsigniaID = reader.GetUUID("InsigniaID"),
            GroupPowers = reader.GetEnum<GroupPowers>("RolePowers"),
            GroupTitle = (string)reader["RoleTitle"],
            IsListInProfile = reader.GetBool("ListInProfile"),
            Group = ResolveName(requestingAgent, new UGI(reader.GetUUID("GroupID"))),
            Principal = ResolveName(new UUI(reader.GetUUID("PrincipalID"))),

            IsAllowPublish = reader.GetBool("AllowPublish"),
            Charter = (string)reader["Charter"],
            ActiveRoleID = reader.GetUUID("ActiveRoleID"),
            Founder = ResolveName(new UUI(reader.GetUUID("FounderID"))),
            AccessToken = (string)reader["AccessToken"],
            IsMaturePublish = reader.GetBool("MaturePublish"),
            IsOpenEnrollment = reader.GetBool("OpenEnrollment"),
            MembershipFee = (int)(long)reader["MembershipFee"],
            IsShownInList = reader.GetBool("ShowInList")
        };

        List<GroupMembership> IGroupMembershipsInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                var memberships = new List<GroupMembership>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(
                            "SELECT g.*, m.AccessToken as AccessToken, m.SelectedRoleID AS ActiveRoleID, m.PrincipalID, m.SelectedRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " +
                            "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                            RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                            "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                            "WHERE m.PrincipalID = @principalid", conn))
                    {
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                memberships.Add(MembershipFromReader(reader, requestingAgent));
                            }
                        }
                    }
                }
                return memberships;
            }
        }

        GroupMembership IGroupMembershipsInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                GroupMembership gmem;
                if (!Memberships.TryGetValue(requestingAgent, group, principal, out gmem))
                {
                    throw new KeyNotFoundException();
                }
                return gmem;
            }
        }

        bool IGroupMembershipsInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                        "SELECT g.*, m.PrincipalID, m.SelectedRoleID AS ActiveRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " +
                        "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                        RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                        "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                        "WHERE m.PrincipalID = @principalid", conn))
                {
                    cmd.Parameters.AddParameter("@principalid", principal.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        bool IGroupMembershipsInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMembership gmem)
        {
            gmem = null;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                        "SELECT g.*, m.PrincipalID, m.SelectedRoleID AS ActiveRoleID, m.Contribution, m.ListInProfile, m.AcceptNotices, m.AccessToken, " +
                        "r.RoleID, r.Name AS RoleName, r.Description AS RoleDescription, r.Title as RoleTitle, r.Powers as RolePowers, " +
                        RCountQuery + "," + MCountQuery + " FROM (groupmemberships AS m INNER JOIN groups AS g ON m.GroupID = g.GroupID) " +
                        "INNER JOIN grouproles AS r ON m.SelectedRoleID = r.RoleID " +
                        "WHERE m.PrincipalID = @principalid", conn))
                {
                    cmd.Parameters.AddParameter("@principalid", principal.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            gmem = MembershipFromReader(reader, requestingAgent);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
