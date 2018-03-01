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
using SilverSim.Types;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage : ISimulationDataEnvControllerStorageInterface
    {
        bool ISimulationDataEnvControllerStorageInterface.TryGetValue(UUID regionID, out byte[] settings)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT SerializedData FROM environmentcontroller WHERE RegionID = @regionid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings = reader.GetBytes("SerializedData");
                            return true;
                        }
                    }
                }
            }
            settings = null;
            return false;
        }

        /* setting value to null will delete the entry */
        byte[] ISimulationDataEnvControllerStorageInterface.this[UUID regionID]
        {
            get
            {
                byte[] settings;
                if (!EnvironmentController.TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    if (value == null)
                    {
#if DEBUG
                        m_Log.DebugFormat("Removing environment controller settings for {0}", regionID.ToString());
#endif
                        using (var cmd = new SQLiteCommand("DELETE FROM environmentcontroller WHERE RegionID = @regionid", conn))
                        {
                            cmd.Parameters.AddParameter("@regionid", regionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
#if DEBUG
                        m_Log.DebugFormat("Storing new environment controller settings for {0}", regionID.ToString());
#endif
                        var param = new Dictionary<string, object>
                        {
                            ["RegionID"] = regionID,
                            ["SerializedData"] = value
                        };
                        conn.ReplaceInto("environmentcontroller", param);
                    }
                }
            }
        }

        bool ISimulationDataEnvControllerStorageInterface.Remove(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM environmentcontroller WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
