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
using SilverSim.Types;
using SilverSim.Types.Parcel;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage
    {
        private static readonly IMigrationElement[] Migrations_Parcels = new IMigrationElement[]
        {
            #region Table parcels
            new SqlTable("parcels"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("LocalID") { IsNullAllowed = false, Default = 0 },
            new AddColumn<byte[]>("Bitmap") { IsLong = true },
            new AddColumn<int>("BitmapWidth") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("BitmapHeight") { IsNullAllowed = false, Default = 0 },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Description"),
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsGroupOwned") { IsNullAllowed = false, Default = false },
            new AddColumn<uint>("Area") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<uint>("AuctionID") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<UUI>("AuthBuyer") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<ParcelCategory>("Category") { IsNullAllowed = false, Default = ParcelCategory.Any },
            new AddColumn<Date>("ClaimDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<int>("ClaimPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UGI.Unknown },
            new AddColumn<ParcelFlags>("Flags") { IsNullAllowed = false, Default = ParcelFlags.None },
            new AddColumn<TeleportLandingType>("LandingType") { IsNullAllowed = false, Default = TeleportLandingType.Anywhere },
            new AddColumn<Vector3>("LandingPosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("LandingLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<ParcelStatus>("Status") { IsNullAllowed = false, Default = ParcelStatus.Leased },
            new AddColumn<string>("MusicURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("MediaURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UUID>("MediaID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SnapshotID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = -1 },
            new AddColumn<int>("OtherCleanTime") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("MediaAutoScale") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("RentPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<Vector3>("AABBMin") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("AABBMax") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<double>("ParcelPrimBonus") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<int>("PassPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<double>("PassHours") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<uint>("ActualArea") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<uint>("BillableArea") { IsNullAllowed = false, Default = (uint)0 },
            new PrimaryKeyInfo("RegionID", "ParcelID"),
            new NamedKeyInfo("ParcelNames", "RegionID", "Name"),
            new NamedKeyInfo("LocalIDs", "RegionID", "LocalID") { IsUnique = true },
            new TableRevision(2),
            new AddColumn<string>("MediaDescription") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("MediaType") { Cardinality = 255, IsNullAllowed = false, Default = "none/none" },
            new AddColumn<bool>("MediaLoop") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("MediaWidth") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("MediaHeight") { IsNullAllowed = false, Default = 0 },
            new TableRevision(3),
            new AddColumn<bool>("ObscureMedia") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("ObscureMusic") { IsNullAllowed = false, Default = false },
            new TableRevision(4),
            new AddColumn<bool>("SeeAvatars") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("GroupAvatarSounds") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AnyAvatarSounds") { IsNullAllowed = false, Default = false },
            new TableRevision(5),
            new AddColumn<bool>("IsPrivate") { IsNullAllowed = false, Default = false },
            new TableRevision(6),
            /* type corrections */
            new ChangeColumn<uint>("Area") { IsNullAllowed = false, Default = (uint)0 },
            new ChangeColumn<ParcelCategory>("Category") { IsNullAllowed = false, Default = ParcelCategory.Any },
            new ChangeColumn<TeleportLandingType>("LandingType") { IsNullAllowed = false, Default = TeleportLandingType.Anywhere },
            new ChangeColumn<bool>("MediaAutoScale") { IsNullAllowed = false, Default = false },
            new ChangeColumn<bool>("MediaLoop") { IsNullAllowed = false, Default = false },
            new TableRevision(7),
            new ChangeColumn<ParcelStatus>("Status") { IsNullAllowed = false, Default = ParcelStatus.Leased },
            new TableRevision(8),
            new ChangeColumn<int>("Area") { IsNullAllowed = false, Default = 0 },
            new ChangeColumn<int>("ActualArea") { IsNullAllowed = false, Default = 0 },
            new ChangeColumn<int>("BillableArea") { IsNullAllowed = false, Default = 0 },
            new TableRevision(9),
            new ChangeColumn<int>("SalePrice") { IsNullAllowed = false, Default = 0 },
            #endregion

            #region Table parcelaccesswhitelist
            new SqlTable("parcelaccesswhitelist"),
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Accessor") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("ExpiresAt") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(2),
            new NamedKeyInfo("Accessor", "Accessor"),
            new NamedKeyInfo("ExpiresAt", "ExpiresAt"),
            new TableRevision(3),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(4),
            new PrimaryKeyInfo("RegionID", "ParcelID", "Accessor"),
            #endregion

            #region Table parcelaccessblacklist
            new SqlTable("parcelaccessblacklist"),
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Accessor") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("ExpiresAt") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(2),
            new NamedKeyInfo("Accessor", "Accessor"),
            new NamedKeyInfo("ExpiresAt", "ExpiresAt"),
            new TableRevision(3),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(4),
            new PrimaryKeyInfo("RegionID", "ParcelID", "Accessor"),
            #endregion

            #region Table parcellandpasslist
            new SqlTable("parcellandpasslist"),
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Accessor") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("ExpiresAt") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(2),
            new NamedKeyInfo("Accessor", "Accessor"),
            new NamedKeyInfo("ExpiresAt", "ExpiresAt"),
            new TableRevision(3),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(4),
            new PrimaryKeyInfo("RegionID", "ParcelID", "Accessor"),
            #endregion

            #region Table parcelexperiences
            new SqlTable("parcelexperiences"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsAllowed") { IsNullAllowed = false, Default = false },
            new PrimaryKeyInfo("RegionID", "ParcelID", "ExperienceID"),
            new NamedKeyInfo("RegionID", "RegionID"),
            new NamedKeyInfo("RegionIDParcelID", "RegionID", "ParcelID"),
            #endregion
        };
    }
}
