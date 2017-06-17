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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.Collections.Generic;
using System.Data.SQLite;
using EnvController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Database.SQLite.SimulationData
{
    public sealed partial class SQLiteSimulationDataStorage : ISimulationDataLightShareStorageInterface
    {
        bool ISimulationDataLightShareStorageInterface.TryGetValue(UUID regionID, out EnvController.WindlightSkyData skyData, out EnvController.WindlightWaterData waterData)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM lightshare WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            skyData = EnvController.WindlightSkyData.Defaults;
                            waterData = EnvController.WindlightWaterData.Defaults;
                            return false;
                        }

                        skyData = new EnvController.WindlightSkyData()
                        {
                            Ambient = reader.GetWLVector4("Ambient"),
                            CloudColor = reader.GetWLVector4("CloudColor"),
                            CloudCoverage = (double)reader["CloudCoverage"],
                            BlueDensity = reader.GetWLVector4("BlueDensity"),
                            CloudDetailXYDensity = reader.GetVector3("CloudDetailXYDensity"),
                            CloudScale = (double)reader["CloudScale"],
                            CloudScroll = reader.GetWLVector2("CloudScroll"),
                            CloudScrollXLock = reader.GetBool("CloudScrollXLock"),
                            CloudScrollYLock = reader.GetBool("CloudScrollYLock"),
                            CloudXYDensity = reader.GetVector3("CloudXYDensity"),
                            DensityMultiplier = (double)reader["DensityMultiplier"],
                            DistanceMultiplier = (double)reader["DistanceMultiplier"],
                            DrawClassicClouds = reader.GetBool("DrawClassicClouds"),
                            EastAngle = (double)reader["EastAngle"],
                            HazeDensity = (double)reader["HazeDensity"],
                            HazeHorizon = (double)reader["HazeHorizon"],
                            Horizon = reader.GetWLVector4("Horizon"),
                            MaxAltitude = (int)(long)reader["MaxAltitude"],
                            SceneGamma = (double)reader["SceneGamma"],
                            SunGlowFocus = (double)reader["SunGlowFocus"],
                            SunGlowSize = (double)reader["SunGlowSize"],
                            SunMoonColor = reader.GetWLVector4("SunMoonColor"),
                            SunMoonPosition = (double)reader["SunMoonPosition"]
                        };
                        waterData = new EnvController.WindlightWaterData()
                        {
                            BigWaveDirection = reader.GetWLVector2("BigWaveDirection"),
                            LittleWaveDirection = reader.GetWLVector2("LittleWaveDirection"),
                            BlurMultiplier = (double)reader["BlurMultiplier"],
                            FresnelScale = (double)reader["FresnelScale"],
                            FresnelOffset = (double)reader["FresnelOffset"],
                            NormalMapTexture = reader.GetUUID("NormalMapTexture"),
                            ReflectionWaveletScale = reader.GetVector3("ReflectionWaveletScale"),
                            RefractScaleAbove = (double)reader["RefractScaleAbove"],
                            RefractScaleBelow = (double)reader["RefractScaleBelow"],
                            UnderwaterFogModifier = (double)reader["UnderwaterFogModifier"],
                            Color = reader.GetColor("WaterColor"),
                            FogDensityExponent = (double)reader["FogDensityExponent"]
                        };
                        return true;
                    }
                }
            }
        }

        void ISimulationDataLightShareStorageInterface.Store(UUID regionID, EnvController.WindlightSkyData skyData, EnvController.WindlightWaterData waterData)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();

                var data = new Dictionary<string, object>
                {
                    ["RegionID"] = regionID,
                    ["Ambient"] = skyData.Ambient,
                    ["CloudColor"] = skyData.CloudColor,
                    ["CloudCoverage"] = skyData.CloudCoverage,
                    ["BlueDensity"] = skyData.BlueDensity,
                    ["CloudDetailXYDensity"] = skyData.CloudDetailXYDensity,
                    ["CloudScale"] = skyData.CloudScale,
                    ["CloudScroll"] = skyData.CloudScroll,
                    ["CloudScrollXLock"] = skyData.CloudScrollXLock,
                    ["CloudScrollYLock"] = skyData.CloudScrollYLock,
                    ["CloudXYDensity"] = skyData.CloudXYDensity,
                    ["DensityMultiplier"] = skyData.DensityMultiplier,
                    ["DistanceMultiplier"] = skyData.DistanceMultiplier,
                    ["DrawClassicClouds"] = skyData.DrawClassicClouds,
                    ["EastAngle"] = skyData.EastAngle,
                    ["HazeDensity"] = skyData.HazeDensity,
                    ["HazeHorizon"] = skyData.HazeHorizon,
                    ["Horizon"] = skyData.Horizon,
                    ["MaxAltitude"] = skyData.MaxAltitude,
                    ["SceneGamma"] = skyData.SceneGamma,
                    ["StarBrightness"] = skyData.StarBrightness,
                    ["SunGlowFocus"] = skyData.SunGlowFocus,
                    ["SunGlowSize"] = skyData.SunGlowSize,
                    ["SunMoonColor"] = skyData.SunMoonColor,
                    ["SunMoonPosition"] = skyData.SunMoonPosition,

                    ["BigWaveDirection"] = waterData.BigWaveDirection,
                    ["LittleWaveDirection"] = waterData.LittleWaveDirection,
                    ["BlurMultiplier"] = waterData.BlurMultiplier,
                    ["FresnelScale"] = waterData.FresnelScale,
                    ["FresnelOffset"] = waterData.FresnelOffset,
                    ["NormalMapTexture"] = waterData.NormalMapTexture,
                    ["ReflectionWaveletScale"] = waterData.ReflectionWaveletScale,
                    ["RefractScaleAbove"] = waterData.RefractScaleAbove,
                    ["RefractScaleBelow"] = waterData.RefractScaleBelow,
                    ["UnderwaterFogModifier"] = waterData.UnderwaterFogModifier,
                    ["WaterColor"] = waterData.Color,
                    ["FogDensityExponent"] = waterData.FogDensityExponent
                };
                conn.ReplaceInto("lightshare", data);
            }
        }

        bool ISimulationDataLightShareStorageInterface.Remove(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM lightshare WHERE RegionID = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
