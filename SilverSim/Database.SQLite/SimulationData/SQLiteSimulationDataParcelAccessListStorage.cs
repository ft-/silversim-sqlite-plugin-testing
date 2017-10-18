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
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed class SQLiteSimulationDataParcelAccessListStorage : ISimulationDataParcelAccessListStorageInterface
    {
        private readonly string m_ConnectionString;
        private readonly string m_TableName;

        public SQLiteSimulationDataParcelAccessListStorage(string connectionString, string tableName)
        {
            m_ConnectionString = connectionString;
            m_TableName = tableName;
        }

        public bool TryGetValue(UUID regionID, UUID parcelID, UUI accessor, out ParcelAccessEntry e)
        {
            var result = new List<ParcelAccessEntry>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt <> 0", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                /* we use a specific implementation to reduce the result set here */
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE RegionID = @regionid AND ParcelID = @parcelid AND Accessor LIKE @accessor", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parcelid", parcelID);
                    cmd.Parameters.AddParameter("@accessor", accessor.ID.ToString() + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entry = new ParcelAccessEntry
                            {
                                RegionID = regionID,
                                ParcelID = reader.GetUUID("ParcelID"),
                                Accessor = reader.GetUUI("Accessor")
                            };
                            ulong val = (ulong)(long)reader["ExpiresAt"];
                            if (val != 0)
                            {
                                entry.ExpiresAt = Date.UnixTimeToDateTime(val);
                            }
                            result.Add(entry);
                        }
                    }
                }
            }

            /* the prefiltered set reduces the amount of checks we have to do here */
            IEnumerable<ParcelAccessEntry> en = from entry in result where entry.Accessor.EqualsGrid(accessor) && (entry.ExpiresAt == null || entry.ExpiresAt.AsULong > Date.Now.AsULong) select entry;
            IEnumerator<ParcelAccessEntry> enumerator = en.GetEnumerator();
            if(!enumerator.MoveNext())
            {
                e = null;
                return false;
            }
            e = enumerator.Current;
            return true;
        }

        public bool this[UUID regionID, UUID parcelID, UUI accessor]
        {
            get
            {
                ParcelAccessEntry e;
                return TryGetValue(regionID, parcelID, accessor, out e);
            }
        }

        public List<ParcelAccessEntry> this[UUID regionID, UUID parcelID]
        {
            get
            {
                var result = new List<ParcelAccessEntry>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt > 0", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE RegionID = @regionid AND ParcelID = @parcelid", connection))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        cmd.Parameters.AddParameter("@parcelid", parcelID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new ParcelAccessEntry()
                                {
                                    RegionID = reader.GetUUID("RegionID"),
                                    ParcelID = reader.GetUUID("ParcelID"),
                                    Accessor = reader.GetUUI("Accessor")
                                };
                                ulong val = (ulong)(long)reader["ExpiresAt"];
                                if (val != 0)
                                {
                                    entry.ExpiresAt = Date.UnixTimeToDateTime(val);
                                }
                                result.Add(entry);
                            }
                        }
                    }
                }
                return result;
            }
        }

        public void Store(ParcelAccessEntry entry)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt > 0", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                var data = new Dictionary<string, object>
                {
                    ["RegionID"] = entry.RegionID,
                    ["ParcelID"] = entry.ParcelID,
                    ["Accessor"] = entry.Accessor,
                    ["ExpiresAt"] = entry.ExpiresAt != null ? entry.ExpiresAt.AsULong : (ulong)0
                };
                connection.ReplaceInto(m_TableName, data);
            }
        }

        public bool RemoveAllFromRegion(UUID regionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE RegionID = @regionid", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Remove(UUID regionID, UUID parcelID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE RegionID = @regionid AND ParcelID = @parcelid", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parcelid", parcelID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Remove(UUID regionID, UUID parcelID, UUI accessor)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE RegionID = @regionid AND ParcelID = @parcelid AND Accessor LIKE @accessor", connection))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@parcelid", parcelID);
                    cmd.Parameters.AddParameter("@accessor", accessor.ID.ToString() + "%");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
