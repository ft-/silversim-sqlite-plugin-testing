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

using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Inventory
{
    public sealed partial class SQLiteInventoryService : IInventoryItemServiceInterface
    {
        bool IInventoryItemServiceInterface.ContainsKey(UUID key)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT ID FROM " + m_InventoryItemTable + " WHERE ID = @itemid LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@itemid", key);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool IInventoryItemServiceInterface.TryGetValue(UUID key, out InventoryItem item)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_InventoryItemTable + " WHERE ID = @itemid LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@itemid", key);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            item = dbReader.ToItem();
                            return true;
                        }
                    }
                }
            }

            item = default(InventoryItem);
            return false;
        }

        InventoryItem IInventoryItemServiceInterface.this[UUID key]
        {
            get
            {
                InventoryItem item;
                if (!Item.TryGetValue(key, out item))
                {
                    throw new InventoryItemNotFoundException(key);
                }
                return item;
            }
        }

        List<InventoryItem> IInventoryItemServiceInterface.this[UUID principalID, List<UUID> itemids]
        {
            get
            {
                if (itemids == null || itemids.Count == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(itemids));
                }
                var items = new List<InventoryItem>();
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    var matchStrings = new List<string>();
                    foreach (UUID itemid in itemids)
                    {
                        matchStrings.Add(string.Format("\"{0}\"", itemid.ToString()));
                    }
                    string qStr = string.Join(",", matchStrings);
                    using (var cmd = new SQLiteCommand("SELECT * FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid AND ID IN (" + qStr + ")", connection))
                    {
                        cmd.Parameters.AddParameter("@ownerid", principalID);
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
        }

        bool IInventoryItemServiceInterface.ContainsKey(UUID principalID, UUID key)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT ID FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid AND ID = @itemid LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    cmd.Parameters.AddParameter("@itemid", key);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool IInventoryItemServiceInterface.TryGetValue(UUID principalID, UUID key, out InventoryItem item)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid AND ID = @itemid LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    cmd.Parameters.AddParameter("@itemid", key);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            item = dbReader.ToItem();
                            return true;
                        }
                    }
                }
            }

            item = default(InventoryItem);
            return false;
        }

        InventoryItem IInventoryItemServiceInterface.this[UUID principalID, UUID key]
        {
            get
            {
                InventoryItem item;
                if (!Item.TryGetValue(principalID, key, out item))
                {
                    throw new InventoryItemNotFoundException(key);
                }
                return item;
            }
        }

        void IInventoryItemServiceInterface.Add(InventoryItem item)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    if (!IsParentFolderIdValid(connection, item.Owner.ID, item.ParentFolderID, UUID.Zero, transaction))
                    {
                        throw new InvalidParentFolderIdException(string.Format("Invalid parent folder {0} for item {1}", item.ParentFolderID, item.ID));
                    }

                    connection.InsertInto(m_InventoryItemTable, item.ToDictionary(), transaction);
                });
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        void IInventoryItemServiceInterface.Update(InventoryItem item)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                var newVals = new Dictionary<string, object>
                {
                    ["AssetID"] = item.AssetID,
                    ["Name"] = item.Name,
                    ["Description"] = item.Description,
                    ["BasePermissionsMask"] = item.Permissions.Base,
                    ["CurrentPermissionsMask"] = item.Permissions.Current,
                    ["EveryOnePermissionsMask"] = item.Permissions.EveryOne,
                    ["NextOwnerPermissionsMask"] = item.Permissions.NextOwner,
                    ["GroupPermissionsMask"] = item.Permissions.Group,
                    ["SalePrice"] = item.SaleInfo.Price,
                    ["SaleType"] = item.SaleInfo.Type,
                    ["Flags"] = item.Flags
                };
                connection.UpdateSet(m_InventoryItemTable, newVals, string.Format("OwnerID = '{0}' AND ID = '{1}'", item.Owner.ID, item.ID));
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        void IInventoryItemServiceInterface.Delete(UUID principalID, UUID id)
        {
            InventoryItem item = Item[principalID, id];
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_InventoryItemTable + " WHERE OwnerID = @ownerid AND ID = @itemid", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", principalID);
                    cmd.Parameters.AddParameter("@itemid", id);
                    if (1 > cmd.ExecuteNonQuery())
                    {
                        throw new InventoryItemNotFoundException(id);
                    }
                }
            }
            IncrementVersion(principalID, item.ParentFolderID);
        }

        List<UUID> IInventoryItemServiceInterface.Delete(UUID principalID, List<UUID> itemids)
        {
            var deleted = new List<UUID>();
            foreach (UUID id in itemids)
            {
                try
                {
                    Item.Delete(principalID, id);
                    deleted.Add(id);
                }
                catch
                {
                    /* nothing else to do */
                }
            }
            return deleted;
        }

        void IInventoryItemServiceInterface.Move(UUID principalID, UUID id, UUID toFolderID)
        {
            InventoryItem item = Item[principalID, id];

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction((transaction) =>
                {
                    if (!IsParentFolderIdValid(connection, principalID, toFolderID, UUID.Zero, transaction))
                    {
                        throw new InvalidParentFolderIdException(string.Format("Invalid parent folder {0} for item {1}", toFolderID, id));
                    }

                    using (var cmd = new SQLiteCommand("SELECT NULL FROM " + m_InventoryFolderTable + " WHERE ID = @folderid AND OwnerID = @ownerid", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@folderid", toFolderID);
                        cmd.Parameters.AddParameter("@ownerid", principalID);
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                throw new InventoryItemNotStoredException(id);
                            }
                        }
                    }
                    using (var cmd = new SQLiteCommand("UPDATE " + m_InventoryItemTable + " SET ParentFolderID = @folderid WHERE ID = @itemid AND OwnerID = @ownerid", connection)
                    {
                        Transaction = transaction
                    })
                    {
                        cmd.Parameters.AddParameter("@folderid", toFolderID);
                        cmd.Parameters.AddParameter("@ownerid", principalID);
                        cmd.Parameters.AddParameter("@itemid", id);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new InventoryItemNotFoundException(id);
                        }
                    }
                });
            }
            IncrementVersion(principalID, item.ParentFolderID);
            IncrementVersion(principalID, toFolderID);
        }

        UUID IInventoryItemServiceInterface.Copy(UUID principalID, UUID id, UUID newFolder) =>
            CopyItem(principalID, id, newFolder);

        private void IncrementVersion(UUID principalID, UUID folderID)
        {
            try
            {
                using (var connection = new SQLiteConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("UPDATE " + m_InventoryFolderTable + " SET Version = Version + 1 WHERE OwnerID = @ownerid AND ID = @folderid", connection))
                    {
                        cmd.Parameters.AddParameter("@ownerid", principalID);
                        cmd.Parameters.AddParameter("@folderid", folderID);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new InventoryFolderNotStoredException(folderID);
                        }
                    }
                }
            }
            catch
            {
                /* nothing to do here */
            }
        }
    }
}
