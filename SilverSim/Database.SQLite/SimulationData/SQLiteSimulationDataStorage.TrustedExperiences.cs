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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage : ISimulationDataRegionTrustedExperiencesStorageInterface
    {
        List<UUID> IRegionTrustedExperienceList.this[UUID regionID]
        {
            get
            {
                var result = new List<UUID>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT ExperienceID FROM regiontrustedexperiences WHERE RegionID = @regionid", conn))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(reader.GetUUID("ExperienceID"));
                            }
                        }
                    }
                }
                return result;
            }
        }

        bool IRegionTrustedExperienceList.this[UUID regionID, UUID experienceID]
        {
            get
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT NULL FROM regiontrustedexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid", conn))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        cmd.Parameters.AddParameter("@experienceid", experienceID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
            set
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    if (value)
                    {
                        Dictionary<string, object> vals = new Dictionary<string, object>();
                        vals.Add("RegionID", regionID);
                        vals.Add("ExperienceID", experienceID);
                        conn.ReplaceInto("regiontrustedexperiences", vals);
                    }
                    else
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM regiontrustedexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid", conn))
                        {
                            cmd.Parameters.AddParameter("@regionid", regionID);
                            cmd.Parameters.AddParameter("@experienceid", experienceID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        bool IRegionTrustedExperienceList.Remove(UUID regionID, UUID experienceID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM regiontrustedexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        void ISimulationDataRegionTrustedExperiencesStorageInterface.RemoveRegion(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM regiontrustedexperiences WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        bool IRegionTrustedExperienceList.TryGetValue(UUID regionID, UUID experienceID, out bool trusted)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM regiontrustedexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        trusted = reader.Read();
                    }
                }
            }

            return true;
        }
    }
}
