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

using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage
    {
        private readonly RwLockedList<SQLiteSceneListener> m_SceneListenerThreads = new RwLockedList<SQLiteSceneListener>();
        public class SQLiteSceneListener : SceneListener
        {
            private readonly string m_ConnectionString;
            private readonly RwLockedList<SQLiteSceneListener> m_SceneListenerThreads;

            public SQLiteSceneListener(string connectionString, UUID regionID, RwLockedList<SQLiteSceneListener> sceneListenerThreads)
            {
                m_ConnectionString = connectionString;
                RegionID = regionID;
                m_SceneListenerThreads = sceneListenerThreads;
            }

            public UUID RegionID { get; }

            public QueueStat GetStats()
            {
                int count = m_StorageMainRequestQueue.Count;
                return new QueueStat(count != 0 ? "PROCESSING" : "IDLE", count, (uint)m_ProcessedPrims);
            }

            private int m_ProcessedPrims;

            private readonly Dictionary<int, string> m_CachedUpdateObjectCmds = new Dictionary<int, string>();

            private string GenerateUpdateObjectCmd(SQLiteConnection conn, List<string> vals, int index)
            {
                string cmd;
                if (m_CachedUpdateObjectCmds.TryGetValue(index, out cmd))
                {
                    return cmd;
                }

                SQLiteCommandBuilder b = new SQLiteCommandBuilder();
                StringBuilder sb = new StringBuilder();

                sb.Append("REPLACE INTO objects (RegionID, ID");
                StringBuilder sb2 = new StringBuilder();
                sb2.AppendFormat(" VALUES (@v_RegionID,@v_ID{0},", index);
                foreach (string f in vals)
                {
                    sb.Append(",");
                    sb.Append(b.QuoteIdentifier(f));
                    sb2.Append(",");
                    sb2.Append("@v_" + f + index.ToString());
                }
                sb.Append(sb2);
                sb.Append(";");

                cmd = sb.ToString();
                m_CachedUpdateObjectCmds.Add(index, cmd);
                return cmd;
            }

            private readonly Dictionary<int, string> m_CachedUpdatePrimCmds = new Dictionary<int, string>();
            private string GenerateUpdatePrimCmd(SQLiteConnection conn, List<string> vals, int index)
            {
                string cmd;
                if (m_CachedUpdatePrimCmds.TryGetValue(index, out cmd))
                {
                    return cmd;
                }

                SQLiteCommandBuilder b = new SQLiteCommandBuilder();
                StringBuilder sb = new StringBuilder();

                sb.Append("REPLACE INTO prims (RegionID, ID");
                StringBuilder sb2 = new StringBuilder();
                sb2.AppendFormat(" VALUES (@v_RegionID,@v_ID{0},", index);
                foreach (string f in vals)
                {
                    sb.Append(",");
                    sb.Append(b.QuoteIdentifier(f));
                    sb2.Append(",");
                    sb2.Append("@v_" + f + index.ToString());
                }
                sb.Append(sb2);
                sb.Append(";");

                cmd = sb.ToString();
                m_CachedUpdatePrimCmds.Add(index, cmd);
                return cmd;
            }

            private readonly Dictionary<int, string> m_CachedUpdatePrimItemCmds = new Dictionary<int, string>();
            private string GenerateUpdatePrimItemCmd(SQLiteConnection conn, List<string> vals, int index)
            {
                string cmd;
                if (m_CachedUpdatePrimItemCmds.TryGetValue(index, out cmd))
                {
                    return cmd;
                }

                SQLiteCommandBuilder b = new SQLiteCommandBuilder();
                StringBuilder sb = new StringBuilder();

                sb.Append("REPLACE INTO primitems (RegionID,PrimID,InventoryID");
                StringBuilder sb2 = new StringBuilder();
                sb2.Append(" VALUES (@v_RegionID,@v_PrimID,@v_IventoryID,");
                foreach (string f in vals)
                {
                    sb.Append(",");
                    sb.Append(b.QuoteIdentifier(f));
                    sb2.Append(",");
                    sb2.Append("@v_" + f + index.ToString());
                }
                sb.Append(sb2);
                sb.Append(";");

                cmd = sb.ToString();
                m_CachedUpdatePrimItemCmds.Add(index, cmd);
                return cmd;
            }

            protected override void StorageMainThread()
            {
                try
                {
                    m_SceneListenerThreads.Add(this);
                    Thread.CurrentThread.Name = "Storage Main Thread: " + RegionID.ToString();
                    var primDeletionRequests = new List<string>();
                    var primItemDeletionRequests = new List<string>();
                    var objectDeletionRequests = new List<string>();
                    int updateObjectsRequestCount = 0;
                    int updatePrimsRequestCount = 0;
                    int updatePrimItemsRequestCount = 0;
                    var updateObjectsRequestData = new Dictionary<string, object>();
                    var updatePrimsRequestData = new Dictionary<string, object>();
                    var updatePrimItemsRequestData = new Dictionary<string, object>();

                    var knownSerialNumbers = new C5.TreeDictionary<uint, int>();
                    var knownInventorySerialNumbers = new C5.TreeDictionary<uint, int>();
                    var knownInventories = new C5.TreeDictionary<uint, List<UUID>>();
                    List<string> updatePrimFields = null;
                    List<string> updatePrimItemFields = null;
                    List<string> updateObjectFields = null;
                    SQLiteCommandBuilder cmdbuild = new SQLiteCommandBuilder();

                    while (!m_StopStorageThread || m_StorageMainRequestQueue.Count != 0)
                    {
                        ObjectUpdateInfo req;
                        try
                        {
                            req = m_StorageMainRequestQueue.Dequeue(1000);
                        }
                        catch
                        {
                            continue;
                        }

                        int serialNumber = req.SerialNumber;
                        int knownSerial;
                        int knownInventorySerial;
                        bool updatePrim = false;
                        bool updateInventory = false;
                        if (req.IsKilled)
                        {
                            /* has to be processed */
                            string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                            string partID = req.Part.ID.ToString();
                            primDeletionRequests.Add(string.Format("(RegionID = '{0}' AND ID = '{1}')", sceneID, partID));
                            primItemDeletionRequests.Add(string.Format("(RegionID = '{0}' AND PrimID = '{1}')", sceneID, partID));
                            knownSerialNumbers.Remove(req.LocalID);
                            if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                            {
                                objectDeletionRequests.Add(string.Format("(RegionID = '{0}' AND ID = '{1}')", sceneID, partID));
                            }
                        }
                        else if (knownSerialNumbers.Contains(req.LocalID))
                        {
                            knownSerial = knownSerialNumbers[req.LocalID];
                            if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                            {
                                string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                                string partID = req.Part.ID.ToString();
                                primDeletionRequests.Add(string.Format("(RegionID = '{0}' AND ID = '{1}')", sceneID, partID));
                                primItemDeletionRequests.Add(string.Format("(RegionID = '{0}' AND PrimID = '{1}')", sceneID, partID));
                                if (req.Part.LinkNumber == ObjectGroup.LINK_ROOT)
                                {
                                    objectDeletionRequests.Add(string.Format("(RegionID = '{0}' AND ID = '{1}')", sceneID, partID));
                                }
                            }
                            else
                            {
                                if (knownSerial != serialNumber && !req.Part.ObjectGroup.IsAttached && !req.Part.ObjectGroup.IsTemporary)
                                {
                                    /* prim update */
                                    updatePrim = true;
                                    updateInventory = true;
                                }

                                if (knownInventorySerialNumbers.Contains(req.LocalID))
                                {
                                    knownInventorySerial = knownSerialNumbers[req.LocalID];
                                    /* inventory update */
                                    updateInventory = knownInventorySerial != req.Part.Inventory.InventorySerial;
                                }
                            }
                        }
                        else if (req.Part.ObjectGroup.IsAttached || req.Part.ObjectGroup.IsTemporary)
                        {
                            /* ignore it */
                            continue;
                        }
                        else
                        {
                            updatePrim = true;
                            updateInventory = true;
                        }

                        int newPrimInventorySerial = req.Part.Inventory.InventorySerial;

                        int count = Interlocked.Increment(ref m_ProcessedPrims);
                        if (count % 100 == 0)
                        {
                            m_Log.DebugFormat("Processed {0} prims", count);
                        }

                        if (updatePrim)
                        {
                            Dictionary<string, object> primData = GenerateUpdateObjectPart(req.Part);
                            if (updatePrimFields == null)
                            {
                                updatePrimFields = new List<string>(primData.Keys);
                                updatePrimFields.Remove("ID");
                            }
                            foreach (KeyValuePair<string, object> kvp in primData)
                            {
                                updatePrimsRequestData.Add(kvp.Key + updatePrimsRequestCount.ToString(), kvp.Value);
                            }
                            ++updatePrimsRequestCount;
                            ObjectGroup grp = req.Part.ObjectGroup;

                            knownSerialNumbers[req.LocalID] = req.SerialNumber;

                            Dictionary<string, object> objData = GenerateUpdateObjectGroup(grp);
                            if (updateObjectFields == null)
                            {
                                updateObjectFields = new List<string>(objData.Keys);
                                updateObjectFields.Remove("ID");
                            }
                            foreach (KeyValuePair<string, object> kvp in primData)
                            {
                                updateObjectsRequestData.Add(kvp.Key + updateObjectsRequestCount.ToString(), kvp.Value);
                            }
                        }

                        if (updateInventory)
                        {
                            var items = new Dictionary<UUID, ObjectPartInventoryItem>();
                            foreach (ObjectPartInventoryItem item in req.Part.Inventory.ValuesByKey1)
                            {
                                items.Add(item.ID, item);
                            }

                            if (knownInventories.Contains(req.Part.LocalID))
                            {
                                string sceneID = req.Part.ObjectGroup.Scene.ID.ToString();
                                string partID = req.Part.ID.ToString();
                                foreach (UUID itemID in knownInventories[req.Part.LocalID])
                                {
                                    if (!items.ContainsKey(itemID))
                                    {
                                        primItemDeletionRequests.Add(string.Format("(RegionID = '{0}' AND PrimID = '{1}' AND InventoryID = '{2}')",
                                            sceneID, partID, itemID.ToString()));
                                    }
                                }

                                foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                                {
                                    Dictionary<string, object> data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                    if (updatePrimItemFields == null)
                                    {
                                        updatePrimItemFields = new List<string>(data.Keys);
                                        updatePrimItemFields.Remove("PrimID");
                                        updatePrimItemFields.Remove("InventoryID");
                                    }
                                    foreach (KeyValuePair<string, object> kvpInner in data)
                                    {
                                        updatePrimItemsRequestData.Add(kvpInner.Key + updatePrimItemsRequestCount.ToString(), kvp.Value);
                                    }
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<UUID, ObjectPartInventoryItem> kvp in items)
                                {
                                    Dictionary<string, object> data = GenerateUpdateObjectPartInventoryItem(req.Part.ID, kvp.Value);
                                    foreach (KeyValuePair<string, object> kvpInner in data)
                                    {
                                        updatePrimItemsRequestData.Add(kvpInner.Key + updatePrimItemsRequestCount.ToString(), kvp.Value);
                                    }
                                }
                            }
                            knownInventories[req.Part.LocalID] = new List<UUID>(items.Keys);
                            knownInventorySerialNumbers[req.Part.LocalID] = newPrimInventorySerial;
                        }

                        bool emptyQueue = m_StorageMainRequestQueue.Count == 0;
                        bool processUpdateObjects = updateObjectsRequestCount != 0;
                        bool processUpdatePrims = updatePrimsRequestCount != 0;
                        bool processUpdatePrimItems = updatePrimItemsRequestCount != 0;

                        if (((emptyQueue || processUpdateObjects) && objectDeletionRequests.Count > 0) || objectDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", objectDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM objects WHERE " + elems;
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (var cmd = new SQLiteCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                objectDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object deletion failed", e);
                            }
                        }

                        if (((emptyQueue || processUpdatePrims) && primDeletionRequests.Count > 0) || primDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", primDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM prims WHERE " + elems;
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (var cmd = new SQLiteCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                primDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim deletion failed", e);
                            }
                        }

                        if (((emptyQueue || processUpdatePrimItems) && primItemDeletionRequests.Count > 0) || primItemDeletionRequests.Count > 256)
                        {
                            string elems = string.Join(" OR ", primItemDeletionRequests);
                            try
                            {
                                string command = "DELETE FROM primitems WHERE " + elems;
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    using (var cmd = new SQLiteCommand(command, conn))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                primItemDeletionRequests.Clear();
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object deletion failed", e);
                            }
                        }

                        if ((emptyQueue && updateObjectsRequestCount > 0) || updateObjectsRequestCount > 256)
                        {
                            try
                            {
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    StringBuilder command = new StringBuilder();
                                    for (int i = 0; i < updateObjectsRequestCount; ++i)
                                    {
                                        command.Append(GenerateUpdateObjectCmd(conn, updateObjectFields, i));
                                    }
                                    using (var cmd = new SQLiteCommand(command.ToString(), conn))
                                    {
                                        cmd.Parameters.AddParameter("@v_RegionID", RegionID);
                                        foreach (KeyValuePair<string, object> kvp in updateObjectsRequestData)
                                        {
                                            cmd.Parameters.AddParameter("@v_" + kvp.Key, kvp.Value);
                                        }
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updateObjectsRequestData.Clear();
                                updateObjectsRequestCount = 0;
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Object update failed", e);
                            }
                        }

                        if ((emptyQueue && updatePrimsRequestCount > 0) || updatePrimsRequestCount > 256)
                        {
                            try
                            {
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    StringBuilder command = new StringBuilder();
                                    for (int i = 0; i < updatePrimsRequestCount; ++i)
                                    {
                                        command.Append(GenerateUpdatePrimCmd(conn, updatePrimFields, i));
                                    }
                                    using (var cmd = new SQLiteCommand(command.ToString(), conn))
                                    {
                                        cmd.Parameters.AddParameter("@v_RegionID", RegionID);
                                        foreach (KeyValuePair<string, object> kvp in updateObjectsRequestData)
                                        {
                                            cmd.Parameters.AddParameter("@v_" + kvp.Key, kvp.Value);
                                        }
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updatePrimsRequestData.Clear();
                                updatePrimsRequestCount = 0;
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim update failed", e);
                            }
                        }

                        if ((emptyQueue && updatePrimItemsRequestCount > 0) || updatePrimItemsRequestCount > 256)
                        {
                            try
                            {
                                using (var conn = new SQLiteConnection(m_ConnectionString))
                                {
                                    conn.Open();
                                    StringBuilder command = new StringBuilder();
                                    for (int i = 0; i < updateObjectsRequestCount; ++i)
                                    {
                                        command.Append(GenerateUpdatePrimItemCmd(conn, updatePrimItemFields, i));
                                    }
                                    using (var cmd = new SQLiteCommand(command.ToString(), conn))
                                    {
                                        cmd.Parameters.AddParameter("@v_RegionID", RegionID);
                                        foreach (KeyValuePair<string, object> kvp in updateObjectsRequestData)
                                        {
                                            cmd.Parameters.AddParameter("@v_" + kvp.Key, kvp.Value);
                                        }
                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                updatePrimItemsRequestData.Clear();
                                updatePrimItemsRequestCount = 0;
                            }
                            catch (Exception e)
                            {
                                m_Log.Error("Prim inventory update failed", e);
                            }
                        }
                    }
                }
                finally
                {
                    m_SceneListenerThreads.Remove(this);
                }
            }

            private Dictionary<string, object> GenerateUpdateObjectPartInventoryItem(UUID primID, ObjectPartInventoryItem item)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = item.PermsGranter;
                return new Dictionary<string, object>
                {
                    ["AssetId"] = item.AssetID,
                    ["AssetType"] = item.AssetType,
                    ["CreationDate"] = item.CreationDate,
                    ["Creator"] = item.Creator,
                    ["Description"] = item.Description,
                    ["Flags"] = item.Flags,
                    ["Group"] = item.Group,
                    ["GroupOwned"] = item.IsGroupOwned,
                    ["PrimID"] = primID,
                    ["Name"] = item.Name,
                    ["InventoryID"] = item.ID,
                    ["InventoryType"] = item.InventoryType,
                    ["LastOwner"] = item.LastOwner,
                    ["Owner"] = item.Owner,
                    ["ParentFolderID"] = item.ParentFolderID,
                    ["BasePermissions"] = item.Permissions.Base,
                    ["CurrentPermissions"] = item.Permissions.Current,
                    ["EveryOnePermissions"] = item.Permissions.EveryOne,
                    ["GroupPermissions"] = item.Permissions.Group,
                    ["NextOwnerPermissions"] = item.Permissions.NextOwner,
                    ["SaleType"] = item.SaleInfo.Type,
                    ["SalePrice"] = item.SaleInfo.Price,
                    ["SalePermMask"] = item.SaleInfo.PermMask,
                    ["PermsGranter"] = grantinfo.PermsGranter.ToString(),
                    ["PermsMask"] = grantinfo.PermsMask,
                    ["NextOwnerAssetID"] = item.NextOwnerAssetID
                };
            }

            private Dictionary<string, object> GenerateUpdateObjectGroup(ObjectGroup objgroup) => new Dictionary<string, object>
            {
                ["ID"] = objgroup.ID,
                ["RegionID"] = objgroup.Scene.ID,
                ["IsTempOnRez"] = objgroup.IsTempOnRez,
                ["Owner"] = objgroup.Owner,
                ["LastOwner"] = objgroup.LastOwner,
                ["Group"] = objgroup.Group,
                ["OriginalAssetID"] = objgroup.OriginalAssetID,
                ["NextOwnerAssetID"] = objgroup.NextOwnerAssetID,
                ["SaleType"] = objgroup.SaleType,
                ["SalePrice"] = objgroup.SalePrice,
                ["PayPrice0"] = objgroup.PayPrice0,
                ["PayPrice1"] = objgroup.PayPrice1,
                ["PayPrice2"] = objgroup.PayPrice2,
                ["PayPrice3"] = objgroup.PayPrice3,
                ["PayPrice4"] = objgroup.PayPrice4,
                ["AttachedPos"] = objgroup.AttachedPos,
                ["AttachPoint"] = objgroup.AttachPoint,
                ["IsIncludedInSearch"] = objgroup.IsIncludedInSearch,
                ["RezzingObjectID"] = objgroup.RezzingObjectID
            };

            private Dictionary<string, object> GenerateUpdateObjectPart(ObjectPart objpart)
            {
                var data = new Dictionary<string, object>
                {
                    ["ID"] = objpart.ID,
                    ["LinkNumber"] = objpart.LinkNumber,
                    ["RootPartID"] = objpart.ObjectGroup.RootPart.ID,
                    ["Position"] = objpart.Position,
                    ["Rotation"] = objpart.Rotation,
                    ["SitText"] = objpart.SitText,
                    ["TouchText"] = objpart.TouchText,
                    ["Name"] = objpart.Name,
                    ["Description"] = objpart.Description,
                    ["SitTargetOffset"] = objpart.SitTargetOffset,
                    ["SitTargetOrientation"] = objpart.SitTargetOrientation,
                    ["PhysicsShapeType"] = objpart.PhysicsShapeType,
                    ["PathfindingType"] = objpart.PathfindingType,
                    ["Material"] = objpart.Material,
                    ["Size"] = objpart.Size,
                    ["Slice"] = objpart.Slice,
                    ["MediaURL"] = objpart.MediaURL,
                    ["Creator"] = objpart.Creator,
                    ["CreationDate"] = objpart.CreationDate,
                    ["Flags"] = objpart.Flags,
                    ["AngularVelocity"] = objpart.AngularVelocity,
                    ["LightData"] = objpart.PointLight.DbSerialization,
                    ["ProjectionData"] = objpart.Projection.DbSerialization,
                    ["HoverTextData"] = objpart.Text.Serialization,
                    ["FlexibleData"] = objpart.Flexible.DbSerialization,
                    ["LoopedSoundData"] = objpart.Sound.Serialization,
                    ["ImpactSoundData"] = objpart.CollisionSound.Serialization,
                    ["PrimitiveShapeData"] = objpart.Shape.Serialization,
                    ["ParticleSystem"] = objpart.ParticleSystemBytes,
                    ["TextureEntryBytes"] = objpart.TextureEntryBytes,
                    ["TextureAnimationBytes"] = objpart.TextureAnimationBytes,
                    ["ScriptAccessPin"] = objpart.ScriptAccessPin,
                    ["CameraAtOffset"] = objpart.CameraAtOffset,
                    ["CameraEyeOffset"] = objpart.CameraEyeOffset,
                    ["ForceMouselook"] = objpart.ForceMouselook,
                    ["BasePermissions"] = objpart.BaseMask,
                    ["CurrentPermissions"] = objpart.OwnerMask,
                    ["EveryOnePermissions"] = objpart.EveryoneMask,
                    ["GroupPermissions"] = objpart.GroupMask,
                    ["NextOwnerPermissions"] = objpart.NextOwnerMask,
                    ["ClickAction"] = objpart.ClickAction,
                    ["PassCollisionMode"] = objpart.PassCollisionMode,
                    ["PassTouchMode"] = objpart.PassTouchMode,
                    ["Velocity"] = objpart.Velocity,
                    ["IsSoundQueueing"] = objpart.IsSoundQueueing,
                    ["IsAllowedDrop"] = objpart.IsAllowedDrop,
                    ["PhysicsDensity"] = objpart.PhysicsDensity,
                    ["PhysicsFriction"] = objpart.PhysicsFriction,
                    ["PhysicsRestitution"] = objpart.PhysicsRestitution,
                    ["PhysicsGravityMultiplier"] = objpart.PhysicsGravityMultiplier,

                    ["IsRotateXEnabled"] = objpart.IsRotateXEnabled,
                    ["IsRotateYEnabled"] = objpart.IsRotateYEnabled,
                    ["IsRotateZEnabled"] = objpart.IsRotateZEnabled,
                    ["IsVolumeDetect"] = objpart.IsVolumeDetect,
                    ["IsPhantom"] = objpart.IsPhantom,
                    ["IsPhysics"] = objpart.IsPhysics,
                    ["IsSandbox"] = objpart.IsSandbox,
                    ["IsBlockGrab"] = objpart.IsBlockGrab,
                    ["IsDieAtEdge"] = objpart.IsDieAtEdge,
                    ["IsReturnAtEdge"] = objpart.IsReturnAtEdge,
                    ["IsBlockGrabObject"] = objpart.IsBlockGrabObject
                };
                using (var ms = new MemoryStream())
                {
                    LlsdBinary.Serialize(objpart.DynAttrs, ms);
                    data.Add("DynAttrs", ms.ToArray());
                }

                return data;
            }
        }

        public override SceneListener GetSceneListener(UUID regionID) =>
            new SQLiteSceneListener(m_ConnectionString, regionID, m_SceneListenerThreads);
    }
}
