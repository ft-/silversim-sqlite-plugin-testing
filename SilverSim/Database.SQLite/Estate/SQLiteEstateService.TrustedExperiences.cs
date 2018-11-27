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
    public sealed partial class SQLiteEstateService : IEstateTrustedExperienceServiceInterface
    {
        List<UEI> IEstateTrustedExperienceServiceInterface.this[uint estateID]
        {
            get
            {
                var result = new List<UEI>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT ExperienceID FROM estatetrustedexperiences WHERE EstateID = @estateid", conn))
                    {
                        cmd.Parameters.AddParameter("@estateid", estateID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new UEI(reader.GetUUID("ExperienceID")));
                            }
                        }
                    }
                }
                return result;
            }
        }

        bool IEstateTrustedExperienceServiceInterface.this[uint estateID, UEI experienceID]
        {
            get
            {
                bool trusted;
                TrustedExperiences.TryGetValue(estateID, experienceID, out trusted);
                return trusted;
            }

            set
            {
                if (value)
                {
                    var vals = new Dictionary<string, object>
                    {
                        ["EstateID"] = estateID,
                        ["ExperienceID"] = experienceID.ID
                    };
                    using (var conn = new SQLiteConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInto("estatetrustedexperiences", vals);
                    }
                }
                else
                {
                    TrustedExperiences.Remove(estateID, experienceID);
                }
            }
        }

        bool IEstateTrustedExperienceServiceInterface.Remove(uint estateID, UEI experienceID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM estatetrustedexperiences WHERE EstateID = @estateid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@estateid", estateID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID.ID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        bool IEstateTrustedExperienceServiceInterface.TryGetValue(uint estateID, UEI experienceID, out bool trusted)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM estatetrustedexperiences WHERE EstateID = @estateid AND ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@estateid", estateID);
                    cmd.Parameters.AddParameter("@experienceid", experienceID.ID);
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
