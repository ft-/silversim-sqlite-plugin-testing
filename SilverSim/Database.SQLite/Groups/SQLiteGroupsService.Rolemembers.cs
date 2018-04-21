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
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface.IGroupRolemembersInterface
    {
        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                var rolemembers = new List<GroupRolemember>();

                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID = r.GroupID AND rm.RoleID = r.RoleID WHERE rm.GroupID = @groupid", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRolemember grolemem = reader.ToGroupRolemember();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                rolemembers.Add(grolemem);
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("SELECT * FROM groupmemberships WHERE rm.PrincipalID = @principalid", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRole groupRole;
                                if (Roles.TryGetValue(requestingAgent, group, UUID.Zero, out groupRole))
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolememberEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }

                return rolemembers;
            }
        }

        List<GroupRolemembership> IGroupRolemembersInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                var rolemembers = new List<GroupRolemembership>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT rm.*, r.Powers, r.Title FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID = r.GroupID AND rm.RoleID = r.RoleID WHERE rm.PrincipalID = @principalid", conn))
                    {
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRolemembership grolemem = reader.ToGroupRolemembership();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                rolemembers.Add(grolemem);
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("SELECT * FROM groupmemberships WHERE rm.PrincipalID = @principalid", conn))
                    {
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var group = new UGI(reader.GetUUID("GroupID"));
                                GroupRole groupRole;
                                if (Roles.TryGetValue(requestingAgent, group, UUID.Zero, out groupRole))
                                {
                                    GroupRolemembership grolemem = reader.ToGroupRolemembershipEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    grolemem.GroupTitle = groupRole.Title;
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }

                return rolemembers;
            }
        }

        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                var rolemembers = new List<GroupRolemember>();

                if (UUID.Zero == roleID)
                {
                    GroupRole groupRole;
                    if (!Roles.TryGetValue(requestingAgent, group, roleID, out groupRole))
                    {
                        return rolemembers;
                    }

                    using (var conn = new SQLiteConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("SELECT * FROM groupmemberships WHERE rm.GroupID = @groupid", conn))
                        {
                            cmd.Parameters.AddParameter("@groupid", group.ID);
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolememberEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (var conn = new SQLiteConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID = r.GroupID AND rm.RoleID = r.RoleID WHERE rm.GroupID = @groupid AND rm.RoleID = @roleid", conn))
                        {
                            cmd.Parameters.AddParameter("@groupid", group.ID);
                            cmd.Parameters.AddParameter("@roleid", roleID);
                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolemember();
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }
                return rolemembers;
            }
        }

        GroupRolemember IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal]
        {
            get
            {
                GroupRolemember rolemem;
                if (!Rolemembers.TryGetValue(requestingAgent, group, roleID, principal, out rolemem))
                {
                    throw new KeyNotFoundException();
                }
                return rolemem;
            }
        }

        void IGroupRolemembersInterface.Add(UGUI requestingAgent, GroupRolemember rolemember)
        {
            if (rolemember.RoleID == UUID.Zero)
            {
                return; /* ignore those */
            }
            var vals = new Dictionary<string, object>
            {
                ["GroupID"] = rolemember.Group.ID,
                ["RoleID"] = rolemember.RoleID,
                ["PrincipalID"] = rolemember.Principal.ID
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("grouprolememberships", vals);
            }
        }

        bool IGroupRolemembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal)
        {
            if (UUID.Zero == roleID)
            {
                return Members.ContainsKey(requestingAgent, group, principal);
            }
            else
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT rm.GroupID FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID = r.GroupID AND rm.RoleID = r.RoleID WHERE rm.GroupID = @groupid AND rm.RoleID = @roleid and rm.PrincipalID = @principalid LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        cmd.Parameters.AddParameter("@roleid", roleID);
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
        }

        void IGroupRolemembersInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal)
        {
            if (UUID.Zero == roleID)
            {
                throw new NotSupportedException();
            }
            else
            {
                var tablenames = new string[] { "groupinvites", "grouprolememberships" };

                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.InsideTransaction((transaction) =>
                    {
                        using (var cmd = new SQLiteCommand("UPDATE groupmemberships SET SelectedRoleID=@zeroid WHERE SelectedRoleID = @roleid AND GroupID = @groupid AND PrincipalID = @principalid", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@zeroid", UUID.Zero);
                            cmd.Parameters.AddParameter("@principalid", principal.ID);
                            cmd.Parameters.AddParameter("@groupid", group.ID);
                            cmd.Parameters.AddParameter("@roleid", roleID);
                            cmd.ExecuteNonQuery();
                        }

                        foreach (string table in tablenames)
                        {
                            using (var cmd = new SQLiteCommand("DELETE FROM " + table + " WHERE GroupID = @groupid AND RoleID = @roleid AND PrincipalID = @principalid", conn)
                            {
                                Transaction = transaction
                            })
                            {
                                cmd.Parameters.AddParameter("@principalid", principal.ID);
                                cmd.Parameters.AddParameter("@groupid", group.ID);
                                cmd.Parameters.AddParameter("@roleid", roleID);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                }
            }
        }

        bool IGroupRolemembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal, out GroupRolemember grolemem)
        {
            grolemem = null;
            if (UUID.Zero == roleID)
            {
                GroupMember gmem;
                GroupRole role;
                if (Members.TryGetValue(requestingAgent, group, principal, out gmem) &&
                    Roles.TryGetValue(requestingAgent, group, UUID.Zero, out role))
                {
                    grolemem = new GroupRolemember
                    {
                        Powers = role.Powers,
                        Principal = ResolveName(principal),
                        RoleID = UUID.Zero,
                        Group = gmem.Group
                    };
                    return true;
                }
            }
            else
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID = r.GroupID AND rm.RoleID = r.RoleID WHERE rm.GroupID = @groupid AND rm.RoleID = @roleid and rm.PrincipalID = @principalid LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        cmd.Parameters.AddParameter("@roleid", roleID);
                        cmd.Parameters.AddParameter("@principalid", principal.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                grolemem = reader.ToGroupRolemember();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
