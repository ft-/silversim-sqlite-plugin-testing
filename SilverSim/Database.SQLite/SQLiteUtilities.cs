﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Main.Common;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Database.SQLite
{
    public static class SQLiteUtilities
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        static private bool m_Inited;
        static private readonly object m_InitLock = new object();

        static SQLiteUtilities()
        {
            if (!m_Inited)
            {
                lock (m_InitLock)
                {
                    if (!m_Inited)
                    {
                        string installationBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        /* preload necessary windows dll */
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            if (Environment.Is64BitProcess)
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.Combine(installationBinPath, "../platform-libs/windows/64/SQLite.Interop.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/64/SQLite.Interop.dll");
                                }
                            }
                            else
                            {
                                if (IntPtr.Zero == LoadLibrary(Path.Combine(installationBinPath, "../platform-libs/windows/32/SQLite.Interop.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/32/SQLite.Interop.dll");
                                }
                            }
                        }
                        m_Inited = true;
                    }
                }
            }
        }

        public static string ToSQLiteQuoted(this string s)
        {
            var sb = new StringBuilder();
            sb.Append("'");
            foreach(char c in s)
            {
                if(c == '\'')
                {
                    sb.Append("''");
                }
                else
                {
                    sb.Append(c);
                }
            }
            sb.Append("'");
            return sb.ToString();
        }

        #region Connection String Creator
        public static string BuildConnectionString(IConfig config, ILog log)
        {
            string configName = config.Name;
            bool containsDataSource = config.Contains("DataSource");
            if (!containsDataSource && !config.Contains("DataSourceDefault"))
            {
                log.FatalFormat("[SQLITE CONFIG]: Parameter 'DataSource' missing in [{0}]", configName);
                throw new ConfigurationLoader.ConfigurationErrorException();
            }
            var sb = new SQLiteConnectionStringBuilder
            {
                DataSource = config.GetString(containsDataSource ? "DataSource" : "DataSourceDefault"),
                Password = config.GetString("Password", string.Empty)
            };
            return sb.ToString();
        }
        #endregion

        #region Exceptions
        [Serializable]
        public class SQLiteInsertException : Exception
        {
            public SQLiteInsertException()
            {
            }

            public SQLiteInsertException(string msg)
                : base(msg)
            {
            }

            protected SQLiteInsertException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public SQLiteInsertException(string msg, Exception innerException)
                : base(msg, innerException)
            {
            }
        }

        [Serializable]
        public class SQLiteMigrationException : Exception
        {
            public SQLiteMigrationException()
            {
            }

            public SQLiteMigrationException(string msg)
                : base(msg)
            {
            }

            protected SQLiteMigrationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public SQLiteMigrationException(string msg, Exception innerException)
                : base(msg, innerException)
            {
            }
        }
        #endregion

        #region Transaction Helper
        public static void InsideTransaction(this SQLiteConnection connection, Action<SQLiteTransaction> del) =>
            InsideTransaction(connection, IsolationLevel.Serializable, del);

        public static void InsideTransaction(this SQLiteConnection connection, IsolationLevel level, Action<SQLiteTransaction> del)
        {
            SQLiteTransaction transaction = connection.BeginTransaction(level);
            try
            {
                del(transaction);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            transaction.Commit();
        }

        public static T InsideTransaction<T>(this SQLiteConnection connection, Func<SQLiteTransaction, T> del) =>
            InsideTransaction(connection, IsolationLevel.Serializable, del);

        public static T InsideTransaction<T>(this SQLiteConnection connection, IsolationLevel level, Func<SQLiteTransaction, T> del)
        {
            T result;
            SQLiteTransaction transaction = connection.BeginTransaction(level);
            try
            {
                result = del(transaction);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            transaction.Commit();
            return result;
        }
        #endregion

        #region Push parameters
        public static void AddParameter(this SQLiteParameterCollection mysqlparam, string key, object value)
        {
            var t = value?.GetType();
            if (t == typeof(Vector3))
            {
                var v = (Vector3)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
                mysqlparam.AddWithValue(key + "Z", v.Z);
            }
            else if (t == typeof(GridVector))
            {
                var v = (GridVector)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
            }
            else if (t == typeof(Quaternion))
            {
                var v = (Quaternion)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
                mysqlparam.AddWithValue(key + "Z", v.Z);
                mysqlparam.AddWithValue(key + "W", v.W);
            }
            else if (t == typeof(Color))
            {
                var v = (Color)value;
                mysqlparam.AddWithValue(key + "Red", v.R);
                mysqlparam.AddWithValue(key + "Green", v.G);
                mysqlparam.AddWithValue(key + "Blue", v.B);
            }
            else if (t == typeof(ColorAlpha))
            {
                var v = (ColorAlpha)value;
                mysqlparam.AddWithValue(key + "Red", v.R);
                mysqlparam.AddWithValue(key + "Green", v.G);
                mysqlparam.AddWithValue(key + "Blue", v.B);
                mysqlparam.AddWithValue(key + "Alpha", v.A);
            }
            else if (t == typeof(EnvironmentController.WLVector2))
            {
                var vec = (EnvironmentController.WLVector2)value;
                mysqlparam.AddWithValue(key + "X", vec.X);
                mysqlparam.AddWithValue(key + "Y", vec.Y);
            }
            else if (t == typeof(EnvironmentController.WLVector4))
            {
                var vec = (EnvironmentController.WLVector4)value;
                mysqlparam.AddWithValue(key + "Red", vec.X);
                mysqlparam.AddWithValue(key + "Green", vec.Y);
                mysqlparam.AddWithValue(key + "Blue", vec.Z);
                mysqlparam.AddWithValue(key + "Value", vec.W);
            }
            else if (t == typeof(bool))
            {
                mysqlparam.AddWithValue(key, (bool)value ? 1 : 0);
            }
            else if (t == typeof(UUID) || t == typeof(UGUI) || t == typeof(UGUIWithName) || t == typeof(UGI) || t == typeof(Uri) || t == typeof(UEI))
            {
                mysqlparam.AddWithValue(key, value.ToString());
            }
            else if(t == typeof(ParcelID))
            {
                UUID id = new UUID(((ParcelID)value).GetBytes(), 0);
                mysqlparam.AddWithValue(key, id.ToString());
            }
            else if (t == typeof(AnArray))
            {
                using (var stream = new MemoryStream())
                {
                    LlsdBinary.Serialize((AnArray)value, stream);
                    mysqlparam.AddWithValue(key, stream.ToArray());
                }
            }
            else if (t == typeof(Date))
            {
                mysqlparam.AddWithValue(key, ((Date)value).AsULong);
            }
            else if (t.IsEnum)
            {
                mysqlparam.AddWithValue(key, Convert.ChangeType(value, t.GetEnumUnderlyingType()));
            }
            else
            {
                mysqlparam.AddWithValue(key, value);
            }
        }

        private static void AddParameters(this SQLiteParameterCollection sqliteparams, Dictionary<string, object> vals)
        {
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    AddParameter(sqliteparams, "@v_" + kvp.Key, kvp.Value);
                }
            }
        }
        #endregion

        #region Common REPLACE INTO/INSERT INTO helper
        public static void AnyInto(this SQLiteConnection connection, string cmd, string tablename, Dictionary<string, object> vals, SQLiteTransaction transaction = null)
        {
            var sb = new SQLiteCommandBuilder();
            var q = new List<string>();
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;

                var t = value?.GetType();
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                }
                else if (t == typeof(GridVector) || t == typeof(EnvironmentController.WLVector2))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                }
                else if (t == typeof(Quaternion))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                    q.Add(key + "W");
                }
                else if (t == typeof(Color))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Value");
                }
                else if (t == typeof(ColorAlpha))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Alpha");
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    q.Add(key);
                }
            }

            var q1 = new StringBuilder();
            var q2 = new StringBuilder();
            q1.Append(cmd);
            q1.Append(" INTO ");
            q1.Append(tablename);
            q1.Append(" (");
            q2.Append(") VALUES (");
            bool first = true;
            foreach (string p in q)
            {
                if (!first)
                {
                    q1.Append(",");
                    q2.Append(",");
                }
                first = false;
                q1.Append(sb.QuoteIdentifier(p));
                q2.Append("@v_");
                q2.Append(p);
            }
            q1.Append(q2);
            q1.Append(")");
            using (var command = new SQLiteCommand(q1.ToString(), connection)
            {
                Transaction = transaction
            })
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new SQLiteInsertException();
                }
            }
        }
        #endregion

        #region REPLACE INSERT INTO helper
        public static void ReplaceInto(this SQLiteConnection connection, string tablename, Dictionary<string, object> vals, SQLiteTransaction transaction = null)
        {
            connection.AnyInto("REPLACE", tablename, vals, transaction);
        }
        #endregion

        #region INSERT INTO helper
        public static void InsertInto(this SQLiteConnection connection, string tablename, Dictionary<string, object> vals, SQLiteTransaction transaction = null)
        {
            connection.AnyInto("INSERT", tablename, vals, transaction);
        }
        #endregion

        #region UPDATE SET helper
        private static List<string> UpdateSetFromVals(SQLiteCommandBuilder sb, Dictionary<string, object> vals)
        {
            var updates = new List<string>();

            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;
                var t = value?.GetType();
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {
                    updates.Add(sb.QuoteIdentifier(key + "X") + " = @v_" + key + "X");
                    updates.Add(sb.QuoteIdentifier(key + "Y") + " = @v_" + key + "Y");
                    updates.Add(sb.QuoteIdentifier(key + "Z") + " = @v_" + key + "Z");
                }
                else if (t == typeof(GridVector) || t == typeof(EnvironmentController.WLVector2))
                {
                    updates.Add(sb.QuoteIdentifier(key + "X") + " = @v_" + key + "X");
                    updates.Add(sb.QuoteIdentifier(key + "Y") + " = @v_" + key + "Y");
                }
                else if (t == typeof(Quaternion))
                {
                    updates.Add(sb.QuoteIdentifier(key + "X") + " = @v_" + key + "X");
                    updates.Add(sb.QuoteIdentifier(key + "Y") + " = @v_" + key + "Y");
                    updates.Add(sb.QuoteIdentifier(key + "Z") + " = @v_" + key + "Z");
                    updates.Add(sb.QuoteIdentifier(key + "W") + " = @v_" + key + "W");
                }
                else if (t == typeof(Color))
                {
                    updates.Add(sb.QuoteIdentifier(key + "Red") + " = @v_" + key + "Red");
                    updates.Add(sb.QuoteIdentifier(key + "Green") + " = @v_" + key + "Green");
                    updates.Add(sb.QuoteIdentifier(key + "Blue") + " = @v_" + key + "Blue");
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    updates.Add(sb.QuoteIdentifier(key + "Red") + " = @v_" + key + "Red");
                    updates.Add(sb.QuoteIdentifier(key + "Green") + " = @v_" + key + "Green");
                    updates.Add(sb.QuoteIdentifier(key + "Blue") + " = @v_" + key + "Blue");
                    updates.Add(sb.QuoteIdentifier(key + "Value") + " = @v_" + key + "Value");
                }
                else if (t == typeof(ColorAlpha))
                {
                    updates.Add(sb.QuoteIdentifier(key + "Red") + " = @v_" + key + "Red");
                    updates.Add(sb.QuoteIdentifier(key + "Green") + " = @v_" + key + "Green");
                    updates.Add(sb.QuoteIdentifier(key + "Blue") + " = @v_" + key + "Blue");
                    updates.Add(sb.QuoteIdentifier(key + "Alpha") + " = @v_" + key + "Alpha");
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    updates.Add(sb.QuoteIdentifier(key) + " = @v_" + key);
                }
            }
            return updates;
        }

        public static void UpdateSet(this SQLiteConnection connection, string tablename, Dictionary<string, object> vals, string where, SQLiteTransaction transaction = null)
        {
            var sb = new SQLiteCommandBuilder();
            string q1 = "UPDATE " + tablename + " SET ";

            q1 += string.Join(",", UpdateSetFromVals(sb, vals));

            using (var command = new SQLiteCommand(q1 + " WHERE " + where, connection)
            {
                Transaction = transaction
            })
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new SQLiteInsertException();
                }
            }
        }

        public static void UpdateSet(this SQLiteConnection connection, string tablename, Dictionary<string, object> vals, Dictionary<string, object> where, SQLiteTransaction transaction = null)
        {
            SQLiteCommandBuilder sb = new SQLiteCommandBuilder();
            string q1 = "UPDATE " + tablename + " SET ";

            q1 += string.Join(",", UpdateSetFromVals(sb, vals));

            var wherestr = new StringBuilder();
            foreach (KeyValuePair<string, object> w in where)
            {
                if (wherestr.Length != 0)
                {
                    wherestr.Append(" AND ");
                }
                wherestr.AppendFormat("{0} = @w_{1}", sb.QuoteIdentifier(w.Key), w.Key);
            }

            using (var command = new SQLiteCommand(q1 + " WHERE " + wherestr, connection)
            {
                Transaction = transaction
            })
            {
                AddParameters(command.Parameters, vals);
                foreach (KeyValuePair<string, object> w in where)
                {
                    command.Parameters.AddParameter("@w_" + w.Key, w.Value);
                }
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new SQLiteInsertException();
                }
            }
        }
        #endregion

        #region Data parsers
        public static EnvironmentController.WLVector4 GetWLVector4(this SQLiteDataReader dbReader, string prefix) =>
            new EnvironmentController.WLVector4(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Value"]);

        public static T GetEnum<T>(this SQLiteDataReader dbreader, string prefix)
        {
            var enumType = typeof(T).GetEnumUnderlyingType();
            long v = (long)dbreader[prefix];
            return (T)Convert.ChangeType(v, enumType);
        }

        public static ParcelID GetParcelID(this SQLiteDataReader dbReader, string prefix)
        {
            UUID id = dbReader.GetUUID(prefix);
            return new ParcelID(id.GetBytes(), 0);
        }

        public static UUID GetUUID(this SQLiteDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            var t = v?.GetType();
            if (t == typeof(Guid))
            {
                return new UUID((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UUID((string)v);
            }

            throw new InvalidCastException("GetUUID could not convert value for " + prefix);
        }

        public static UGUI GetUGUI(this SQLiteDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            var t = v?.GetType();
            if (t == typeof(Guid))
            {
                return new UGUI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UGUI((string)v);
            }

            throw new InvalidCastException("GetUGUI could not convert value for " + prefix);
        }

        public static UGUIWithName GetUGUIWithName(this SQLiteDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            var t = v?.GetType();
            if (t == typeof(Guid))
            {
                return new UGUIWithName((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UGUIWithName((string)v);
            }

            throw new InvalidCastException("GetUGUIWithName could not convert value for " + prefix);
        }

        public static UEI GetUEI(this SQLiteDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            var t = v?.GetType();
            if (t == typeof(Guid))
            {
                return new UEI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UEI((string)v);
            }

            throw new InvalidCastException("GetUEI could not convert value for " + prefix);
        }

        public static UGI GetUGI(this SQLiteDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            var t = v?.GetType();
            if (t == typeof(Guid))
            {
                return new UGI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UGI((string)v);
            }

            throw new InvalidCastException("GetUGI could not convert value for " + prefix);
        }

        public static Date GetDate(this SQLiteDataReader dbReader, string prefix)
        {
            return Date.UnixTimeToDateTime((ulong)(long)dbReader[prefix]);
        }

        public static EnvironmentController.WLVector2 GetWLVector2(this SQLiteDataReader dbReader, string prefix) =>
            new EnvironmentController.WLVector2(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"]);

        public static Vector3 GetVector3(this SQLiteDataReader dbReader, string prefix) =>
            new Vector3(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"]);

        public static Quaternion GetQuaternion(this SQLiteDataReader dbReader, string prefix) =>
            new Quaternion(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"],
                (double)dbReader[prefix + "W"]);

        public static Color GetColor(this SQLiteDataReader dbReader, string prefix) =>
            new Color(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"]);

        public static ColorAlpha GetColorAlpha(this SQLiteDataReader dbReader, string prefix) =>
            new ColorAlpha(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Alpha"]);

        public static bool GetBool(this SQLiteDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            var t = o?.GetType();
            if (t == typeof(long))
            {
                return (long)o != 0;
            }
            else
            {
                throw new InvalidCastException("GetBoolean could not convert value for " + prefix + ": got type " + o.GetType().FullName);
            }
        }

        public static byte[] GetBytes(this SQLiteDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            var t = o?.GetType();
            if (t == typeof(DBNull))
            {
                return new byte[0];
            }
            return (byte[])o;
        }

        public static byte[] GetBytesOrNull(this SQLiteDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            var t = o?.GetType();
            if (t == typeof(DBNull))
            {
                return null;
            }
            return (byte[])o;
        }

        public static Uri GetUri(this SQLiteDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            var t = o?.GetType();
            if (t == typeof(DBNull))
            {
                return null;
            }
            var s = (string)o;
            if (s.Length == 0)
            {
                return null;
            }
            return new Uri(s);
        }

        public static GridVector GetGridVector(this SQLiteDataReader dbReader, string prefix) =>
            new GridVector((uint)(long)dbReader[prefix + "X"], (uint)(long)dbReader[prefix + "Y"]);
        #endregion

        #region Migrations helper
        public static uint GetTableRevision(this SQLiteConnection connection, string name)
        {
            using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS migrations (tablename text, revision integer, primary key(tablename))", connection))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand("SELECT revision FROM migrations WHERE tablename=@name", connection))
            {
                cmd.Parameters.AddWithValue("@name", name);
                using (SQLiteDataReader dbReader = cmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        return (uint)(long)dbReader["revision"];
                    }
                }
            }
            return 0;
        }
        #endregion
    }
}
