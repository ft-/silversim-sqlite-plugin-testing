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
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Grid
{
    [Description("SQLite Grid Backend")]
    [PluginName("Grid")]
    [ServerParam("DeleteOnUnregister", Type = ServerParamType.GlobalOnly, ParameterType = typeof(bool), DefaultValue = false)]
    [ServerParam("AllowDuplicateRegionNames", Type = ServerParamType.GlobalOnly, ParameterType = typeof(bool), DefaultValue = false)]
    public sealed partial class SQLiteGridService : GridServiceInterface, IDBServiceInterface, IPlugin, IServerParamListener
    {
        private readonly string m_ConnectionString;
        private readonly string m_TableName;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE GRID SERVICE");
        private bool m_IsDeleteOnUnregister;
        private bool m_AllowDuplicateRegionNames;
        private readonly bool m_UseRegionDefaultServices;
        private List<RegionDefaultFlagsServiceInterface> m_RegionDefaultServices;

        [ServerParam("DeleteOnUnregister")]
        public void DeleteOnUnregisterUpdated(UUID regionid, string value)
        {
            if (regionid == UUID.Zero)
            {
                m_IsDeleteOnUnregister = bool.Parse(value);
            }
        }

        [ServerParam("AllowDuplicateRegionNames")]
        public void AllowDuplicateRegionNamesUpdated(UUID regionid, string value)
        {
            if (regionid == UUID.Zero)
            {
                m_AllowDuplicateRegionNames = bool.Parse(value);
            }
        }

        #region Constructor
        public SQLiteGridService(IConfig ownSection)
        {
            m_UseRegionDefaultServices = ownSection.GetBoolean("UseRegionDefaultServices", true);
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
            m_TableName = ownSection.GetString("TableName", "regions");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_RegionDefaultServices = loader.GetServicesByValue<RegionDefaultFlagsServiceInterface>();
        }
        #endregion

        public void VerifyConnection()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                var migrations = new List<IMigrationElement>
                {
                    new SqlTable(m_TableName)
                };
                migrations.AddRange(Migrations);
                connection.MigrateTables(migrations.ToArray(), m_Log);
            }
        }

        #region Accessors
        public override bool TryGetValue(UUID regionID, out RegionInfo rInfo)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE uuid = @id LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@id", regionID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID regionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM " + m_TableName + " WHERE uuid = @id LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@id", regionID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        public override bool TryGetValue(uint gridX, uint gridY, out RegionInfo rInfo)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE locX <= @x AND locY <= @y AND locX + sizeX > @x AND locY + sizeY > @y LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@x", gridX);
                    cmd.Parameters.AddParameter("@y", gridY);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(uint gridX, uint gridY)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE locX <= @x AND locY <= @y AND locX + sizeX > @x AND locY + sizeY > @y LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@x", gridX);
                    cmd.Parameters.AddParameter("@y", gridY);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }

        public override bool TryGetValue(string regionName, out RegionInfo rInfo)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE regionName = @name LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@name", regionName);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            rInfo = ToRegionInfo(dbReader);
                            return true;
                        }
                    }
                }
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(string regionName)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM " + m_TableName + " WHERE regionName = @name LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("@name", regionName);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        return dbReader.Read();
                    }
                }
            }
        }
        #endregion

        #region dbData to RegionInfo
        private RegionInfo ToRegionInfo(SQLiteDataReader dbReader) => new RegionInfo
        {
            ID = dbReader.GetUUID("uuid"),
            Name = (string)dbReader["regionName"],
            RegionSecret = (string)dbReader["regionSecret"],
            ServerIP = (string)dbReader["serverIP"],
            ServerPort = (uint)(long)dbReader["serverPort"],
            ServerURI = (string)dbReader["serverURI"],
            Location = dbReader.GetGridVector("loc"),
            RegionMapTexture = dbReader.GetUUID("regionMapTexture"),
            ServerHttpPort = (uint)(long)dbReader["serverHttpPort"],
            Owner = dbReader.GetUGUI("owner"),
            Access = dbReader.GetEnum<RegionAccess>("access"),
            Size = dbReader.GetGridVector("size"),
            Flags = dbReader.GetEnum<RegionFlags>("flags"),
            AuthenticatingToken = (string)dbReader["AuthenticatingToken"],
            AuthenticatingPrincipal = dbReader.GetUGUI("AuthenticatingPrincipalID"),
            ParcelMapTexture = dbReader.GetUUID("parcelMapTexture"),
            ProductName = (string)dbReader["ProductName"]
        };
        #endregion

        #region Region Registration
        public override void AddRegionFlags(UUID regionID, RegionFlags setflags)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE " + m_TableName + " SET flags = flags | @flags WHERE uuid = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@flags", setflags);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void RemoveRegionFlags(UUID regionID, RegionFlags removeflags)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("UPDATE " + m_TableName + " SET flags = flags & ~@flags WHERE uuid = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@flags", removeflags);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void RegisterRegion(RegionInfo regionInfo)
        {
            RegisterRegion(regionInfo, false);
        }

        public override void RegisterRegion(RegionInfo regionInfo, bool keepOnlineUnmodified)
        {
            foreach (RegionDefaultFlagsServiceInterface service in m_RegionDefaultServices)
            {
                regionInfo.Flags |= service.GetRegionDefaultFlags(regionInfo.ID);
            }

            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();

                if (!m_AllowDuplicateRegionNames)
                {
                    using (var cmd = new SQLiteCommand("SELECT uuid FROM " + m_TableName + " WHERE \"regionName\" = @name LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("@name", regionInfo.Name);
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read() &&
                                dbReader.GetUUID("uuid") != regionInfo.ID)
                            {
                                throw new GridRegionUpdateFailedException("Duplicate region name");
                            }
                        }
                    }
                }

                if (keepOnlineUnmodified)
                {
                    using (var cmd = new SQLiteCommand("SELECT flags FROM " + m_TableName + " WHERE uuid = @id LIMIT 1", conn))
                    {
                        cmd.Parameters.AddParameter("@id", regionInfo.ID);
                        using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                RegionFlags flags = dbReader.GetEnum<RegionFlags>("flags");
                                regionInfo.Flags &= ~RegionFlags.RegionOnline;
                                regionInfo.Flags |= (flags & RegionFlags.RegionOnline);
                            }
                        }
                    }
                }

                /* we have to give checks for all intersection variants */
                using (var cmd = new SQLiteCommand("SELECT uuid FROM " + m_TableName + " WHERE (" +
                            "(locX >= @minx AND locY >= @miny AND locX < @maxx AND locY < @maxy) OR " +
                            "(locX + sizeX > @minx AND locY+sizeY > @miny AND locX + sizeX < @maxx AND locY + sizeY < @maxy)" +
                            ") AND (NOT uuid = @regionid) LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@min", regionInfo.Location);
                    cmd.Parameters.AddParameter("@max", regionInfo.Location + regionInfo.Size);
                    cmd.Parameters.AddParameter("@regionid", regionInfo.ID);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read() &&
                            dbReader.GetUUID("uuid") != regionInfo.ID)
                        {
                            throw new GridRegionUpdateFailedException("Overlapping regions");
                        }
                    }
                }

                var regionData = new Dictionary<string, object>
                {
                    ["uuid"] = regionInfo.ID,
                    ["regionName"] = regionInfo.Name,
                    ["loc"] = regionInfo.Location,
                    ["size"] = regionInfo.Size,
                    ["regionName"] = regionInfo.Name,
                    ["serverIP"] = regionInfo.ServerIP,
                    ["serverHttpPort"] = regionInfo.ServerHttpPort,
                    ["serverURI"] = regionInfo.ServerURI,
                    ["serverPort"] = regionInfo.ServerPort,
                    ["regionMapTexture"] = regionInfo.RegionMapTexture,
                    ["parcelMapTexture"] = regionInfo.ParcelMapTexture,
                    ["access"] = regionInfo.Access,
                    ["regionSecret"] = regionInfo.RegionSecret,
                    ["owner"] = regionInfo.Owner,
                    ["AuthenticatingToken"] = regionInfo.AuthenticatingToken,
                    ["AuthenticatingPrincipalID"] = regionInfo.AuthenticatingPrincipal,
                    ["flags"] = regionInfo.Flags,
                    ["ProductName"] = regionInfo.ProductName
                };
                conn.ReplaceInto(m_TableName, regionData);
            }
        }

        public override void UnregisterRegion(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();

                if (m_IsDeleteOnUnregister)
                {
                    /* we handoff most stuff to mysql here */
                    /* first line deletes only when region is not persistent */
                    using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE uuid = @regionid AND (flags & @persistent) != 0", conn))
                    {
                        cmd.Parameters.AddParameter("@regionid", regionID);
                        cmd.Parameters.AddParameter("@persistent", RegionFlags.Persistent);
                        cmd.ExecuteNonQuery();
                    }

                    /* second step is to set it offline when it is persistent */
                }

                using (var cmd = new SQLiteCommand("UPDATE " + m_TableName + " SET flags = flags - @online, last_seen=@unixtime WHERE uuid = @regionid AND (flags & @online) != 0", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.Parameters.AddParameter("@online", RegionFlags.RegionOnline);
                    cmd.Parameters.AddParameter("@unixtime", Date.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteRegion(UUID regionID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM " + m_TableName + " WHERE uuid = @regionid", conn))
                {
                    cmd.Parameters.AddParameter("@regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region List accessors
        private List<RegionInfo> GetRegionsByFlag(RegionFlags flags)
        {
            var result = new List<RegionInfo>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM regions WHERE flags & @flag != 0", connection))
                {
                    cmd.Parameters.AddParameter("@flag", flags);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetHyperlinks() =>
            GetRegionsByFlag(RegionFlags.Hyperlink);

        public override List<RegionInfo> GetDefaultRegions() =>
            GetRegionsByFlag(RegionFlags.DefaultRegion);

        public override List<RegionInfo> GetOnlineRegions() =>
            GetRegionsByFlag(RegionFlags.RegionOnline);

        public override List<RegionInfo> GetFallbackRegions() =>
            GetRegionsByFlag(RegionFlags.FallbackRegion);

        public override List<RegionInfo> GetDefaultIntergridRegions() =>
            GetRegionsByFlag(RegionFlags.DefaultIntergridRegion);

        public override List<RegionInfo> GetRegionsByRange(GridVector min, GridVector max)
        {
            var result = new List<RegionInfo>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE " +
                        "locX+sizeX > @xmin AND locX <= @xmax AND locY+sizeY > @ymin AND locY <= @ymax", connection))
                {
                    cmd.Parameters.AddParameter("@xmin", min.X);
                    cmd.Parameters.AddParameter("@ymin", min.Y);
                    cmd.Parameters.AddParameter("@xmax", max.X);
                    cmd.Parameters.AddParameter("@ymax", max.Y);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetNeighbours(UUID regionID)
        {
            RegionInfo ri = this[regionID];
            var result = new List<RegionInfo>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE (" +
                                                            "((locX = @maxX OR locX + sizeX = @locX)  AND " +
                                                            "(locY <= @maxY AND locY + sizeY >= @locY))" +
                                                            " OR " +
                                                            "((locY = @maxY OR locY + sizeY = @locY) AND " +
                                                            "(locX <= @maxX AND locX + sizeX >= @locX))" +
                                                            ") AND", connection))
                {
                    cmd.Parameters.AddParameter("@locX", ri.Location.X);
                    cmd.Parameters.AddParameter("@locY", ri.Location.Y);
                    cmd.Parameters.AddParameter("@maxX", ri.Size.X + ri.Location.X);
                    cmd.Parameters.AddParameter("@maxY", ri.Size.Y + ri.Location.Y);
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> GetAllRegions()
        {
            var result = new List<RegionInfo>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName, connection))
                {
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override List<RegionInfo> SearchRegionsByName(string searchString)
        {
            var result = new List<RegionInfo>();

            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();

                using (var cmd = new SQLiteCommand("SELECT * FROM " + m_TableName + " WHERE regionName LIKE '" + searchString.ToSQLiteQuoted() + "%'", connection))
                {
                    using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            result.Add(ToRegionInfo(dbReader));
                        }
                    }
                }
            }

            return result;
        }

        public override Dictionary<string, string> GetGridExtraFeatures() =>
            new Dictionary<string, string>();

        #endregion

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            /* no SqlTable here since we are adding it when processing migrations */
            new AddColumn<UUID>("uuid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("regionName") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("regionSecret") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("serverIP") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<uint>("serverPort") { IsNullAllowed = false },
            new AddColumn<string>("serverURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<GridVector>("loc") { IsNullAllowed = false, Default = GridVector.Zero },
            new AddColumn<UUID>("regionMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("serverHttpPort") { IsNullAllowed = false },
            new AddColumn<UGUI>("owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("access") { IsNullAllowed = false, Default = (uint)13 },
            new AddColumn<UUID>("ScopeID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<GridVector>("size") { IsNullAllowed = false, Default = GridVector.Zero },
            new AddColumn<uint>("flags") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<Date>("last_seen") { IsNullAllowed = false , Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<string>("AuthenticatingToken") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UGUI>("AuthenticatingPrincipalID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("parcelMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("uuid"),
            new NamedKeyInfo("regionName", "regionName"),
            new NamedKeyInfo("ScopeID", "ScopeID"),
            new NamedKeyInfo("flags", "flags"),
            new AddColumn<string>("ProductName") { Cardinality = 255, IsNullAllowed = false, Default = "Mainland" },
            new TableRevision(2),
            new DropNamedKeyInfo("ScopeID"),
            new DropColumn("ScopeID")
        };
    }
}
