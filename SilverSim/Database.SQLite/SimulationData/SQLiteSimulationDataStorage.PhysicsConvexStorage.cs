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

using SilverSim.Database.SQLite._Migration;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage : ISimulationDataPhysicsConvexStorageInterface, IPhysicsHacdCleanCache
    {
        private static readonly IMigrationElement[] Migrations_Physics = new IMigrationElement[]
        {
            #region Table sculptmeshphysics
            new SqlTable("meshphysics"),
            new AddColumn<UUID>("MeshID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<byte[]>("ConvexData") { IsLong = true },
            new PrimaryKeyInfo(new string[] {"MeshID" }),
            #endregion

            #region Table primphysics
            new SqlTable("primphysics"),
            new AddColumn<byte[]>("ShapeKey") { Cardinality = 255 },
            new AddColumn<byte[]>("ConvexData") { IsLong = true },
            new PrimaryKeyInfo(new string[] {"ShapeKey" }),
            #endregion
        };

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[UUID meshid]
        {
            get
            {
                PhysicsConvexShape shape;
                if (!((ISimulationDataPhysicsConvexStorageInterface)this).TryGetValue(meshid, out shape))
                {
                    throw new KeyNotFoundException();
                }
                return shape;
            }
            set
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    var param = new Dictionary<string, object>
                    {
                        ["MeshID"] = meshid,
                        ["ConvexData"] = value.SerializedData
                    };
                    conn.ReplaceInto("sculptmeshphysics", param);
                }
            }
        }

        PhysicsConvexShape ISimulationDataPhysicsConvexStorageInterface.this[ObjectPart.PrimitiveShape primShape]
        {
            get
            {
                PhysicsConvexShape shape;
                if (!((ISimulationDataPhysicsConvexStorageInterface)this).TryGetValue(primShape, out shape))
                {
                    throw new KeyNotFoundException();
                }
                return shape;
            }
            set
            {
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    var param = new Dictionary<string, object>
                    {
                        ["ShapeKey"] = primShape.Serialization,
                        ["ConvexData"] = value.SerializedData
                    };
                    conn.ReplaceInto("primphysics", param);
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(UUID meshid, out PhysicsConvexShape shape)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT ConvexData FROM meshphysics WHERE MeshID=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", meshid);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            shape = new PhysicsConvexShape()
                            {
                                SerializedData = dbReader.GetBytes("ConvexData")
                            };
                            return true;
                        }
                    }
                }
            }

            shape = null;
            return false;
        }

        bool ISimulationDataPhysicsConvexStorageInterface.TryGetValue(ObjectPart.PrimitiveShape primShape, out PhysicsConvexShape shape)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT ConvexData FROM primphysics WHERE ShapeKey=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", primShape.Serialization);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            shape = new PhysicsConvexShape()
                            {
                                SerializedData = dbReader.GetBytes("ConvexData")
                            };
                            return true;
                        }
                    }
                }
            }

            shape = null;
            return false;
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(UUID sculptmeshid)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM meshphysics WHERE MeshID=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", sculptmeshid);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.ContainsKey(ObjectPart.PrimitiveShape primShape)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM primphysics WHERE ShapeKey=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", primShape.Serialization);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(UUID sculptmeshid)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM meshphysics WHERE MeshID=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", sculptmeshid);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        bool ISimulationDataPhysicsConvexStorageInterface.Remove(ObjectPart.PrimitiveShape primShape)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM primphysics WHERE ShapeKey=@id", conn))
                {
                    cmd.Parameters.AddParameter("@id", primShape.Serialization);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        ICollection<UUID> ISimulationDataPhysicsConvexStorageInterface.KnownMeshIds
        {
            get
            {
                var sculptids = new List<UUID>();
                using (var conn = new SQLiteConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT MeshID FROM meshphysics", conn))
                    {
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                sculptids.Add(dbReader.GetUUID("MeshID"));
                            }
                        }
                    }
                }
                return sculptids;
            }
        }

        void ISimulationDataPhysicsConvexStorageInterface.RemoveAll()
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(() =>
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM primphysics WHERE 1", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SQLiteCommand("DELETE FROM meshphysics WHERE 1", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        void IPhysicsHacdCleanCache.CleanCache()
        {
            ((ISimulationDataPhysicsConvexStorageInterface)this).RemoveAll();
        }

        HacdCleanCacheOrder IPhysicsHacdCleanCache.CleanOrder => HacdCleanCacheOrder.AfterPhysicsShapeManager;
    }
}
