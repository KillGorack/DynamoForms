using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using DynamoForms.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DetailModel : abstract_BasePageModel
{
    private readonly DatabaseHelper _dbHelper;
    private readonly DeleteService _deleteService;

    public DetailModel(DatabaseHelper dbHelper, AppRegistryService registryService, DeleteService deleteService)
        : base(registryService)
    {
        _dbHelper = dbHelper;
        _deleteService = deleteService;

        Console.WriteLine($"In the detail view class");
    }

    public Dictionary<string, object> Record { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Registry == null || Registry.ValidatedQuery == null || Registry.Settings == null)
        {
            return NotFound("Registry data is missing.");
        }
        if (!Registry.ValidatedQuery.TryGetValue("id", out var idValue) || !int.TryParse(idValue?.ToString(), out var id))
        {
            return BadRequest("Invalid or missing ID in the query.");
        }

        if (!Registry.Settings.TryGetValue("Var", out var tableName) || string.IsNullOrWhiteSpace(tableName?.ToString()))
        {
            return BadRequest("Invalid or missing table name in the registry.");
        }
        var pk = "ID";
        if (string.IsNullOrWhiteSpace(pk))
        {
            return BadRequest("Primary key column not defined in the registry.");
        }
        using var conn = _dbHelper.CreateConnection();
        var sql = $"SELECT * FROM [{tableName}] WHERE [{pk}] = @Id";
        var record = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });

        if (record == null)
        {
            return NotFound("Record not found.");
        }
        Record = ((IDictionary<string, object>)record)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string app, int id)
    {
        if (string.IsNullOrWhiteSpace(app) || id <= 0)
        {
            return BadRequest("Invalid app or ID.");
        }
        try
        {
            await _deleteService.DeleteRecordAsync(app, id, Registry);
            return RedirectToPage("/Content/List", new { app });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while deleting the record.");
        }
    }
}