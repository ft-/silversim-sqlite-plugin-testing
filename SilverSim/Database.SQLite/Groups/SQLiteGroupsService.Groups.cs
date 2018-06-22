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
    public sealed partial class SQLiteGroupsService : IGroupsInterface
    {
        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, string groupName)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT GroupID FROM groups WHERE Name = @groupname LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupname", groupName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UGI group) => Groups.ContainsKey(requestingAgent, group.ID);

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UUID groupID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT GroupID FROM groups WHERE GroupID = @groupid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", groupID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        GroupInfo IGroupsInterface.Create(UGUI requestingAgent, GroupInfo group)
        {
            var vals = new Dictionary<string, object>
            {
                ["GroupID"] = group.ID.ID,
                ["Location"] = group.ID.HomeURI != null ? group.ID.HomeURI.ToString() : string.Empty,
                ["Name"] = group.ID.GroupName,
                ["Charter"] = group.Charter,
                ["InsigniaID"] = group.InsigniaID,
                ["FounderID"] = group.Founder.ID,
                ["MembershipFee"] = group.MembershipFee,
                ["OpenEnrollment"] = group.IsOpenEnrollment,
                ["ShowInList"] = group.IsShownInList,
                ["AllowPublish"] = group.IsAllowPublish,
                ["MaturePublish"] = group.IsMaturePublish,
                ["OwnerRoleID"] = group.OwnerRoleID
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("groups", vals);
            }
            return group;
        }

        void IGroupsInterface.Delete(UGUI requestingAgent, UGI group)
        {
            var tablenames = new string[] { "grouproles", "grouprolememberships", "groupnotices", "groupmemberships", "groupinvites", "groups" };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM activegroup WHERE ActiveGroupID = @groupid", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@groupid", group.ID);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (string table in tablenames)
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM " + table + " WHERE GroupID = @groupid", conn)
                        {
                            Transaction = transaction
                        })
                        {
                            cmd.Parameters.AddParameter("@groupid", group.ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UGUI requestingAgent, string query)
        {
            var groups = new List<DirGroupInfo>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT g.GroupID, g.Name, g.Location, " + GCountQuery + " FROM groups AS g WHERE g.Name LIKE @value", conn))
                {
                    cmd.Parameters.AddParameter("@value", "%" + query + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var info = new DirGroupInfo();
                            info.ID.ID = reader.GetUUID("GroupID");
                            info.ID.GroupName = (string)reader["Name"];
                            var uri = (string)reader["Location"];
                            if (!string.IsNullOrEmpty(uri))
                            {
                                info.ID.HomeURI = new Uri(uri, UriKind.Absolute);
                            }
                            info.MemberCount = (int)(long)reader["MemberCount"];
                            groups.Add(info);
                        }
                    }
                }
            }
            return groups;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            groupInfo = null;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT g.*, " + GCountQuery + " FROM groups AS g WHERE g.Name = @groupname LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupname", groupName);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            groupInfo = reader.ToGroupInfo();
                            groupInfo.Founder = ResolveName(groupInfo.Founder);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UGI group, out GroupInfo groupInfo)
        {
            groupInfo = null;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT g.*, " + GCountQuery + " FROM groups AS g WHERE g.GroupID = @groupid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            groupInfo = reader.ToGroupInfo();
                            groupInfo.Founder = ResolveName(groupInfo.Founder);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UUID groupID, out UGI ugi)
        {
            ugi = default(UGI);
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Name, Location FROM groups WHERE GroupID = @groupid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", groupID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ugi = new UGI
                            {
                                ID = groupID
                            };
                            var uri = (string)reader["Location"];
                            if (!string.IsNullOrEmpty(uri))
                            {
                                ugi.HomeURI = new Uri(uri, UriKind.Absolute);
                            }
                            ugi.GroupName = (string)reader["Name"];
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        GroupInfo IGroupsInterface.Update(UGUI requestingAgent, GroupInfo group)
        {
            var vals = new Dictionary<string, object>
            {
                ["Charter"] = group.Charter,
                ["InsigniaID"] = group.InsigniaID,
                ["FounderID"] = group.Founder.ID,
                ["MembershipFee"] = group.MembershipFee,
                ["OpenEnrollment"] = group.IsOpenEnrollment,
                ["ShowInList"] = group.IsShownInList,
                ["AllowPublish"] = group.IsAllowPublish,
                ["MaturePublish"] = group.IsMaturePublish,
                ["OwnerRoleID"] = group.OwnerRoleID
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.UpdateSet("groups", vals, "GroupID = \"" + group.ID.ID.ToString() + "\"");
            }
            return group;
        }
    }
}
