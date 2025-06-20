using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using DynamoForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TableAddModel : abstract_BasePageModel
{
    private readonly DatabaseHelper _dbHelper;

    public TableAddModel(AppRegistryService registryService, DatabaseHelper dbHelper)
        : base(registryService)
    {
        _dbHelper = dbHelper;
    }

    public Dictionary<string, DynamicFormField> Fields { get; set; }
    public string TableName { get; set; }
    public string Message { get; set; }

    [BindProperty]
    public Dictionary<string, string> NewRecord { get; set; } = new();

    public async Task OnGetAsync(string table)
    {
        TableName = table;
        Fields = Registry.Fields;

        // Set current datetime for any datetime field
        foreach (var field in Fields.Values)
        {
            if (field.Type == "datetime")
            {
                NewRecord[field.Name] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
        }
    }

    public async Task<IActionResult> OnPostAsync(string app)
    {
        TableName = app;
        Fields = Registry.Fields;

        var insertFields = Fields.Values
            .Where(f => !string.Equals(f.Name, "ID", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var insertCols = insertFields.Select(f => f.Name).ToList();
        var paramNames = insertCols.Select(c => "@" + c).ToList();

        var sql = $"INSERT INTO [{app}] ({string.Join(",", insertCols.Select(c => $"[{c}]"))}) VALUES ({string.Join(",", paramNames)})";

        var parameters = new DynamicParameters();
        foreach (var field in insertFields)
        {
            if (field.Type == "bit")
            {
                var strVal = NewRecord.ContainsKey(field.Name) ? NewRecord[field.Name] : "False";
                var value = strVal == "True" || strVal == "true" || strVal == "1" || strVal == "on";
                parameters.Add("@" + field.Name, value);
            }
            else
            {
                parameters.Add("@" + field.Name, NewRecord.ContainsKey(field.Name) ? NewRecord[field.Name] : null);
            }
        }

        using var conn = _dbHelper.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);

        Message = "Record added!";
        return RedirectToPage("/Content/List", new { app });
    }
}