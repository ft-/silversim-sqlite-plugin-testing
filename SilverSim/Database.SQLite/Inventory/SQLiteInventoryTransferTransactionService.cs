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
    [Description("SQLite Inventory Transfer Transaction Backend")]
    [PluginName("InventoryTransferTransaction")]
    class SQLiteInventoryTransferTransactionService : InventoryTransferTransactionServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("SQLITE INVENTORY TRANSFER TRANSACTION SERVICE");

        private readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("inventorytransfertransactions"),
            new AddColumn<UGUI>("srcagentid") { IsNullAllowed = false },
            new AddColumn<UGUI>("dstagentid") { IsNullAllowed = false },
            new AddColumn<UUID>("srctransactionid") { IsNullAllowed = false },
            new AddColumn<UUID>("dsttransactionid") { IsNullAllowed = false },
            new AddColumn<AssetType>("assettype") { IsNullAllowed = false },
            new AddColumn<UUID>("inventoryid") { IsNullAllowed = false },
            new PrimaryKeyInfo("dsttransactionid"),
            new NamedKeyInfo("dstagentidindex", "dstagentid")
        };

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

        public SQLiteInventoryTransferTransactionService(IConfig ownSection)
        {
            m_ConnectionString = SQLiteUtilities.BuildConnectionString(ownSection, m_Log);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }


        public void Remove(UUID userAccount)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM inventorytransfertransactions WHERE dstagentid LIKE @ownerid", connection))
                {
                    cmd.Parameters.AddParameter("@ownerid", userAccount + ";%");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool ContainsKey(UUID userid, UUID dstTransactionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT NULL FROM inventorytransfertransactions WHERE dsttransactionid = @transactionid AND dstagentid LIKE @userid", connection))
                {
                    cmd.Parameters.AddParameter("@transactionid", dstTransactionID);
                    cmd.Parameters.AddParameter("@userid", userid + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        public override bool TryGetValue(UUID userid, UUID dstTransactionID, out InventoryTransferInfo info)
        {
            info = null;
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM inventorytransfertransactions WHERE dsttransactionid = @transactionid AND dstagentid LIKE @userid", connection))
                {
                    cmd.Parameters.AddParameter("@transactionid", dstTransactionID);
                    cmd.Parameters.AddParameter("@userid", userid + "%");
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            info = new InventoryTransferInfo
                            {
                                SrcAgent = reader.GetUGUI("srcagentid"),
                                DstAgent = reader.GetUGUI("dstagentid"),
                                SrcTransactionID = reader.GetUUID("srctransactionid"),
                                DstTransactionID = reader.GetUUID("dsttransactionid"),
                                AssetType = reader.GetEnum<AssetType>("assettype"),
                                InventoryID = reader.GetUUID("inventoryid")
                            };
                        }
                    }
                }
            }
            return info != null;
        }

        public override void Store(InventoryTransferInfo info)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                var vals = new Dictionary<string, object>
                {
                    { "srcagentid", info.SrcAgent },
                    { "dstagentid", info.DstAgent },
                    { "srctransactionid", info.SrcTransactionID },
                    { "dsttransactionid", info.DstTransactionID },
                    { "assettype", info.AssetType },
                    { "inventoryid", info.InventoryID }
                };
                connection.ReplaceInto("inventorytransfertransactions", vals);
            }
        }

        public override bool Remove(UUID userid, UUID dstTransactionID)
        {
            using (var connection = new SQLiteConnection(m_ConnectionString))
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM inventorytransfertransactions WHERE dsttransactionid = @transactionid AND dstagentid LIKE @agentid", connection))
                {
                    cmd.Parameters.AddParameter("@agentid", userid + "%");
                    cmd.Parameters.AddParameter("@transactionid", dstTransactionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
