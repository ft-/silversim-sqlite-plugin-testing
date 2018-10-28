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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using System.ComponentModel;

namespace SilverSim.Database.SQLite._Migration
{
    public static partial class Migrator
    {
        private static bool m_DeleteTablesBefore = false;
        private static uint m_StopAtMigrationRevision = uint.MaxValue;
        private static uint m_MaxAvailableMigrationRevision = 1;

        [Description("SQLite migrator test control")]
        [PluginName("MigratorTestControl")]
        public sealed class TestControl : DBMigratorTestInterface, IPlugin
        {
            public override bool DeleteTablesBefore
            {
                get
                {
                    return m_DeleteTablesBefore;
                }

                set
                {
                    m_DeleteTablesBefore = value;
                }
            }

            public override uint StopAtMigrationRevision
            {
                get
                {
                    return m_StopAtMigrationRevision;
                }

                set
                {
                    m_StopAtMigrationRevision = value;
                }
            }

            public override uint MaxAvailableMigrationRevision => m_MaxAvailableMigrationRevision;

            public void Startup(ConfigurationLoader loader)
            {
                /* intentionally left empty */
            }
        }
    }
}
