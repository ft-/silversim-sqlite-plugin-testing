﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Types.Experience;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage : ISimulationDataRegionExperiencesStorageInterface
    {
        List<RegionExperienceInfo> IRegionExperienceList.this[UUID regionID]
        {
            get
            {
                List<RegionExperienceInfo> result = new List<RegionExperienceInfo>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM regionexperiences WHERE RegionID = @regionid", conn))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new RegionExperienceInfo
                                {
                                    ExperienceID = new UEI(reader.GetUUID("ExperienceID")),
                                    RegionID = reader.GetUUID("RegionID"),
                                    IsAllowed = reader.GetBool("IsAllowed")
                                });
                            }
                        }
                    }
                }
                return result;
            }
        }

        RegionExperienceInfo IRegionExperienceList.this[UUID regionID, UEI experienceID]
        {
            get
            {
                RegionExperienceInfo info;
                if (!RegionExperiences.TryGetValue(regionID, experienceID, out info))
                {
                    throw new KeyNotFoundException();
                }
                return info;
            }
        }

        bool IRegionExperienceList.Remove(UUID regionID, UEI experienceID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM regionexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID.ID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        void ISimulationDataRegionExperiencesStorageInterface.RemoveRegion(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM regionexperiences WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        void IRegionExperienceList.Store(RegionExperienceInfo info)
        {
            var vals = new Dictionary<string, object>
            {
                ["RegionID"] = info.RegionID,
                ["ExperienceID"] = info.ExperienceID.ID,
                ["IsAllowed"] = info.IsAllowed
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.ReplaceInto("regionexperiences", vals);
            }
        }

        bool IRegionExperienceList.TryGetValue(UUID regionID, UEI experienceID, out RegionExperienceInfo info)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM regionexperiences WHERE RegionID = @regionid AND ExperienceID = @experienceid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            info = new RegionExperienceInfo
                            {
                                ExperienceID = new UEI(reader.GetUUID("ExperienceID")),
                                RegionID = reader.GetUUID("RegionID"),
                                IsAllowed = reader.GetBool("IsAllowed"),
                            };
                            return true;
                        }
                    }
                }
            }
            info = default(RegionExperienceInfo);
            return false;
        }
    }
}
