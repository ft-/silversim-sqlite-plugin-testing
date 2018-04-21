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
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    [Description("SQLite Groups Backend")]
    [PluginName("Groups")]
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE GROUPS SERVICE");
        private AggregatingAvatarNameService m_AvatarNameService;

        private const string GCountQuery = "(SELECT COUNT(m.PrincipalID) FROM groupmemberships AS m WHERE m.GroupID = g.GroupID) AS MemberCount," +
                                    "(SELECT COUNT(r.RoleID) FROM grouproles AS r WHERE r.GroupID = g.GroupID) AS RoleCount";

        private const string MCountQuery = "(SELECT COUNT(xr.RoleID) FROM grouproles AS xr WHERE xr.GroupID = g.GroupID) AS RoleCount";

        private const string RCountQuery = "(SELECT COUNT(xrm.PrincipalID) FROM grouprolememberships AS xrm WHERE xrm.RoleID = r.RoleID AND xrm.GroupID = r.GroupID) AS RoleMembers," +
                                    "(SELECT COUNT(xm.PrincipalID) FROM groupmemberships AS xm WHERE xm.GroupID = r.GroupID) AS GroupMembers";

        private UGUI ResolveName(UGUI uui)
        {
            UGUI resultuui;
            if (m_AvatarNameService.TryGetValue(uui, out resultuui))
            {
                return resultuui;
            }
            return uui;
        }

        private UGI ResolveName(UGUI requestingAgent, UGI group)
        {
            UGI resolved;
            return Groups.TryGetValue(requestingAgent, group.ID, out resolved) ? resolved : group;
        }

        public override IGroupSelectInterface ActiveGroup => this;

        public override IActiveGroupMembershipInterface ActiveMembership => this;

        public override IGroupsInterface Groups => this;

        public override IGroupInvitesInterface Invites => this;

        public override IGroupMembersInterface Members => this;

        public override IGroupMembershipsInterface Memberships => this;

        public override IGroupNoticesInterface Notices => this;

        public override IGroupRolemembersInterface Rolemembers => this;

        public override IGroupRolesInterface Roles => this;

        private bool TryGetGroupRoleRights(UGUI requestingAgent, UGI group, UUID roleID, out GroupPowers powers)
        {
            powers = GroupPowers.None;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Powers FROM grouproles AS r WHERE r.GroupID = @groupid AND r.RoleID = @grouproleid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    cmd.Parameters.AddParameter("@grouproleid", roleID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            powers = reader.GetEnum<GroupPowers>("Powers");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override GroupPowers GetAgentPowers(UGI group, UGUI agent)
        {
            if (!Members.ContainsKey(agent, group, agent))
            {
                return GroupPowers.None;
            }

            GroupPowers powers;
            if (!TryGetGroupRoleRights(agent, group, UUID.Zero, out powers))
            {
                return GroupPowers.None;
            }

            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT Powers FROM roles AS r INNER JOIN " +
                    "((grouprolemembers AS rm INNER JOIN groupmembers AS m ON rm.GroupID = m.GroupID AND rm.PrincipalID = m.PrincipalID) ON " +
                    "r.RoleID = rm.RoleID WHERE rm.GroupID = @groupid AND rm.PrincipalID = @principalid", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    cmd.Parameters.AddParameter("@principalid", agent.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powers |= reader.GetEnum<GroupPowers>("Powers");
                        }
                    }
                }
            }
            return powers;
        }

        public void Startup(ConfigurationLoader loader)
        {
            var avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
            foreach (string name in m_AvatarNameServiceNames.Trim().Split(','))
            {
                avatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(name));
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServices);
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM groupinvites WHERE PrincipalID = @id", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM groupmemberships WHERE PrincipalID = @id", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM activegroup WHERE PrincipalID = @id", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM grouprolememberships WHERE PrincipalID = @id", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@id", accountID);
                    }
                });
            }
        }

        private readonly string m_ConnectionString;
        private readonly string m_AvatarNameServiceNames;

        public SQLiteGroupsService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", "AvatarNameStorage");
        }
    }
}
