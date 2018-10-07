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
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using System;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.UserAccounts
{
    public static class SQLiteUserAccountExtensionMethods
    {
        public static UserAccount ToUserAccount(this SQLiteDataReader reader, Uri homeURI)
        {
            var info = new UserAccount();
            string gkUri;
            UserRegionData regData;

            info.Principal.ID = reader.GetUUID("ID");
            info.Principal.FirstName = (string)reader["FirstName"];
            info.Principal.LastName = (string)reader["LastName"];
            info.Principal.HomeURI = homeURI;
            info.Principal.IsAuthoritative = true;
            info.ScopeID = reader.GetUUID("ScopeID");
            info.Email = (string)reader["Email"];
            info.Created = reader.GetDate("Created");
            info.UserLevel = (int)(long)reader["UserLevel"];
            info.UserFlags = reader.GetEnum<UserFlags>("UserFlags");
            info.UserTitle = (string)reader["UserTitle"];
            info.IsLocalToGrid = true;
            info.IsEverLoggedIn = reader.GetBool("IsEverLoggedIn");

            gkUri = (string)reader["LastGatekeeperURI"];
            regData = new UserRegionData
            {
                RegionID = reader.GetUUID("LastRegionID"),
                Position = reader.GetVector3("LastPosition"),
                LookAt = reader.GetVector3("LastLookAt"),
            };
            if (!string.IsNullOrEmpty(gkUri))
            {
                regData.GatekeeperURI = new URI(gkUri);
            }
            if (regData.RegionID != UUID.Zero)
            {
                info.LastRegion = regData;
            }

            gkUri = (string)reader["HomeGatekeeperURI"];
            regData = new UserRegionData
            {
                RegionID = reader.GetUUID("HomeRegionID"),
                Position = reader.GetVector3("HomePosition"),
                LookAt = reader.GetVector3("HomeLookAt"),
            };
            if (!string.IsNullOrEmpty(gkUri))
            {
                regData.GatekeeperURI = new URI(gkUri);
            }
            if (regData.RegionID != UUID.Zero)
            {
                info.HomeRegion = regData;
            }

            return info;
        }
    }
}
