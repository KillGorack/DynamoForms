using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using DynamoForms.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TableEditModel : abstract_BasePageModel
{
    private readonly DatabaseHelper _dbHelper;

    public TableEditModel(DatabaseHelper dbHelper, AppRegistryService registryService)
        : base(registryService)
    {
        _dbHelper = dbHelper;
    }

    public List<UnifiedField> Columns { get; set; }
    public string TableName { get; set; }
    public int EditId { get; set; }
    public string Message { get; set; }

    [BindProperty]
    public Dictionary<string, string> EditRecord { get; set; } = new();

    public async Task OnGetAsync(string app)
    {
        TableName = app;

        // Populate Columns manually from Registry.Fields
        Columns = Registry.Fields.Values.Select(f => new UnifiedField
        {
            Label = f.Name,
            Type = f.Type,
            IsNullable = f.IsNullable,
            IsPrimaryKey = f.Name.Equals("ID", StringComparison.OrdinalIgnoreCase),
            Length = f.Length
        }).ToList();

        // Retrieve the ID from the validated query
        if (Registry.ValidatedQuery.TryGetValue("id", out var idValue) && int.TryParse(idValue?.ToString(), out var id))
        {
            EditId = id;

            var pk = "ID";
            if (pk != null)
            {
                using var conn = _dbHelper.CreateConnection();
                var sql = $"SELECT * FROM [{app}] WHERE [{pk}] = @Id";
                var record = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });
                if (record != null)
                {
                    EditRecord = ((IDictionary<string, object>)record)
                        .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                }
            }
        }
        else
        {
            throw new Exception("Invalid or missing ID in the query string.");
        }
    }

    public async Task<IActionResult> OnPostAsync(string app)
    {
        TableName = app;

        // Populate Columns manually from Registry.Fields
        Columns = Registry.Fields.Values.Select(f => new UnifiedField
        {
            Label = f.Name,
            Type = f.Type,
            IsNullable = f.IsNullable,
            IsPrimaryKey = f.Name.Equals("ID", StringComparison.OrdinalIgnoreCase),
            Length = f.Length
        }).ToList();

        // Retrieve the ID from the validated query
        if (Registry.ValidatedQuery.TryGetValue("id", out var idValue) && int.TryParse(idValue?.ToString(), out var id))
        {
            EditId = id;

            // Set current datetime for any datetime column before saving
            foreach (var col in Columns)
            {
                if (col.Type == "datetime")
                {
                    EditRecord[col.Label] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }

            var pk = Columns.FirstOrDefault(c => c.IsPrimaryKey)?.Label;
            var updateCols = Columns.Where(c => !c.IsPrimaryKey).Select(c => c.Label).ToList();
            var setClause = string.Join(", ", updateCols.Select(c => $"[{c}] = @{c}"));

            var sql = $"UPDATE [{app}] SET {setClause} WHERE [{pk}] = @Id";
            var parameters = new DynamicParameters();
            foreach (var col in updateCols)
            {
                var columnMeta = Columns.First(c => c.Label == col);
                if (columnMeta.Type == "bit")
                {
                    var strVal = EditRecord.ContainsKey(col) ? EditRecord[col] : "False";
                    var value = strVal == "True" || strVal == "true" || strVal == "1" || strVal == "on";
                    parameters.Add("@" + col, value);
                }
                else if (columnMeta.Type == "date" || columnMeta.Type == "datetime")
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

            return RedirectToPage("/Content/Detail", new { id = EditId, app = TableName });
        }
        else
        {
            throw new Exception("Invalid or missing ID in the query string.");
        }
    }
}