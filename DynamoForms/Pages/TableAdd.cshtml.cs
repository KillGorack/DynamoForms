using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamoForms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Dapper;
using DynamoForms.Data;
using System;

public class TableAddModel : PageModel
{
    private readonly DatabaseHelper _dbHelper;

    public TableAddModel(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<TableColumnMeta> Columns { get; set; }
    public string TableName { get; set; }
    public string Message { get; set; }

    [BindProperty]
    public Dictionary<string, string> NewRecord { get; set; } = new();

    public async Task OnGetAsync(string table)
    {
        TableName = table;
        Columns = await _dbHelper.GetTableMetaAsync(table);

        // Set current datetime for any datetime column
        foreach (var col in Columns)
        {
            if (col.DataType == "datetime")
            {
                NewRecord[col.ColumnName] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
        }
    }

    public async Task<IActionResult> OnPostAsync(string table)
    {
        TableName = table;
        Columns = await _dbHelper.GetTableMetaAsync(table);

        var insertCols = Columns.Where(c => !c.IsIdentity).Select(c => c.ColumnName).ToList();
        var paramNames = insertCols.Select(c => "@" + c).ToList();

        var sql = $"INSERT INTO [{table}] ({string.Join(",", insertCols.Select(c => $"[{c}]"))}) VALUES ({string.Join(",", paramNames)})";

        var parameters = new DynamicParameters();
        foreach (var col in insertCols)
        {
            var columnMeta = Columns.First(c => c.ColumnName == col);
            if (columnMeta.DataType == "bit")
            {
                var strVal = NewRecord.ContainsKey(col) ? NewRecord[col] : "False";
                var value = strVal == "True" || strVal == "true" || strVal == "1" || strVal == "on";
                parameters.Add("@" + col, value);
            }
            else
            {
                parameters.Add("@" + col, NewRecord.ContainsKey(col) ? NewRecord[col] : null);
            }
        }

        using var conn = _dbHelper.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);

        Message = "Record added!";
        return RedirectToPage("TableList", new { table });
    }
}