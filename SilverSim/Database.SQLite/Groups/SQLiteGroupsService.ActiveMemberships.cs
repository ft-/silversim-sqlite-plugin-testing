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

namespace SilverSim.Database.SQLite.Groups
{
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface.IActiveGroupMembershipInterface
    {
        GroupActiveMembership IActiveGroupMembershipInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                GroupActiveMembership gam;
                if (!ActiveMembership.TryGetValue(requestingAgent, principal, out gam))
                {
                    throw new KeyNotFoundException();
                }
                return gam;
            }
        }

        bool IActiveGroupMembershipInterface.ContainsKey(UUI requestingAgent, UUI principal)
        {
            GroupActiveMembership gam;
            return ActiveMembership.TryGetValue(requestingAgent, principal, out gam);
        }

        bool IActiveGroupMembershipInterface.TryGetValue(UUI requestingAgent, UUI principal, out GroupActiveMembership gam)
        {
            gam = default(GroupActiveMembership);
            UGI activegroup;
            if (!ActiveGroup.TryGetValue(requestingAgent, principal, out activegroup))
            {
                return false;
            }
            GroupInfo group;
            if (!Groups.TryGetValue(requestingAgent, activegroup, out group))
            {
                return false;
            }

            GroupMember gmem;
            if (!Members.TryGetValue(requestingAgent, activegroup, principal, out gmem))
            {
                return false;
            }

            GroupRole role;
            if (!Roles.TryGetValue(requestingAgent, activegroup, gmem.SelectedRoleID, out role))
            {
                return false;
            }

            gam = new GroupActiveMembership
            {
                Group = group.ID,
                SelectedRoleID = gmem.SelectedRoleID,
                User = gmem.Principal
            };
            return true;
        }
    }
}
