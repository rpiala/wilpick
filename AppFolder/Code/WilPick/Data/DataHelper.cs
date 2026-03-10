
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Reflection;
using System.Text;
using Logger = WilPick.Common;

namespace WilPick.Data
{


    public sealed class DbSettings
    {
        public int CommandTimeoutSeconds { get; set; } = 30;
    }

    public sealed class SqlConnectionFactory
    {
        private readonly string _connStr;
        public SqlConnectionFactory(string connStr) => _connStr = connStr;
        public SqlConnection Create() => new SqlConnection(_connStr);
    }


    public sealed class DataHelper
    {
        private readonly SqlConnectionFactory _factory;
        private readonly int _dbTimeout;

        
        // Cache property maps for types to speed up reflection
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propMapCache = new();


        public DataHelper(SqlConnectionFactory factory, IOptions<DbSettings> options)
        {
            _factory = factory;
            _dbTimeout = options.Value.CommandTimeoutSeconds;
        }

        // -----------------------------
        //  CreateLoginTransactionLog
        // -----------------------------
        public async Task CreateLoginTransactionLogAsync(string userName, CancellationToken ct = default)
        {
            string transDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var sb = new StringBuilder();
            sb.Append("COLUMNSINSERT{:}wpUserLogTransaction(RequestDate,UserName,TransactionType,RequestDetails)")
              .Append("{|}VALUES{:}('")
              .Append(transDate).Append("','")
              .Append(userName).Append("','")
              .Append("Login").Append("','")
              .Append("Login Request by user").Append("')");

            string formatted = sb.ToString();

            await using var cn = _factory.Create();
            await cn.OpenAsync(ct);
            await using var trans = await cn.BeginTransactionAsync(ct);

            try
            {
                await using var cm = cn.CreateCommand();
                cm.Transaction = (SqlTransaction)trans;

                bool ok = await InsertUpdateTableDataAsync(formatted, cm, ct);
                if (!ok)
                {
                    await trans.RollbackAsync(ct);
                    return;
                }

                await trans.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "CreateLoginTransactionLogAsync", ex.Message);
                try { await trans.RollbackAsync(ct); } catch { /* ignored */ }
                throw;
            }
        }

        // Optional exact sync version (to match your original signature)
        public void CreateLoginTransactionLog(string userName)
            => CreateLoginTransactionLogAsync(userName).GetAwaiter().GetResult();

        // ---------------------------------
        //  InsertUpdateTableData (Async)
        // ---------------------------------
        public async Task<bool> InsertUpdateTableDataAsync(string rawQuery, SqlCommand cm, CancellationToken ct = default)
        {
            try
            {
                string sqlQuery = ParseQuery(rawQuery /*, pageNumber: null, pageSize: null */);

                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spGetTableDataQuery";
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("DataHelper", "InsertUpdateTableDataAsync", "Query: " + sqlQuery);

                using var dt = new DataTable();
                await using var reader = await cm.ExecuteReaderAsync(ct);
                dt.Load(reader);

                // Keep same behavior (consume rows if needed)
                foreach (DataRow _ in dt.Rows) { /* no-op */ }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("DataHelper", "InsertUpdateTableDataAsync", ex.Message);
                return false;
            }
        }

        // Optional exact sync version
        public bool InsertUpdateTableData(string rawQuery, SqlCommand cm)
            => InsertUpdateTableDataAsync(rawQuery, cm).GetAwaiter().GetResult();

        // ------------------------------------------------------
        //  ParseQuery — faithful to your token rules + paging
        //  Tokens supported:
        //    COLUMNS | TABLES | WHERE | GROUP | SORT
        //    COLUMNSINSERT | VALUES
        //    UPDATETABLE  | COLUMNSVALUESET
        //  Paging (optional): requires SORT/ORDER BY present
        // ------------------------------------------------------
        private static string ParseQuery(string rawQuery, int? pageNumber = null, int? pageSize = null)
        {
            var clauses = rawQuery.Split(new[] { "{|}" }, StringSplitOptions.None);
            var sql = new StringBuilder();
            bool hasOrderBy = false;

            foreach (var clause in clauses)
            {
                var parseClause = clause.Split(new[] { "{:}" }, StringSplitOptions.None);
                if (parseClause.Length > 1)
                {
                    var key = parseClause[0].Trim().ToUpperInvariant();
                    var value = parseClause[1].Trim();

                    switch (key)
                    {
                        case "COLUMNS":
                            sql.Append("SELECT ").Append(value).Append(' ');
                            break;
                        case "TABLES":
                            sql.Append("FROM ").Append(value).Append(' ');
                            break;
                        case "WHERE":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("WHERE ").Append(value).Append(' ');
                            break;
                        case "GROUP":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("GROUP BY ").Append(value).Append(' ');
                            break;
                        case "SORT":
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                sql.Append("ORDER BY ").Append(value).Append(' ');
                                hasOrderBy = true;
                            }
                            break;
                        case "COLUMNSINSERT":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("INSERT INTO ").Append(value).Append(' ');
                            break;
                        case "VALUES":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("VALUES ").Append(value).Append(' ');
                            break;
                        case "UPDATETABLE":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("UPDATE ").Append(value).Append(' ');
                            break;
                        case "COLUMNSVALUESET":
                            if (!string.IsNullOrWhiteSpace(value))
                                sql.Append("SET ").Append(value).Append(' ');
                            break;
                    }
                }
            }

            // Optional pagination: only valid when ORDER BY exists
            if (pageNumber.HasValue && pageSize.HasValue && hasOrderBy)
            {
                int page = Math.Max(1, pageNumber.Value);
                int size = Math.Max(1, pageSize.Value);
                int offset = (page - 1) * size;
                sql.Append($"OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY ");
            }

            // Ensure trailing semicolon for EXEC
            var finalSql = sql.ToString().TrimEnd();
            if (!finalSql.EndsWith(";")) finalSql += ";";
            return finalSql;
        }


        public async Task<DataTable?> GetTableDataAsync(
                string query,
                int? pageNumber = null,
                int? pageSize = null,
                CancellationToken ct = default)
        {
            try
            {
                var dt = new DataTable();

                await using var cn = _factory.Create();
                await cn.OpenAsync(ct);

                await using var cm = cn.CreateCommand();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spGetTableDataQuery";

                var sqlQuery = ParseQuery(query, pageNumber, pageSize);
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("Datahelper", "GetTableDataAsync", "Query: " + sqlQuery);

                // Execute and load into DataTable (replaces SqlDataAdapter.Fill)
                await using var reader = await cm.ExecuteReaderAsync(ct);
                dt.Load(reader);

                return dt;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("Datahelper", "GetTableDataAsync", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Optional sync wrapper to preserve a sync call-site.
        /// </summary>
        public DataTable? GetTableData(string query, int? pageNumber = null, int? pageSize = null)
            => GetTableDataAsync(query, pageNumber, pageSize).GetAwaiter().GetResult();


        /// <summary>
        /// .NET 8 async version of GetTableDataModel&lt;T&gt;.
        /// Builds dynamic SQL from your tokenized query and executes dbo.spGetTableDataQuery.
        /// Maps the result to a list of T using reflection (case-insensitive property matching).
        /// Returns null on error to mirror original behavior.
        /// </summary>
        public async Task<List<T>?> GetTableDataModelAsync<T>(
            string query,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken ct = default) where T : new()
        {
            try
            {
                await using var cn = _factory.Create();
                await cn.OpenAsync(ct);

                await using var cm = cn.CreateCommand();
                cm.CommandType = CommandType.StoredProcedure;
                cm.CommandTimeout = _dbTimeout;
                cm.CommandText = "dbo.spGetTableDataQuery";

                var sqlQuery = ParseQuery(query, pageNumber, pageSize);
                cm.Parameters.Add(new SqlParameter("@sqlQuery", SqlDbType.NVarChar) { Value = sqlQuery });

                Logger.Logger.Status("Datahelper", "GetTableDataModelAsync", "Query: " + sqlQuery);

                var list = new List<T>();

                await using var reader = await cm.ExecuteReaderAsync(ct);
                if (!reader.HasRows)
                    return list;

                // Build a column name -> ordinal map once
                var fieldCount = reader.FieldCount;
                var columnOrdinals = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < fieldCount; i++)
                {
                    columnOrdinals[reader.GetName(i)] = i;
                }

                // Get (or build) property map for T (case-insensitive)
                var propMap = GetPropertyMap<T>();

                while (await reader.ReadAsync(ct))
                {
                    var obj = new T();

                    // For each column, map to property if present
                    foreach (var kvp in columnOrdinals)
                    {
                        var colName = kvp.Key;
                        var ordinal = kvp.Value;

                        if (!propMap.TryGetValue(colName, out var prop) || prop == null || !prop.CanWrite)
                            continue;

                        if (await reader.IsDBNullAsync(ordinal, ct))
                        {
                            // Assign null to nullable reference types/nullable value types; skip for non-nullables
                            if (IsNullableProperty(prop))
                                prop.SetValue(obj, null);
                            continue;
                        }

                        var value = reader.GetValue(ordinal);
                        var converted = ConvertToPropertyType(value, prop.PropertyType);
                        prop.SetValue(obj, converted);
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch (Exception ex)
            {
                Logger.Logger.Error("Datahelper", "GetTableDataModelAsync", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Optional sync wrapper to preserve existing sync call-sites.
        /// </summary>
        public List<T>? GetTableDataModel<T>(string query, int? pageNumber = null, int? pageSize = null) where T : new()
            => GetTableDataModelAsync<T>(query, pageNumber, pageSize).GetAwaiter().GetResult();

        // ----------------- Helpers -----------------

        private static Dictionary<string, PropertyInfo> GetPropertyMap<T>()
        {
            var type = typeof(T);
            if (_propMapCache.TryGetValue(type, out var cached))
                return cached;

            // Case-insensitive property map (public instance, readable/writable)
            var map = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite)
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _propMapCache[type] = map;
            return map;
        }

        private static bool IsNullableProperty(PropertyInfo prop)
        {
            var t = prop.PropertyType;
            return !t.IsValueType || Nullable.GetUnderlyingType(t) != null;
        }

        private static object? ConvertToPropertyType(object value, Type propertyType)
        {
            if (value is null) return null;

            // Handle Nullable<T>
            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // If already assignable, short-circuit
            if (targetType.IsInstanceOfType(value))
                return value;

            // Enums
            if (targetType.IsEnum)
            {
                if (value is string s) return Enum.Parse(targetType, s, ignoreCase: true);
                return Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType)));
            }

            // Guid
            if (targetType == typeof(Guid))
            {
                if (value is Guid g) return g;
                return Guid.Parse(value.ToString()!);
            }

            // DateTime/DateOnly/TimeOnly conversions if needed
            if (targetType == typeof(DateTime))
                return Convert.ToDateTime(value);
            #if NET8_0_OR_GREATER
            if (targetType == typeof(DateOnly))
            {
                if (value is DateTime dt) return DateOnly.FromDateTime(dt);
                return DateOnly.FromDateTime(Convert.ToDateTime(value));
            }
            if (targetType == typeof(TimeOnly))
            {
                if (value is TimeSpan ts) return TimeOnly.FromTimeSpan(ts);
                if (value is DateTime dtt) return TimeOnly.FromDateTime(dtt);
            }
            #endif

            // Boolean (handle 0/1, "true"/"false")
            if (targetType == typeof(bool))
            {
                if (value is bool b) return b;
                if (value is byte by) return by != 0;
                if (value is short sh) return sh != 0;
                if (value is int i) return i != 0;
                var s = value.ToString()!.Trim();
                if (string.Equals(s, "1") || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "0") || string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
                return Convert.ChangeType(value, targetType);
            }

            // Fallback
            return Convert.ChangeType(value, targetType);
        }


    }

}
