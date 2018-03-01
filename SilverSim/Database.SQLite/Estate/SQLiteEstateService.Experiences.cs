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
using SilverSim.Types.Experience;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Estate
{
    public sealed partial class SQLiteEstateService : IEstateExperienceServiceInterface
    {
        List<EstateExperienceInfo> IEstateExperienceServiceInterface.this[uint estateID]
        {
            get
            {
                var result = new List<EstateExperienceInfo>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM estateexperiences WHERE EstateID = @estateid", conn))
                    {
                        cmd.Parameters.AddParameter("@estateid", estateID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new EstateExperienceInfo
                                {
                                    EstateID = (uint)(long)reader["EstateID"],
                                    ExperienceID = reader.GetUUID("ExperienceID"),
                                    IsAllowed = reader.GetBool("IsAllowed")
                                });
                            }
                        }
                    }
                }
                return result;
            }
        }

        EstateExperienceInfo IEstateExperienceServiceInterface.this[uint estateID, UUID experienceID]
        {
            get
            {
                EstateExperienceInfo info;
                if (!Experiences.TryGetValue(estateID, experienceID, out info))
                {
                    throw new KeyNotFoundException();
                }
                return info;
            }
        }

        bool IEstateExperienceServiceInterface.Remove(uint estateID, UUID experienceID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM estateexperiences WHERE EstateID = @estateid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@estateid", estateID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        void IEstateExperienceServiceInterface.Store(EstateExperienceInfo info)
        {
            var vals = new Dictionary<string, object>
            {
                ["EstateID"] = info.EstateID,
                ["ExperienceID"] = info.ExperienceID,
                ["IsAllowed"] = info.IsAllowed
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.ReplaceInto("estateexperiences", vals);
            }
        }

        bool IEstateExperienceServiceInterface.TryGetValue(uint estateID, UUID experienceID, out EstateExperienceInfo info)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM estateexperiences WHERE EstateID = @estateid AND ExperienceID = @experienceid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@estateid", estateID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            info = new EstateExperienceInfo
                            {
                                EstateID = estateID,
                                ExperienceID = experienceID,
                                IsAllowed = reader.GetBool("IsAllowed")
                            };
                            return true;
                        }
                    }
                }
            }
            info = default(EstateExperienceInfo);
            return false;
        }
    }
}
