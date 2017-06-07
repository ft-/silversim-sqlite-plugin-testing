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

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Estate
{
    public sealed partial class SQLiteEstateService : IEstateRegionMapServiceInterface
    {
        List<UUID> IEstateRegionMapServiceInterface.this[uint estateID]
        {
            get
            {
                var regionList = new List<UUID>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT RegionID FROM estate_regionmap WHERE EstateID = @estateid", conn))
                    {
                        cmd.Parameters.AddParameter("@estateid", estateID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                regionList.Add(reader.GetUUID("RegionID"));
                            }
                        }
                    }
                }
                return regionList;
            }
        }

        bool IEstateRegionMapServiceInterface.TryGetValue(UUID regionID, out uint estateID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT EstateID FROM estate_regionmap WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estateID = (uint)(long)reader["EstateID"];
                            return true;
                        }
                    }
                }
            }

            estateID = 0;
            return false;
        }

        bool IEstateRegionMapServiceInterface.Remove(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM estate_regionmap WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        uint IEstateRegionMapServiceInterface.this[UUID regionID]
        {
            get
            {
                uint estateID;
                if (!RegionMap.TryGetValue(regionID, out estateID))
                {
                    throw new KeyNotFoundException();
                }
                return estateID;
            }
            set
            {
                var vals = new Dictionary<string, object>
                {
                    ["EstateID"] = value,
                    ["RegionID"] = regionID
                };
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("estate_regionmap", vals);
                }
            }
        }
    }
}
