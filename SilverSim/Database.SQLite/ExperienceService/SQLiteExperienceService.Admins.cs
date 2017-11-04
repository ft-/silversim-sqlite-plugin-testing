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

using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.ExperienceService
{
    public sealed partial class SQLiteExperienceService : ExperienceServiceInterface.IExperienceAdminInterface
    {
        List<UUID> IExperienceAdminInterface.this[UUI agent]
        {
            get
            {
                var result = new List<UUID>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT ExperienceID, Admin FROM experienceadmins WHERE Admin LIKE @admin", conn))
                    {
                        cmd.Parameters.AddParameter("@admin", agent.ID.ToString() + "%");
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetUUI("Admin").EqualsGrid(agent))
                                {
                                    result.Add(reader.GetUUID("ExperienceID"));
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }

        bool IExperienceAdminInterface.this[UUID experienceID, UUI agent]
        {
            get
            {
                bool allowed;
                return Admins.TryGetValue(experienceID, agent, out allowed) && allowed;
            }

            set
            {
                if (value)
                {
                    var vals = new Dictionary<string, object>
                    {
                        ["ExperienceID"] = experienceID,
                        ["Admin"] = agent,
                    };
                    using (var conn = new SQLiteConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInto("experienceadmins", vals);
                    }
                }
                else
                {
                    using (var conn = new SQLiteConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand("DELETE FROM experienceadmins WHERE ExperienceID = @experienceid AND Admin LIKE @admin", conn))
                        {
                            cmd.Parameters.AddParameter("@experienceid", experienceID);
                            cmd.Parameters.AddParameter("@admin", agent.ID.ToString() + "%");
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        bool IExperienceAdminInterface.TryGetValue(UUID experienceID, UUI agent, out bool allowed)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT Admin FROM experienceadmins WHERE ExperienceID = @experienceid AND Admin LIKE @admin", conn))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    cmd.Parameters.AddParameter("@admin", agent.ID.ToString() + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetUUI("Admin").EqualsGrid(agent))
                            {
                                return allowed = true;
                            }
                        }
                    }
                }
            }
            return allowed = false;
        }
    }
}
