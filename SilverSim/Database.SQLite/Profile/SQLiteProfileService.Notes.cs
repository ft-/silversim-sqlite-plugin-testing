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
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Profile
{
    public sealed partial class SQLiteProfileService : ProfileServiceInterface.INotesInterface
    {
        bool INotesInterface.ContainsKey(UGUI user, UGUI target)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT useruuid FROM usernotes WHERE useruuid = @user AND targetuuid = @target LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@user", user.ID);
                    cmd.Parameters.AddParameter("@target", target.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool INotesInterface.TryGetValue(UGUI user, UGUI target, out string notes)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT notes FROM usernotes WHERE useruuid = @user AND targetuuid = @target LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@user", user.ID);
                    cmd.Parameters.AddParameter("@target", target.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            notes = (string)reader["notes"];
                            return true;
                        }
                    }
                }
            }

            notes = string.Empty;
            return false;
        }

        string INotesInterface.this[UGUI user, UGUI target]
        {
            get
            {
                string notes;
                if (!Notes.TryGetValue(user, target, out notes))
                {
                    throw new KeyNotFoundException();
                }
                return notes;
            }
            set
            {
                var replaceVals = new Dictionary<string, object>
                {
                    ["user"] = user.ID,
                    ["target"] = target.ID,
                    ["notes"] = value
                };
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("usernotes", replaceVals);
                }
            }
        }
    }
}
