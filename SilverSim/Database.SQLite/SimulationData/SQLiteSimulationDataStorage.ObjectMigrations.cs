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
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage
    {
        private static readonly IMigrationElement[] Migrations_Objects = new IMigrationElement[]
        {
            #region Table objects
            new SqlTable("objects"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsVolumeDetect") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhantom") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhysics") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsTempOnRez") { IsNullAllowed = false, Default = false },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("LastOwner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("ID"),
            new TableRevision(2),
            new AddColumn<UUID>("OriginalAssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("NextOwnerAssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new TableRevision(3),
            new AddColumn<int>("Category") { IsNullAllowed = false, Default = 0 },
            new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice0") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice1") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice2") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice3") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice4") { IsNullAllowed = false, Default = 0 },
            new TableRevision(4),
            new AddColumn<Vector3>("AttachedPos") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<AttachmentPoint>("AttachPoint") { IsNullAllowed = false, Default = AttachmentPoint.NotAttached },
            new TableRevision(5),
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(6),
            new AddColumn<bool>("IsIncludedInSearch") { IsNullAllowed = false, Default = false },
            new TableRevision(7),
            new AddColumn<bool>("IsRotateXEnabled") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsRotateYEnabled") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsRotateZEnabled") { IsNullAllowed = false, Default = true },
            new TableRevision(8),
            new DropColumn("IsVolumeDetect"),
            new DropColumn("IsPhantom"),
            new DropColumn("IsPhysics"),
            new DropColumn("IsRotateXEnabled"),
            new DropColumn("IsRotateYEnabled"),
            new DropColumn("IsRotateZEnabled"),
            new TableRevision(9),
            new AddColumn<UUID>("RezzingObjectID") {IsNullAllowed = false, Default = UUID.Zero },
            #endregion

            #region Table prims
            new SqlTable("prims"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("RootPartID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("LinkNumber") { IsNullAllowed = false, Default = 0 },
            new AddColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new AddColumn<Vector3>("Position") { IsNullAllowed = false },
            new AddColumn<Quaternion>("Rotation") { IsNullAllowed = false },
            new AddColumn<string>("SitText"),
            new AddColumn<string>("TouchText"),
            new AddColumn<UUI>("Creator") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<byte[]>("DynAttrs") { IsNullAllowed = false },
            new AddColumn<Vector3>("SitTargetOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Quaternion>("SitTargetOrientation") { IsNullAllowed = false, Default = Quaternion.Identity },
            new AddColumn<PrimitivePhysicsShapeType>("PhysicsShapeType") { IsNullAllowed = false, Default = PrimitivePhysicsShapeType.Prim },
            new AddColumn<PrimitiveMaterial>("Material") { IsNullAllowed = false, Default = PrimitiveMaterial.Wood },
            new AddColumn<Vector3>("Size") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("Slice") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<string>("MediaURL") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<Vector3>("AngularVelocity") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<byte[]>("LightData"),
            new AddColumn<byte[]>("HoverTextData"),
            new AddColumn<byte[]>("FlexibleData"),
            new AddColumn<byte[]>("LoopedSoundData"),
            new AddColumn<byte[]>("ImpactSoundData"),
            new AddColumn<byte[]>("PrimitiveShapeData"),
            new AddColumn<byte[]>("ParticleSystem"),
            new AddColumn<byte[]>("TextureEntryBytes"),
            new AddColumn<int>("ScriptAccessPin") { IsNullAllowed = false, Default = 0},
            new AddColumn<byte[]>("TextureAnimationBytes"),
            new PrimaryKeyInfo("ID", "RootPartID"),
            new NamedKeyInfo("ID", "ID") { IsUnique = true },
            new NamedKeyInfo("RootPartID", "RootPartID"),
            new TableRevision(2),
            new AddColumn<Vector3>("CameraEyeOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("CameraAtOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<bool>("ForceMouselook") { IsNullAllowed = false, Default = false },
            new TableRevision(3),
            /* type corrections */
            new ChangeColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(4),
            new AddColumn<InventoryPermissionsMask>("BasePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.All | InventoryPermissionsMask.Export },
            new AddColumn<InventoryPermissionsMask>("CurrentPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.All | InventoryPermissionsMask.Export },
            new AddColumn<InventoryPermissionsMask>("EveryOnePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("GroupPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("NextOwnerPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new TableRevision(5),
            new ChangeColumn<byte[]>("LightData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("FlexibleData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("LoopedSoundData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("ImpactSoundData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("PrimitiveShapeData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("ParticleSystem") { Cardinality = 255 },
            new ChangeColumn<byte[]>("TextureAnimationBytes") { Cardinality = 255 },
            new TableRevision(6),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new PrimaryKeyInfo("RegionID", "ID", "RootPartID"),
            new TableRevision(7),
            new AddColumn<ClickActionType>("ClickAction") { IsNullAllowed = false, Default = ClickActionType.None },
            new TableRevision(8),
            new AddColumn<bool>("IsPassCollisions") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPassTouches") { IsNullAllowed = false, Default = false },
            new AddColumn<Vector3>("Velocity") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<bool>("IsSoundQueueing") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsAllowedDrop") { IsNullAllowed = false, Default = false },
            new AddColumn<double>("PhysicsDensity") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("PhysicsFriction") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("PhysicsRestitution") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("PhysicsGravityMultiplier") { IsNullAllowed = false, Default = (double)0 },
            new TableRevision(9),
            new DropColumn("IsPassTouches"),
            new DropColumn("IsPassCollisions"),
            new AddColumn<PassEventMode>("PassTouchMode") { IsNullAllowed = false, Default = PassEventMode.Always },
            new AddColumn<PassEventMode>("PassCollisionMode") { IsNullAllowed = false, Default = PassEventMode.IfNotHandled },
            new TableRevision(10),
            new AddColumn<bool>("IsVolumeDetect") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhantom") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhysics") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsRotateXEnabled") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsRotateYEnabled") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsRotateZEnabled") { IsNullAllowed = false, Default = true },
            new TableRevision(11),
            new AddColumn<PathfindingType>("PathfindingType") {IsNullAllowed = false, Default = PathfindingType.LegacyLinkset },
            new TableRevision(12),
            new AddColumn<byte[]>("ProjectionData"),
            new TableRevision(13),
            new AddColumn<bool>("IsBlockGrab") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsSandbox") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsDieAtEdge") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsReturnAtEdge") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsBlockGrabObject") { IsNullAllowed = false, Default = false },
            new TableRevision(14),
            new AddColumn<Vector3>("SandboxOrigin") { IsNullAllowed = false, Default = Vector3.Zero },
            new TableRevision(15),
            new AddColumn<byte[]>("ExtendedMeshData"),
            new TableRevision(16),
            new AddColumn<double>("WalkableCoefficientA") { IsNullAllowed = false, Default = 1.0 },
            new AddColumn<double>("WalkableCoefficientB") { IsNullAllowed = false, Default = 1.0 },
            new AddColumn<double>("WalkableCoefficientC") { IsNullAllowed = false, Default = 1.0 },
            new AddColumn<double>("WalkableCoefficientD") { IsNullAllowed = false, Default = 1.0 },
            new TableRevision(17),
            new AddColumn<double>("WalkableCoefficientAvatar") { IsNullAllowed = false, Default = 1.0 },
            new TableRevision(18),
            new AddColumn<Date>("RezDate") { IsNullAllowed = false },
            new TableRevision(19),
            new AddColumn<string>("SitAnimation") { IsNullAllowed = false, Cardinality = 255, Default = string.Empty },
            #endregion

            #region Table primitems
            new SqlTable("primitems"),
            new AddColumn<UUID>("PrimID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("InventoryID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 255, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 255, Default = string.Empty },
            new AddColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new AddColumn<UUID>("AssetId") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<AssetType>("AssetType") { IsNullAllowed = false, Default = AssetType.Unknown },
            new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<UUI>("Creator") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("GroupOwned") { IsNullAllowed = false },
            new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
            new AddColumn<UUI>("LastOwner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false },
            new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<InventoryPermissionsMask>("BasePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("CurrentPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("EveryOnePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("GroupPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("NextOwnerPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
            new AddColumn<InventoryPermissionsMask>("SalePermMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new PrimaryKeyInfo("PrimID", "InventoryID"),
            new NamedKeyInfo("primID", "PrimID"),
            new TableRevision(2),
            new AddColumn<UUI>("PermsGranter") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<ScriptPermissions>("PermsMask") { IsNullAllowed = false, Default = ScriptPermissions.None },
            new TableRevision(3),
            /* type corrections */
            new ChangeColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new TableRevision(4),
            /* type corrections */
            new ChangeColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(5),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new PrimaryKeyInfo("RegionID", "PrimID", "InventoryID"),
            new TableRevision(6),
            new AddColumn<UUID>("NextOwnerAssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new TableRevision(7),
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = 10 },
            new TableRevision(8),
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false, Default = UUID.Zero },
            new TableRevision(9),
            new DropColumn("ParentFolderID")
            #endregion
        };
    }
}
