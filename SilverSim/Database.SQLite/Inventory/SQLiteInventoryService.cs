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
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Inventory
{
    [Description("SQLite Inventory Backend")]
    [PluginName("Inventory")]
    public sealed partial class SQLiteInventoryService : InventoryServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE INVENTORY SERVICE");
        private readonly DefaultInventoryFolderContentService m_ContentService;
        private readonly string m_InventoryItemTable;
        private readonly string m_InventoryFolderTable;

        private readonly IMigrationElement[] Migrations;

        public SQLiteInventoryService(IConfig ownSection)
        {
            m_InventoryItemTable = ownSection.GetString("ItemTable", "inventoryitems");
            m_InventoryFolderTable = ownSection.GetString("FolderTable", "inventoryfolders");
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
            m_ContentService = new DefaultInventoryFolderContentService(this);

            /* renaming of tables for NPCs required so creating those on the fly */
            Migrations = new IMigrationElement[]
            {
                new SqlTable(m_InventoryFolderTable),
                new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<UUID>("OwnerID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
                new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
                new AddColumn<int>("Version") { IsNullAllowed = false, Default = 0 },
                new PrimaryKeyInfo("ID"),
                new NamedKeyInfo("inventoryfolders_owner_index", "OwnerID"),
                new NamedKeyInfo("inventoryfolders_owner_folderid", "OwnerID", "ParentFolderID"),
                new NamedKeyInfo("inventoryfolders_owner_type", "OwnerID", "InventoryType"),
                new TableRevision(2),
                new ChangeColumn<AssetType>("DefaultType") { IsNullAllowed = false, Default = AssetType.Unknown, OldName = "InventoryType" },

                new SqlTable(m_InventoryItemTable),
                new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
                new AddColumn<string>("Description") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
                new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
                new AddColumn<InventoryFlags>("Flags") { IsNullAllowed = false, Default = InventoryFlags.None },
                new AddColumn<UUID>("OwnerID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<UUID>("LastOwnerID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<UUID>("CreatorID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
                new AddColumn<InventoryPermissionsMask>("BasePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<InventoryPermissionsMask>("CurrentPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<InventoryPermissionsMask>("EveryOnePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<InventoryPermissionsMask>("NextOwnerPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<InventoryPermissionsMask>("GroupPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = 10 },
                new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
                new AddColumn<InventoryPermissionsMask>("SalePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
                new AddColumn<UUID>("GroupID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<bool>("IsGroupOwned") { IsNullAllowed = false, Default = false },
                new AddColumn<UUID>("AssetID") { IsNullAllowed = false, Default = UUID.Zero },
                new AddColumn<AssetType>("AssetType") { IsNullAllowed = false, Default = AssetType.Unknown },
                new PrimaryKeyInfo("ID"),
                new NamedKeyInfo("inventoryitems_OwnerID", "OwnerID"),
                new NamedKeyInfo("inventoryitems_OwnerID_ID", "OwnerID", "ID"),
                new NamedKeyInfo("inventoryitems_OwnerID_ParentFolderID", "OwnerID", "ParentFolderID"),
            };
        }

        public override bool SupportsLegacyFunctions => true;

        public override IInventoryFolderServiceInterface Folder => this;

        public override IInventoryItemServiceInterface Item => this;

        IInventoryFolderContentServiceInterface IInventoryFolderServiceInterface.Content => m_ContentService;

        public override List<InventoryItem> GetActiveGestures(UUID principalID)
        {
            var items = new List<InventoryItem>();
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid AND AssetType = @assettype AND (flags & 1) <>0", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    cmd.Parameters.AddParameter("@assettype", AssetType.Gesture);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            items.Add(dbReader.ToItem());
                        }
                    }
                }
            }

            return items;
        }

        public override List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            var folders = new List<InventoryFolder>();
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_InventoryFolderTable + " WHERE OwnerID = @ownerid", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            folders.Add(dbReader.ToFolder());
                        }
                    }
                }
            }

            return folders;
        }

        private bool TryGetParentFolderId(SQLiteConnection connection, UUID principalID, UUID folderID, out UUID parentFolderID)
        {
            using (var cmd = new SQLiteCommand("SELECT ParentFolderID FROM " + m_InventoryFolderTable + " WHERE OwnerID = @ownerid AND ID = @folderid LIMIT 1", connection))
            {
                cmd.Parameters.AddParameter("@ownerid", principalID);
                cmd.Parameters.AddParameter("@folderid", folderID);
                using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        parentFolderID = dbReader.GetUUID("ParentFolderID");
                        return true;
                    }
                }
            }
            parentFolderID = UUID.Zero;
            return false;
        }

        public bool IsParentFolderIdValid(SQLiteConnection conn, UUID principalID, UUID parentFolderID, UUID expectedFolderID, SQLiteTransaction transaction = null)
        {
            if(parentFolderID == UUID.Zero)
            {
                using (var cmd = new SQLiteCommand("SELECT NULL FROM " + m_InventoryFolderTable + " WHERE OwnerID = @ownerid AND ParentFolderID = @parentfolderid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    cmd.Parameters.AddParameter("@parentfolderid", UUID.Zero);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return !dbReader.Read();
                    }
                }
            }
            else
            {
                UUID checkFolderID = parentFolderID;
                UUID actParentFolderID;
                /* traverse to root folder and check that we never see the moved folder in that path */
                while (TryGetParentFolderId(conn, principalID, checkFolderID, out actParentFolderID))
                {
                    if (checkFolderID == expectedFolderID)
                    {
                        /* this folder would trigger a circular dependency */
                        return false;
                    }
                    if (actParentFolderID == UUID.Zero)
                    {
                        /* this is a good one, it ends at the root folder */
                        return true;
                    }

                    checkFolderID = actParentFolderID;
                }

                /* folder missing */
                return false;
            }
        }

        public override bool IsParentFolderIdValid(UUID principalID, UUID parentFolderID, UUID expectedFolderID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();

                return IsParentFolderIdValid(conn, principalID, parentFolderID, expectedFolderID);
            }
        }

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        #region Table migrations
        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        #endregion

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@ownerid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM " + m_InventoryFolderTable + " WHERE OwnerID = @ownerid", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@ownerid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }
    }
}
