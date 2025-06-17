using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using DynamoForms.Models;
using DynamoForms.Data;

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

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<List<TableColumnMeta>> GetTableMetaAsync(string tableName)
        {
            var sql = @"
                SELECT 
                    c.COLUMN_NAME as ColumnName,
                    c.DATA_TYPE as DataType,
                    CASE c.IS_NULLABLE WHEN 'YES' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END as IsNullable,
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
            var meta = await conn.QueryAsync<TableColumnMeta>(sql, new { TableName = tableName });
            return meta.ToList();
        }

        public async Task<List<Dictionary<string, object>>> GetAllRecordsAsync(string tableName)
        {
            var sql = $"SELECT * FROM [{tableName}]";
            using var conn = CreateConnection();
            var result = await conn.QueryAsync(sql);
            var list = new List<Dictionary<string, object>>();
            foreach (var row in result)
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in row)
                {
                    dict[prop.Key] = prop.Value;
                }
                list.Add(dict);
            }
            return list;
        }

        public async Task<List<string>> GetAllTableNamesAsync()
        {
            using var conn = CreateConnection();
            var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var result = await conn.QueryAsync<string>(sql);
            return result.ToList();
        }

        public async Task<int> GetRecordCountAsync(string tableName)
        {
            using var conn = CreateConnection();
            return await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [{tableName}]");
        }

        public async Task<List<Dictionary<string, object>>> GetPagedRecordsAsync(string tableName, int pageNumber, int pageSize)
        {
            using var conn = CreateConnection();
            var offset = (pageNumber - 1) * pageSize;
            var sql = $"SELECT * FROM [{tableName}] ORDER BY (SELECT NULL) OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var result = await conn.QueryAsync(sql, new { Offset = offset, PageSize = pageSize });
            return result.Select(row => (IDictionary<string, object>)row)
                         .Select(dict => dict.ToDictionary(kv => kv.Key, kv => kv.Value)).ToList();
        }
    }
}