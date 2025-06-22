using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

public class DeleteService
{
    private readonly DatabaseHelper _dbHelper;

    public DeleteService(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task DeleteRecordAsync(string tableName, int id, AppRegistry registry)
    {
        var columns = registry.Columns;
        var pk = columns?.FirstOrDefault(c => c.IsPrimaryKey)?.Label;

        if (string.IsNullOrEmpty(pk))
        {
            throw new Exception("Primary key not found for the table.");
        }

        var sql = $"DELETE FROM [{tableName}] WHERE [{pk}] = @Id";

        using var conn = _dbHelper.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}