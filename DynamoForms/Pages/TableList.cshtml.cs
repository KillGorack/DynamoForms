using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamoForms.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Dapper;
using DynamoForms.Data;
using System;

public class TableListModel : PageModel
{
    private readonly DatabaseHelper _dbHelper;

    public TableListModel(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<string> TableNames { get; set; }
    public string TableName { get; set; }
    public List<TableColumnMeta> Columns { get; set; }
    public List<Dictionary<string, object>> Records { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10; // Or any default page size you want
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

    public async Task OnGetAsync(string table = null, int pageNumber = 1)
    {
        TableNames = await _dbHelper.GetAllTableNamesAsync();
        TableName = table ?? TableNames.FirstOrDefault();
        PageNumber = pageNumber;

        if (TableName != null)
        {
            Columns = await _dbHelper.GetTableMetaAsync(TableName);
            TotalRecords = await _dbHelper.GetRecordCountAsync(TableName);
            Records = await _dbHelper.GetPagedRecordsAsync(TableName, PageNumber, PageSize);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string table, int id)
    {
        var columns = await _dbHelper.GetTableMetaAsync(table);
        var pk = columns.FirstOrDefault(c => c.IsPrimaryKey)?.ColumnName;
        if (!string.IsNullOrEmpty(pk))
        {
            var sql = $"DELETE FROM [{table}] WHERE [{pk}] = @Id";
            using var conn = _dbHelper.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }
        return RedirectToPage(new { table });
    }
}