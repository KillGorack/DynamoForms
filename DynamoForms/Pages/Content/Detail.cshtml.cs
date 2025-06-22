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

    public DetailModel(DatabaseHelper dbHelper, AppRegistryService registryService)
        : base(registryService)
    {
        _dbHelper = dbHelper;

        Console.WriteLine($"In the detail view class");
    }

    public Dictionary<string, object> Record { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Ensure the registry is loaded
        if (Registry == null || Registry.ValidatedQuery == null || Registry.Settings == null)
        {
            return NotFound("Registry data is missing.");
        }

        // Retrieve the table name and ID from the registry
        if (!Registry.ValidatedQuery.TryGetValue("id", out var idValue) || !int.TryParse(idValue?.ToString(), out var id))
        {
            return BadRequest("Invalid or missing ID in the query.");
        }

        if (!Registry.Settings.TryGetValue("Var", out var tableName) || string.IsNullOrWhiteSpace(tableName?.ToString()))
        {
            return BadRequest("Invalid or missing table name in the registry.");
        }

        // Fetch the record from the database
        var pk = Registry.Columns.FirstOrDefault(c => c.IsPrimaryKey)?.ColumnName;
        if (string.IsNullOrWhiteSpace(pk))
        {
            return BadRequest("Primary key column not defined in the registry.");
        }

        using var conn = _dbHelper.CreateConnection();
        var sql = $"SELECT * FROM [{tableName}] WHERE [{pk}] = @Id";

        // Debugging: Print the SQL query and parameters
        Console.WriteLine("Executing SQL:");
        Console.WriteLine(sql);
        Console.WriteLine($"Parameters: Id = {id}");
        Console.WriteLine($"Table: = {tableName}");

        var record = await conn.QueryFirstOrDefaultAsync(sql, new { Id = id });

        if (record == null)
        {
            Console.WriteLine("No record found.");
            return NotFound("Record not found.");
        }

        // Convert the record to a dictionary for rendering
        Record = ((IDictionary<string, object>)record)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Debugging: Log the contents of Record
        Console.WriteLine("Fetched Record:");
        foreach (var kv in Record)
        {
            Console.WriteLine($"Key: {kv.Key}, Value: {kv.Value}");
        }

        // Debugging: Log the contents of Registry.Fields
        Console.WriteLine("Registry Fields:");
        foreach (var field in Registry.Fields)
        {
            Console.WriteLine($"Key: {field.Key}, ShowInDetail: {field.Value.ShowInDetail}, Label: {field.Value.Label}, Name: {field.Value.Name}, Type: {field.Value.Type}");
        }

        return Page();
    }
}