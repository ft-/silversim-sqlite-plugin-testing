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

using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Profile
{
    public sealed partial class SQLiteProfileService : ProfileServiceInterface.IUserPreferencesInterface
    {
        bool IUserPreferencesInterface.ContainsKey(UGUI user)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT useruuid FROM usersettings where useruuid = @uuid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@uuid", user.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        bool IUserPreferencesInterface.TryGetValue(UGUI user, out ProfilePreferences prefs)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM usersettings where useruuid = @uuid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@uuid", user.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            prefs = new ProfilePreferences
                            {
                                User = user,
                                IMviaEmail = (bool)reader["imviaemail"],
                                Visible = (bool)reader["visible"]
                            };
                            return true;
                        }
                        else
                        {
                            prefs = new ProfilePreferences
                            {
                                User = user,
                                IMviaEmail = false,
                                Visible = false
                            };
                            return true;
                        }
                    }
                }
            }
        }

        ProfilePreferences IUserPreferencesInterface.this[UGUI user]
        {
            get
            {
                ProfilePreferences prefs;
                Preferences.TryGetValue(user, out prefs);
                return prefs;
            }
            set
            {
                var replaceVals = new Dictionary<string, object>
                {
                    ["useruuid"] = user.ID,
                    ["imviaemail"] = value.IMviaEmail,
                    ["visible"] = value.Visible
                };
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("usersettings", replaceVals);
                }
            }
        }
    }
}
