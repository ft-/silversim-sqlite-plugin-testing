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
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage
    {
        private static readonly IMigrationElement[] Migrations_Regions = new IMigrationElement[]
        {
            #region Table terrains
            new SqlTable("terrains"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("PatchID") { IsNullAllowed = false },
            new AddColumn<byte[]>("TerrainData"),
            new PrimaryKeyInfo("RegionID", "PatchID"),
            #endregion

            #region Table defaultterrains
            new SqlTable("defaultterrains"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("PatchID") { IsNullAllowed = false },
            new AddColumn<byte[]>("TerrainData"),
            new PrimaryKeyInfo("RegionID", "PatchID"),
            #endregion

            #region Table environmentsettings
            new SqlTable("environmentsettings"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<byte[]>("EnvironmentSettings") { IsLong = true },
            new PrimaryKeyInfo("RegionID"),
            #endregion

            #region Table environmentcontroller
            new SqlTable("environmentcontroller"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<byte[]>("SerializedData") { IsLong = true },
            new PrimaryKeyInfo("RegionID"),
            #endregion

            #region Table lightshare
            new SqlTable("lightshare"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<EnvironmentController.WLVector4>("Ambient") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("CloudColor") { IsNullAllowed = false },
            new AddColumn<double>("CloudCoverage") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudDetailXYDensity") { IsNullAllowed = false },
            new AddColumn<double>("CloudScale") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudScroll") { IsNullAllowed = false },
            new AddColumn<bool>("CloudScrollXLock") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("CloudScrollYLock") { IsNullAllowed = false, Default = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudXYDensity") { IsNullAllowed = false },
            new AddColumn<double>("DensityMultiplier") { IsNullAllowed = false },
            new AddColumn<double>("DistanceMultiplier") { IsNullAllowed = false },
            new AddColumn<bool>("DrawClassicClouds") { IsNullAllowed = false },
            new AddColumn<double>("EastAngle") { IsNullAllowed = false },
            new AddColumn<double>("HazeDensity") { IsNullAllowed = false },
            new AddColumn<double>("HazeHorizon") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("Horizon") { IsNullAllowed = false },
            new AddColumn<int>("MaxAltitude") { IsNullAllowed = false },
            new AddColumn<double>("StarBrightness") { IsNullAllowed = false },
            new AddColumn<double>("SunGlowFocus") { IsNullAllowed = false },
            new AddColumn<double>("SunGlowSize") { IsNullAllowed = false },
            new AddColumn<double>("SceneGamma") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("SunMoonColor") { IsNullAllowed = false },
            new AddColumn<double>("SunMoonPosition") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("BigWaveDirection") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("LittleWaveDirection") { IsNullAllowed = false },
            new AddColumn<double>("BlurMultiplier") { IsNullAllowed = false },
            new AddColumn<double>("FresnelScale") { IsNullAllowed = false },
            new AddColumn<double>("FresnelOffset") { IsNullAllowed = false },
            new AddColumn<UUID>("NormalMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<EnvironmentController.WLVector2>("ReflectionWaveletScale") { IsNullAllowed = false },
            new AddColumn<double>("RefractScaleAbove") { IsNullAllowed = false },
            new AddColumn<double>("RefractScaleBelow") { IsNullAllowed = false },
            new AddColumn<double>("UnderwaterFogModifier") { IsNullAllowed  = false },
            new AddColumn<Color>("WaterColor") { IsNullAllowed = false },
            new AddColumn<double>("FogDensityExponent") { IsNullAllowed = false },
            new PrimaryKeyInfo("RegionID"),
            new TableRevision(2),
            new AddColumn<EnvironmentController.WLVector4>("BlueDensity") { IsNullAllowed = false },
            new TableRevision(3),
            new ChangeColumn<Vector3>("CloudDetailXYDensity") { IsNullAllowed = false },
            new ChangeColumn<Vector3>("CloudXYDensity") { IsNullAllowed = false },
            new ChangeColumn<Vector3>("ReflectionWaveletScale") { IsNullAllowed = false },
            #endregion

            #region Table spawnpoints
            new SqlTable("spawnpoints"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("Distance") { IsNullAllowed = false },
            new NamedKeyInfo("RegionID", "RegionID"),
            #endregion

            #region Table scriptstates
            new SqlTable("scriptstates"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("PrimID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ItemID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("ScriptState") { IsLong = true },
            new PrimaryKeyInfo("RegionID", "PrimID", "ItemID"),
            new TableRevision(2),
            new ChangeColumn<byte[]>("ScriptState") { IsLong = true },
            #endregion

            #region Table regionsettings
            new SqlTable("regionsettings"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("BlockTerraform") { IsNullAllowed = false , Default = false },
            new AddColumn<bool>("BlockFly") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowDamage") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("RestrictPushing") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowLandResell") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("AllowLandJoinDivide") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("BlockShowInSearch") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("AgentLimit") { IsNullAllowed = false, Default = 40 },
            new AddColumn<double>("ObjectBonus") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<bool>("DisableScripts") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("DisableCollisions") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("DisablePhysics") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("BlockFlyOver") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("Sandbox") { IsNullAllowed = false, Default = false },
            new AddColumn<UUID>("TerrainTexture1") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture2") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture3") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture4") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TelehubObject") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<double>("Elevation1NW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2NW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1NE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2NE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1SE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2SE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1SW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2SW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("WaterHeight") { IsNullAllowed = false, Default = (double)20 },
            new AddColumn<double>("TerrainRaiseLimit") { IsNullAllowed = false, Default = (double)100 },
            new AddColumn<double>("TerrainLowerLimit") { IsNullAllowed = false, Default = (double)-100 },
            new PrimaryKeyInfo("RegionID"),
            new TableRevision(2),
            new AddColumn<bool>("UseEstateSun") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsSunFixed") { IsNullAllowed = false, Default = false },
            new AddColumn<double>("SunPosition") { IsNullAllowed = false, Default = (double)0 },
            new TableRevision(3),
            new AddColumn<bool>("BlockDwell") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("ResetHomeOnTeleport") { IsNullAllowed = false, Default = false },
            new TableRevision(4),
            new AddColumn<bool>("AllowDirectTeleport") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowLandmark") { IsNullAllowed = false, Default = false },
            new TableRevision(5),
            new AddColumn<int>("MaxBasePrims") { IsNullAllowed = false, Default = 45000 },
            new TableRevision(6),
            new AddColumn<byte[]>("WalkableCoefficientsData"),
            #endregion

            #region Table regionexperiences
            new SqlTable("regionexperiences"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsAllowed") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsTrusted") { IsNullAllowed = false, Default = false },
            new TableRevision(2),
            new DropColumn("IsTrusted"),
            new TableRevision(3),
            new PrimaryKeyInfo("RegionID", "ExperienceID"),
            #endregion

            #region Table regiontrustedexperiences
            new SqlTable("regiontrustedexperiences"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false },
            new AddColumn<UUID>("ExperienceID") { IsNullAllowed = false },
            new TableRevision(2),
            new PrimaryKeyInfo("RegionID", "ExperienceID"),
            #endregion
        };
    }
}
