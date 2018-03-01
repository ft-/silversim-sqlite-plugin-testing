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
    public sealed partial class SQLiteExperienceService : ExperienceServiceInterface.IExperienceKeyValueInterface
    {
        void IExperienceKeyValueInterface.Add(UUID experienceID, string key, string value)
        {
            var vals = new Dictionary<string, object>
            {
                ["ExperienceID"] = experienceID,
                ["Key"] = key,
                ["Value"] = value
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();

                conn.InsertInto("experiencekeyvalues", vals);
            }
        }

        bool IExperienceKeyValueInterface.GetDatasize(UUID experienceID, out int used, out int quota)
        {
            used = 0;
            quota = -1;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT `Value` FROM experiencekeyvalues WHERE ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            used += ((string)reader["Value"]).Length;
                        }
                    }
                }
            }
            return true;
        }

        List<string> IExperienceKeyValueInterface.GetKeys(UUID experienceID)
        {
            var result = new List<string>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT `Key` FROM experiencekeyvalues WHERE ExperienceID = @experienceid", conn))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add((string)reader["Key"]);
                        }
                    }
                }
            }
            return result;
        }

        bool IExperienceKeyValueInterface.Remove(UUID experienceID, string key)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM experiencekeyvalues WHERE ExperienceID = @experienceid AND `Key` = @key", conn))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    cmd.Parameters.AddParameter("@key", key);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        void IExperienceKeyValueInterface.Store(UUID experienceID, string key, string value)
        {
            var vals = new Dictionary<string, object>
            {
                ["ExperienceID"] = experienceID,
                ["Key"] = key,
                ["Value"] = value
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.ReplaceInto("experiencekeyvalues", vals);
            }
        }

        bool IExperienceKeyValueInterface.StoreOnlyIfEqualOrig(UUID experienceID, string key, string value, string orig_value)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                return conn.InsideTransaction<bool>((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("SELECT `Value` FROM experiencekeyvalues WHERE ExperienceID = @experienceid AND `Key` = @key LIMIT 1", conn)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@experienceid", experienceID);
                        cmd.Parameters.AddParameter("@key", key);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read() && (string)reader["Value"] != orig_value)
                            {
                                return false;
                            }
                        }
                    }

                    var vals = new Dictionary<string, object>
                    {
                        ["ExperienceID"] = experienceID,
                        ["Key"] = key,
                        ["Value"] = value
                    };
                    conn.ReplaceInto("experiencekeyvalues", vals, transaction);

                    return true;
                });
            }
        }

        bool IExperienceKeyValueInterface.TryGetValue(UUID experienceID, string key, out string val)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT `Value` FROM experiencekeyvalues WHERE ExperienceID = @experienceid AND `Key` = @key LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@experienceid", experienceID);
                    cmd.Parameters.AddParameter("@key", key);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            val = (string)reader["Value"];
                            return true;
                        }
                    }
                }
            }
            val = default(string);
            return false;
        }
    }
}
