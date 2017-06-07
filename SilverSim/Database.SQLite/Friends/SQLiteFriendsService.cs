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

using log4net;
using Nini.Config;
using SilverSim.Database.SQLite._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Friends
{
    public static class SQLiteFriendsExtensionMethods
    {
        public static FriendInfo ToFriendInfo(this SQLiteDataReader reader)
        {
            var fi = new FriendInfo();
            fi.User.ID = reader.GetUUID("User");
            fi.Friend.ID = reader.GetUUID("Friend");
            fi.Secret = (string)reader["Secret"];
            fi.FriendGivenFlags = reader.GetEnum<FriendRightFlags>("RightsToFriend");
            fi.UserGivenFlags = reader.GetEnum<FriendRightFlags>("RightsToUser");
            return fi;
        }
    }

    [Description("SQLite Friends Backend")]
    [PluginName("Friends")]
    public sealed class SQLiteFriendsService : FriendsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE FRIENDS SERVICE");
        private readonly string[] m_AvatarNameServiceNames;
        private AggregatingAvatarNameService m_AvatarNameService;

        public SQLiteFriendsService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", string.Empty).Split(',');
        }

        private const string m_InnerJoinSelectFull = "SELECT A.*, B.RightsToFriend AS RightsToUser FROM friends AS A INNER JOIN friends as B ON A.FriendID = B.UserID AND A.UserID = B.FriendID ";

        public void ResolveUUI(FriendInfo fi)
        {
            UUI uui;
            if (!fi.Friend.IsAuthoritative &&
                m_AvatarNameService.TryGetValue(fi.Friend, out uui))
            {
                fi.Friend = uui;
            }
            if (!fi.User.IsAuthoritative &&
                m_AvatarNameService.TryGetValue(fi.User, out uui))
            {
                fi.User = uui;
            }
        }

        public override List<FriendInfo> this[UUI user]
        {
            get
            {
                var fis = new List<FriendInfo>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(m_InnerJoinSelectFull + "WHERE A.UserID = @id", connection))
                    {
                        cmd.Parameters.AddParameter("@id", user.ID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FriendInfo fi = reader.ToFriendInfo();
                                ResolveUUI(fi);
                                fis.Add(fi);
                            }
                        }
                    }
                }

                return fis;
            }
        }

        public override FriendInfo this[UUI user, UUI friend]
        {
            get
            {
                FriendInfo fi;
                if (TryGetValue(user, friend, out fi))
                {
                    ResolveUUI(fi);
                    return fi;
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Delete(FriendInfo fi)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM friends WHERE (UserID = @userid AND FriendID = @friendid) OR (UserID = @friendid AND FriendID = @userid)", connection))
                {
                    cmd.Parameters.AddParameter("@userid", fi.User.ID);
                    cmd.Parameters.AddParameter("@friendid", fi.Friend.ID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM friends WHERE UserID = @id OR FriendID = @id", connection))
                {
                    cmd.Parameters.AddParameter("@id", accountID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            var avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
            foreach (string avatarnameservicename in m_AvatarNameServiceNames)
            {
                avatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(avatarnameservicename.Trim()));
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServices);
        }

        public override void Store(FriendInfo fi)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(() =>
                {
                    var vals = new Dictionary<string, object>();
                    vals.Add("UserID", fi.User.ID);
                    vals.Add("FriendID", fi.Friend.ID);
                    vals.Add("Secret", fi.Secret);
                    vals.Add("RightsToFriend", fi.FriendGivenFlags);

                    connection.ReplaceInto("friends", vals);

                    vals.Add("UserID", fi.Friend.ID);
                    vals.Add("FriendID", fi.User.ID);
                    vals.Add("Secret", fi.Secret);
                    vals.Add("RightsToFriend", fi.UserGivenFlags);
                    connection.ReplaceInto("friends", vals);
                });
            }
        }

        public override void StoreOffer(FriendInfo fi)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                var vals = new Dictionary<string, object>
                {
                    { "UserID", fi.Friend.ID },
                    { "FriendID", fi.User.ID },
                    { "Secret", fi.Secret },
                    { "RightsToFriend", FriendRightFlags.None }
                };
                connection.ReplaceInto("friends", vals);
            }
        }

        public override void StoreRights(FriendInfo fi)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("UPDATE friends SET RightsToFriend = @rights WHERE UserID = @userid AND FriendID = @friendid", connection))
                {
                    cmd.Parameters.AddParameter("@rights", fi.FriendGivenFlags);
                    cmd.Parameters.AddParameter("@userid", fi.User.ID);
                    cmd.Parameters.AddParameter("@friendid", fi.Friend.ID);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new FriendUpdateFailedException();
                    }
                }
            }
        }

        public override bool TryGetValue(UUI user, UUI friend, out FriendInfo fInfo)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(m_InnerJoinSelectFull + "WHERE A.UserID = @userid AND A.FriendID = @friendid", connection))
                {
                    cmd.Parameters.AddParameter("@userid", user.ID);
                    cmd.Parameters.AddParameter("@friendid", friend.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fInfo = reader.ToFriendInfo();
                            return true;
                        }
                    }
                }
            }
            fInfo = new FriendInfo();
            return false;
        }

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("friends"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false },
            new AddColumn<UUID>("FriendID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Secret") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<FriendRightFlags>("RightsToFriend") { IsNullAllowed = false },
            new PrimaryKeyInfo("UserID", "FriendID"),
            new NamedKeyInfo("PrincipalIndex", "UserID") { IsUnique = false },
            new NamedKeyInfo("FriendIndex", "FriendID") { IsUnique = false }
        };
    }
}
