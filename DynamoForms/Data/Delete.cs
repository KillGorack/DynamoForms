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

        var sql = $"DELETE FROM [{tableName}] WHERE [ID] = @Id";

        using var conn = _dbHelper.CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}