using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamoForms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Dapper;
using DynamoForms.Data;
using System;

public class TableEditModel : PageModel
{
    private readonly DatabaseHelper _dbHelper;

    public TableEditModel(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<TableColumnMeta> Columns { get; set; }
    public string TableName { get; set; }
    public int EditId { get; set; }
    public string Message { get; set; }

    [BindProperty]
    public Dictionary<string, string> EditRecord { get; set; } = new();

    public async Task OnGetAsync(string table, int id)
    {
        TableName = table;
        EditId = id;
        Columns = await _dbHelper.GetTableMetaAsync(table);

        var pk = Columns.FirstOrDefault(c => c.IsPrimaryKey)?.ColumnName;
        if (pk != null)
        {
            using var conn = _dbHelper.CreateConnection();
            var sql = $"SELECT * FROM [{table}] WHERE [{pk}] = @Id";
            var record = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });
            if (record != null)
            {
                EditRecord = ((IDictionary<string, object>)record)
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
            }
        }
    }

    public async Task<IActionResult> OnPostAsync(string table, int id)
    {
        TableName = table;
        EditId = id;
        Columns = await _dbHelper.GetTableMetaAsync(table);

        // Set current datetime for any datetime column before saving
        foreach (var col in Columns)
        {
            if (col.DataType == "datetime")
            {
                EditRecord[col.ColumnName] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
        }

        var pk = Columns.FirstOrDefault(c => c.IsPrimaryKey)?.ColumnName;
        var updateCols = Columns.Where(c => !c.IsIdentity && !c.IsPrimaryKey).Select(c => c.ColumnName).ToList();
        var setClause = string.Join(", ", updateCols.Select(c => $"[{c}] = @{c}"));

        var sql = $"UPDATE [{table}] SET {setClause} WHERE [{pk}] = @Id";
        var parameters = new DynamicParameters();
        foreach (var col in updateCols)
        {
            var columnMeta = Columns.First(c => c.ColumnName == col);
            if (columnMeta.DataType == "bit")
            {
                var strVal = EditRecord.ContainsKey(col) ? EditRecord[col] : "False";
                var value = strVal == "True" || strVal == "true" || strVal == "1" || strVal == "on";
                parameters.Add("@" + col, value);
            }
            else if (columnMeta.DataType == "date" || columnMeta.DataType == "datetime")
            {
                var strVal = EditRecord.ContainsKey(col) ? EditRecord[col] : null;
                if (string.IsNullOrWhiteSpace(strVal) && !columnMeta.IsNullable)
                    throw new Exception($"The field '{col}' is required.");
                parameters.Add("@" + col, string.IsNullOrWhiteSpace(strVal) ? (object)DBNull.Value : strVal);
            }
            else
            {
                var strVal = EditRecord.ContainsKey(col) ? EditRecord[col] : null;
                if (string.IsNullOrWhiteSpace(strVal) && !columnMeta.IsNullable)
                    throw new Exception($"The field '{col}' is required.");
                parameters.Add("@" + col, string.IsNullOrWhiteSpace(strVal) ? (object)DBNull.Value : strVal);
            }
        }
        parameters.Add("@Id", id);

        using var conn = _dbHelper.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);

        Message = "Record updated!";
        return RedirectToPage("TableList", new { table });
    }
}