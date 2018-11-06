using Log;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Db
{
    /// <summary>
    /// Executes quieries (by names, not by sql). Queries stored in 'bin\debug\sql' folder
    /// Fill objects with data for select commands (can load classes with IRow interface implementation).
    /// Logs all query execution, errors. Logs stored in 'bin\debug\log' folder
    /// </summary>
    class DbExecuter
    {
        #region public methods

        /// <summary>
        /// Connection string for each operation
        /// </summary>
        public static string ConnectionString { get; set; }

        /// <summary>
        /// Set true if log only errors, otherwise logs all actions.
        /// This option is also available for some methos (to silent execute only specific queries)
        /// </summary>
        public static bool SilentMode { get; set; }

        /// <summary>
        /// Duration in seconds, after which it is notified that the request is executed for a long time
        /// </summary>
        public static double LogRequestTime = 2;

        /// <summary>
        /// Load all sql-queries from sql folder.
        /// Call this method before all others methods.
        /// </summary>
        public static void Init()
        {
            htFiles.Clear();
            htQueries.Clear();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Sql");
            var files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                var dt = File.GetLastWriteTime(file);
                htFiles.Add(file, dt);
            }

            foreach (string file in files)
            {
                ReadFile(file);
            }
        }

        /// <summary>
        /// Search for changes in sql-queries and (if was changes) reloads all sql-queries.
        /// This method can be called periodicaly.
        /// </summary>
        public static void ReloadSql()
        {
            bool foundChanges = false;
            foreach (string file in htFiles.Keys)
            {
                var dtOld = htFiles[file];
                var dt = File.GetLastWriteTime(file);
                if (dt != dtOld)
                {
                    foundChanges = true;
                    break;
                }
            }
            if (foundChanges)
                Init();
        }

        /// <summary>
        /// Manual opening of connection (for transaction)
        /// </summary>
        /// <returns>open connection</returns>
        public static OracleConnection OpenConnection()
        {
            var con = new OracleConnection(ConnectionString);
            con.Open();
            return con;
        }


        /// <summary>
        /// Executes DML block (insert, update, anonymouse block)
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>number of rows affected</returns>
        public static int Execute(string query, params DbParam[] dbParams)
        {
            return Execute(null, query, false, true, dbParams);
        }

        /// <summary>
        /// Executes DML block (insert, update, anonymouse block)
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>number of rows affected</returns>
        public static int Execute(OracleTransaction trans, string query, params DbParam[] dbParams)
        {
            return Execute(trans, query, false, true, dbParams);
        }

        /// <summary>
        /// Executes DML block (insert, update, anonymouse block)
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="showParams">if false, do not output parameters value to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>number of rows affected</returns>
        public static int Execute(string query, bool silentMode, bool showParams, params DbParam[] dbParams)
        {
            return Execute(null, query, silentMode, showParams, dbParams);
        }

        /// <summary>
        /// Executes DML block (insert, update, anonymouse block)
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="showParams">if false, do not output parameters value to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>number of rows affected</returns>
        public static int Execute(OracleTransaction trans, string query, bool silentMode, bool showParams, params DbParam[] dbParams)
        {
            int res = 0;

            OracleConnection con = null;
            if (trans != null)
            {
                con = trans.Connection;
            }
            else
            {
                con = new OracleConnection(ConnectionString);
                con.Open();
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var cmd = con.CreateCommand();
                cmd.CommandText = GetSql(query);
                cmd.Connection = con;
                cmd.Transaction = trans;
                SetParamsInCmd(cmd, dbParams);

                if (!SilentMode && !silentMode)
                    log.Debug(string.Format("Execute query {2}'{0}'{1}", query, showParams ? ParamText(cmd) : "", trans != null ? "(transaction) " : ""));
                try
                {
                    res = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error executing query '{0}': {1}\n{2}", HideDynamic(query), ex.Message, CmdText(cmd)));
                }
                GetParamsFromCmd(cmd, dbParams);
                sw.Stop();
                if (!SilentMode && sw.Elapsed.TotalSeconds > LogRequestTime)
                    log.Debug(string.Format("!Long request '{0}'. Rows affected: {1}. Executed in: {2}{3}", query, res, sw.Elapsed.ToString(), showParams ? ParamText(cmd) : ""));
            }
            finally
            {
                if (trans == null)
                    con.Close();
            }

            return res;
        }

        /// <summary>
        /// Select single value
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Selected value</returns>
        public static object SelectScalar(string query, params DbParam[] dbParams)
        {
            return SelectScalar(null, query, false, true, dbParams);
        }

        /// <summary>
        /// Select single value
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Selected value</returns>
        public static object SelectScalar(OracleTransaction trans, string query, params DbParam[] dbParams)
        {
            return SelectScalar(trans, query, false, true, dbParams);
        }

        /// <summary>
        /// Select single value
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="showParams">if false, do not output parameters value to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Selected value</returns>
        public static object SelectScalar(OracleTransaction trans, string query, bool silentMode, bool showParams, params DbParam[] dbParams)
        {
            object res = null;

            OracleConnection con = null;
            if (trans != null)
            {
                con = trans.Connection;
            }
            else
            {
                con = new OracleConnection(ConnectionString);
                con.Open();
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var cmd = con.CreateCommand();
                cmd.CommandText = GetSql(query);
                cmd.Connection = con;
                cmd.Transaction = trans;
                SetParamsInCmd(cmd, dbParams);

                if (!SilentMode && !silentMode)
                    log.Debug(string.Format("Execute query {2}'{0}'{1}", query, showParams ? ParamText(cmd) : "", trans != null ? "(transaction) " : ""));
                try
                {
                    res = cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error executing query '{0}': {1}\n{2}", HideDynamic(query), ex.Message, CmdText(cmd)));
                }
                GetParamsFromCmd(cmd, dbParams);
                sw.Stop();
                if (!SilentMode && sw.Elapsed.TotalSeconds > LogRequestTime)
                    log.Debug(string.Format("!Long request '{0}' for scalar. Read value: '{1}'. Executed in: {2}{3}", query, res, sw.Elapsed.ToString(), showParams ? ParamText(cmd) : ""));
            }
            finally
            {
                if (trans == null)
                    con.Close();
            }
            return res;
        }

        /// <summary>
        /// Select rows into list of entities (must support IRow interface)
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>List of entities</returns>
        public static List<T> Select<T>(string query, params DbParam[] dbParams) where T : IRow, new()
        {
            return Select<T>(null, query, false, true, dbParams);
        }

        /// <summary>
        /// Select rows into list of entities (must support IRow interface)
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>List of entities</returns>
        public static List<T> Select<T>(OracleTransaction trans, string query, params DbParam[] dbParams) where T : IRow, new()
        {
            return Select<T>(trans, query, false, true, dbParams);
        }

        /// <summary>
        /// Select rows into list of entities (must support IRow interface)
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>List of entities</returns>
        public static List<T> Select<T>(string query, bool silentMode, params DbParam[] dbParams) where T : IRow, new()
        {
            return Select<T>(null, query, silentMode, true, dbParams);
        }

        /// <summary>
        /// Select rows into list of entities (must support IRow interface)
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="showParams">if false, do not output parameters value to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>List of entities</returns>
        public static List<T> Select<T>(OracleTransaction trans, string query, bool silentMode, bool showParams, params DbParam[] dbParams) where T : IRow, new()
        {
            var res = new List<T>();
 
            OracleConnection con = null;
            if (trans != null)
            {
                con = trans.Connection;
            }
            else
            {
                con = new OracleConnection(ConnectionString);
                con.Open();
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var cmd = con.CreateCommand();
                cmd.CommandText = GetSql(query);
                cmd.Connection = con;
                cmd.Transaction = trans;
                SetParamsInCmd(cmd, dbParams);

                if (!SilentMode && !silentMode)
                    log.Debug(string.Format("Execute query {2}'{0}'{1}", query, showParams ? ParamText(cmd) : "", trans != null ? "(transaction) " : ""));
                int cnt = 0;
                OracleDataReader r = null;
                try
                {
                    r = cmd.ExecuteReader();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error executing query '{0}': {1}\n{2}", HideDynamic(query), ex.Message, CmdText(cmd)));
                }
                using (r)
                {
                    var columns = new Dictionary<string, int>();
                    for (int i = 0; i < r.FieldCount; i++)
                    {
                        string name = r.GetName(i);
                        if (columns.ContainsKey(name))
                            throw new Exception(string.Format("Duplicate field {0} in query", name));
                        columns.Add(name, i);
                    }

                    while (r.Read())
                    {
                        cnt++;
                        T t = new T();
                        t.Init(r, columns);
                        res.Add(t);
                    }
                }
                sw.Stop();
                if (!SilentMode && sw.Elapsed.TotalSeconds > LogRequestTime)
                    log.Debug(string.Format("!Long request '{0}' for reading {1}. Read {2} rows. Executed in: {3}{4}", query, typeof(T), cnt, sw.Elapsed.ToString(), showParams ? ParamText(cmd) : ""));
            }
            finally
            {
                if (trans == null)
                    con.Close();
            }

            return res;
        }

        /// <summary>
        /// Select only first row from query
        /// </summary>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Entity</returns>
        public static T SelectRow<T>(string query, params DbParam[] dbParams) where T : IRow, new()
        {
            return SelectRow<T>(null, query, false, true, dbParams);
        }

        /// <summary>
        /// Select only first row from query
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Entity</returns>
        public static T SelectRow<T>(OracleTransaction trans, string query, params DbParam[] dbParams) where T : IRow, new()
        {
            return SelectRow<T>(trans, query, false, true, dbParams);
        }

        /// <summary>
        /// Select only first row from query
        /// </summary>
        /// <param name="trans">transaction, if specified, new connection does not open</param>
        /// <param name="query">name of the query (not sql text)</param>
        /// <param name="silentMode">if true, do not output debug information to log file</param>
        /// <param name="showParams">if false, do not output parameters value to log file</param>
        /// <param name="dbParams">parameters for query</param>
        /// <returns>Entity</returns>
        public static T SelectRow<T>(OracleTransaction trans, string query, bool silentMode, bool showParams, params DbParam[] dbParams) where T : IRow, new()
        {
            var list = Select<T>(trans, query, silentMode, showParams, dbParams);
            return list.Count > 0 ? list[0] : default(T);
        }

        #endregion

        #region private methods

        static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static Dictionary<string, DateTime> htFiles = new Dictionary<string, DateTime>();
        static Dictionary<string, string> htQueries = new Dictionary<string, string>();

        private static void ReadFile(string file)
        {
            var doc = XDocument.Load(file);
            var queries = doc.Root.Elements("sql");
            foreach (XElement q in queries)
            {
                string name = q.Attribute("name").Value;
                if (htQueries.ContainsKey(name))
                    throw new Exception(string.Format("Duplicate key: '{0}' for query", name));
                htQueries.Add(name, q.Value);
            }
        }

        private static string GetSql(string query)
        {
            if (query.StartsWith("<dynamic>"))
                return query.Substring("<dynamic>".Length).Replace("\r", "");

            if (!htQueries.ContainsKey(query))
                throw new Exception(string.Format("Query not found: '{0}'", query));

            return htQueries[query].Replace("\r", "");
        }

        public static string HideDynamic(string query)
        {
            if (query.StartsWith("<dynamic>"))
                return "<dynamic>";

            return query;
        }

        private static string CmdText(OracleCommand cmd)
        {
            string str = cmd.CommandText;

            foreach (OracleParameter p in cmd.Parameters)
            {
                str += string.Format("\n{0}: {1}", p.ParameterName, p.Value == null || p.Value == DBNull.Value ? "null" : "'" +
                    (p.Value.ToString().Length > 100 ? p.Value.ToString().Substring(0, 100) + "..." : p.Value.ToString()) + "'");
            }
            return str;
        }

        private static string ParamText(OracleCommand cmd)
        {
            string str = "";
            foreach (OracleParameter p in cmd.Parameters)
            {
                if (str.Length > 0)
                    str += ", ";

                var pVal = "";
                if (p.Direction == ParameterDirection.Output)
                    pVal = "[out]";
                else if (p.Value == null || p.Value == DBNull.Value)
                    pVal = "null";
                else if (p.Value.ToString().Length > 50)
                    pVal = "...";
                else
                    pVal = "'" + p.Value + "'";

                str += string.Format("{0}={1}", p.ParameterName, pVal);
            }
            return str.Length > 0 ? ": " + str : "";

        }

        private static void GetParamsFromCmd(OracleCommand cmd, params DbParam[] dbParams)
        {
            foreach (var p in dbParams)
            {
                var op = new OracleParameter(p.Name, p.Value);
                if (p.Output)
                {
                    bool found = false;
                    foreach (OracleParameter p1 in cmd.Parameters)
                    {
                        if (p1.ParameterName == p.Name)
                        {
                            p.Value = p1.Value;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        throw new Exception(string.Format("Parameter not found: {0}", p.Name));
                }
            }
        }

        private static void SetParamsInCmd(OracleCommand cmd, params DbParam[] dbParams)
        {
            cmd.BindByName = true;
            foreach (var p in dbParams)
            {
                OracleParameter op = null;
                try
                {
                    op = new OracleParameter(p.Name, p.Value);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error (" + ex.Message + ") creating parameter: Name=" + p.Name + ", Value=" + p.Value);
                }
                if (p.Output)
                {
                    op.Size = 1000;
                    op.Direction = ParameterDirection.Output;
                }
                cmd.Parameters.Add(op);
            }
        }

        #endregion
    }
}
