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
    public sealed partial class SQLiteProfileService : ProfileServiceInterface.IPropertiesInterface
    {
        ProfileProperties IPropertiesInterface.this[UUI user]
        {
            get
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM userprofile where useruuid = @uuid", conn))
                    {
                        cmd.Parameters.AddParameter("@uuid", user.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ProfileProperties()
                                {
                                    User = user,
                                    Partner = new UUI(reader.GetUUID("profilePartner")),
                                    PublishProfile = reader.GetBool("profileAllowPublish"),
                                    PublishMature = reader.GetBool("profileMaturePublish"),
                                    WebUrl = (string)reader["profileURL"],
                                    WantToMask = (uint)(long)reader["profileWantToMask"],
                                    WantToText = (string)reader["profileWantToText"],
                                    SkillsMask = (uint)(long)reader["profileSkillsMask"],
                                    SkillsText = (string)reader["profileSkillsText"],
                                    Language = (string)reader["profileLanguages"],
                                    ImageID = reader.GetUUID("profileImage"),
                                    AboutText = (string)reader["profileAboutText"],
                                    FirstLifeImageID = reader.GetUUID("profileFirstImage"),
                                    FirstLifeText = (string)reader["profileFirstText"]
                                };
                            }
                            else
                            {
                                return new ProfileProperties()
                                {
                                    User = user,
                                    Partner = UUI.Unknown,
                                    WebUrl = string.Empty,
                                    WantToText = string.Empty,
                                    SkillsText = string.Empty,
                                    Language = string.Empty,
                                    AboutText = string.Empty,
                                    FirstLifeText = string.Empty
                                };
                            }
                        }
                    }
                }
            }
        }

        ProfileProperties IPropertiesInterface.this[UUI user, PropertiesUpdateFlags flags]
        {
            set
            {
                var replaceVals = new Dictionary<string, object>
                {
                    ["useruuid"] = user.ID
                };
                if ((flags & PropertiesUpdateFlags.Properties) != 0)
                {
                    replaceVals["profileAllowPublish"] = value.PublishProfile;
                    replaceVals["profileMaturePublish"] = value.PublishMature;
                    replaceVals["profileURL"] = value.WebUrl;
                    replaceVals["profileImage"] = value.ImageID;
                    replaceVals["profileAboutText"] = value.AboutText;
                    replaceVals["profileFirstImage"] = value.FirstLifeImageID;
                    replaceVals["profileFirstText"] = value.FirstLifeText;
                }
                if ((flags & PropertiesUpdateFlags.Interests) != 0)
                {
                    replaceVals["profileWantToMask"] = value.WantToMask;
                    replaceVals["profileWantToText"] = value.WantToText;
                    replaceVals["profileSkillsMask"] = value.SkillsMask;
                    replaceVals["profileSkillsText"] = value.SkillsText;
                    replaceVals["profileLanguages"] = value.Language;
                }

                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    try
                    {
                        conn.InsertInto("userprofile", replaceVals);
                    }
                    catch
                    {
                        replaceVals.Remove("useruuid");
                        conn.UpdateSet("userprofile", replaceVals, "useruuid = '" + user.ID.ToString() + "'");
                    }
                }
            }
        }
    }
}
