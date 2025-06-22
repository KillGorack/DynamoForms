using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace DynamoForms.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);


        public async Task<object> FetchDataAsync(string sql, int dim = 2, object parameters = null)
        {
            using var conn = CreateConnection();
            try
            {
                switch (dim)
                {
                    case 0:
                        // Single value (scalar)
                        return await conn.ExecuteScalarAsync<object>(sql, parameters);

                    case 1:
                        // One-dimensional array (single row as dictionary)
                        var row = await conn.QueryFirstOrDefaultAsync(sql, parameters);
                        if (row == null) return null;
                        return ((IDictionary<string, object>)row).ToDictionary(kv => kv.Key, kv => kv.Value);

                    case 2:
                    default:
                        // Two-dimensional array (list of dictionaries)
                        var result = await conn.QueryAsync(sql, parameters);
                        var list = new List<Dictionary<string, object>>();
                        foreach (var r in result)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var prop in r)
                            {
                                dict[prop.Key] = prop.Value;
                            }
                            list.Add(dict);
                        }
                        return list;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ExecuteAsync(string sql, object parameters = null)
        {
            using var conn = CreateConnection();
            try
            {
                await conn.ExecuteAsync(sql, parameters);
                return true; // Success
            }
            catch (Exception ex)
            {
                // Add more context to the exception
                var errorMessage = $"Database error while executing SQL: {sql}. Parameters: {parameters}. Error: {ex.Message}";
                Debug.WriteLine(errorMessage);
                throw new Exception(errorMessage, ex); // Rethrow with additional context
            }
        }

        public async Task<List<Dictionary<string, object>>> GetTableMetaAsync(string tableName)
        {
            var sql = @"
            SELECT 
                c.COLUMN_NAME as ColumnName,
                c.DATA_TYPE as DataType,
                CASE c.IS_NULLABLE WHEN 'YES' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END as Required,
                c.CHARACTER_MAXIMUM_LENGTH as MaxLength,
                c.COLUMN_DEFAULT as DefaultValue,
                c.NUMERIC_PRECISION as NumericPrecision,
                c.NUMERIC_SCALE as NumericScale,
                COLUMNPROPERTY(object_id(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IsIdentity,
                CASE WHEN k.COLUMN_NAME IS NOT NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END as IsPrimaryKey
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                        AND TABLE_NAME = @TableName
            ) k ON c.COLUMN_NAME = k.COLUMN_NAME
            WHERE c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION;
            ";

            using var conn = CreateConnection();
            var result = await conn.QueryAsync(sql, new { TableName = tableName });

            // Convert the result to a list of dictionaries
            return result.Select(row => (IDictionary<string, object>)row)
                         .Select(dict => dict.ToDictionary(kv => kv.Key, kv => kv.Value))
                         .ToList();
        }

        public async Task<int> GetFilteredRecordCountAsync(
            string tableName, Dictionary<string, string> filters, List<UnifiedField> columns)
        {
            using var conn = CreateConnection();
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            foreach (var filter in filters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnMeta = columns.FirstOrDefault(c => c.Label == filter.Key);
                    if (columnMeta != null && columnMeta.Type == "bit")
                    {
                        if (filter.Value == "true" || filter.Value == "false")
                        {
                            whereClauses.Add($"[{filter.Key}] = @filter_{filter.Key}");
                            parameters.Add($"filter_{filter.Key}", filter.Value == "true" ? 1 : 0);
                        }
                    }
                    else
                    {
                        whereClauses.Add($"[{filter.Key}] LIKE @filter_{filter.Key}");
                        parameters.Add($"filter_{filter.Key}", $"%{filter.Value}%");
                    }
                }
            }
            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            var sql = $"SELECT COUNT(*) FROM [{tableName}] {whereSql}";
            return await conn.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<List<string>> GetAllTableNamesAsync()
        {
            using var conn = CreateConnection();
            var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }

        public async Task<List<Dictionary<string, object>>> GetPagedRecordsAsync(
            string tableName, int pageNumber, int pageSize, string sortColumn, bool sortDescending, Dictionary<string, string> filters, List<UnifiedField> columns)
        {
            using var conn = CreateConnection();
            var offset = (pageNumber - 1) * pageSize;
            var orderBy = !string.IsNullOrEmpty(sortColumn)
                ? $"[{sortColumn}] {(sortDescending ? "DESC" : "ASC")}"
                : "(SELECT NULL)";

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();
            foreach (var filter in filters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnMeta = columns.FirstOrDefault(c => c.Label == filter.Key);
                    if (columnMeta != null && columnMeta.Type == "bit")
                    {
                        // Only filter if value is "true" or "false"
                        if (filter.Value == "true" || filter.Value == "false")
                        {
                            whereClauses.Add($"[{filter.Key}] = @filter_{filter.Key}");
                            parameters.Add($"filter_{filter.Key}", filter.Value == "true" ? 1 : 0);
                        }
                    }
                    else
                    {
                        whereClauses.Add($"[{filter.Key}] LIKE @filter_{filter.Key}");
                        parameters.Add($"filter_{filter.Key}", $"%{filter.Value}%");
                    }
                }
            }
            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var sql = $"SELECT * FROM [{tableName}] {whereSql} ORDER BY {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var result = await conn.QueryAsync(sql, parameters);
            return result.Select(row => (IDictionary<string, object>)row)
                         .Select(dict => dict.ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
        }
    }
}