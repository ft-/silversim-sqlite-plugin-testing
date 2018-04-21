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
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface.IGroupRolesInterface
    {
        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                var roles = new List<GroupRole>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT r.*," + RCountQuery + " FROM grouproles AS r WHERE r.GroupID = @groupid", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRole role = reader.ToGroupRole();
                                role.Group = ResolveName(requestingAgent, role.Group);
                                roles.Add(role);
                            }
                        }
                    }
                }
                return roles;
            }
        }

        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group, UGUI principal]
        {
            get
            {
                var roles = new List<GroupRole>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT r.*," + RCountQuery + " FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID AND r.GroupID AND rm.RoleID = r.RoleID WHERE r.GroupID = @groupid AND rm.PrincipalID = @principalid", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRole role = reader.ToGroupRole();
                                role.Group = ResolveName(requestingAgent, role.Group);
                                roles.Add(role);
                            }
                        }
                    }
                }
                return roles;
            }
        }

        GroupRole IGroupRolesInterface.this[UGUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                GroupRole role;
                if (!Roles.TryGetValue(requestingAgent, group, roleID, out role))
                {
                    throw new KeyNotFoundException();
                }
                return role;
            }
        }

        void IGroupRolesInterface.Add(UGUI requestingAgent, GroupRole role)
        {
            var vals = new Dictionary<string, object>
            {
                ["GroupID"] = role.Group.ID,
                ["RoleID"] = role.ID,
                ["Name"] = role.Name,
                ["Description"] = role.Description,
                ["Title"] = role.Title,
                ["Powers"] = role.Powers
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("grouproles", vals);
            }
        }

        bool IGroupRolesInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT r.GroupID FROM grouproles AS r WHERE r.GroupID = @groupid AND r.RoleID = @roleid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    cmd.Parameters.AddParameter("@roleid", roleID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        void IGroupRolesInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID)
        {
            var tablenames = new string[] { "groupinvites", "grouprolememberships", "grouproles" };

            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("UPDATE groupmemberships SET SelectedRoleID=@zeroid WHERE SelectedRoleID = @roleid", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@zeroid", UUID.Zero);
                        cmd.Parameters.AddParameter("@roleid", roleID);
                        cmd.ExecuteNonQuery();
                    }

                    foreach (string table in tablenames)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM " + table + " WHERE GroupID = @groupid AND RoleID = @roleid", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@groupid", group.ID);
                            cmd.Parameters.AddParameter("@roleid", roleID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        bool IGroupRolesInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            groupRole = null;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT r.*, " + RCountQuery + " FROM grouproles AS r WHERE r.GroupID = @groupid AND r.RoleID = @roleid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    cmd.Parameters.AddParameter("@roleid", roleID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            groupRole = reader.ToGroupRole();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        void IGroupRolesInterface.Update(UGUI requestingAgent, GroupRole role)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE grouproles SET Name=@name, Description=@description, Title=@title,Powers=@powers WHERE GroupID = @groupid AND RoleID = @roleid", conn))
                {
                    cmd.Parameters.AddParameter("@name", role.Name);
                    cmd.Parameters.AddParameter("@description", role.Description);
                    cmd.Parameters.AddParameter("@title", role.Title);
                    cmd.Parameters.AddParameter("@powers", role.Powers);
                    cmd.Parameters.AddParameter("@groupid", role.Group.ID);
                    cmd.Parameters.AddParameter("@roleid", role.ID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
