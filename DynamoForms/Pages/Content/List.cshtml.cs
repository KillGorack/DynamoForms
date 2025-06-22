using Dapper;
using DynamoForms.Data;
using DynamoForms.Models;
using DynamoForms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TableListModel : abstract_BasePageModel
{
    private readonly DatabaseHelper _dbHelper;
    private readonly DeleteService _deleteService;

    public TableListModel(DatabaseHelper dbHelper, AppRegistryService registryService, DeleteService deleteService)
        : base(registryService)
    {
        _dbHelper = dbHelper;
        _deleteService = deleteService;
    }

    public List<string> TableNames { get; set; }
    public string TableName { get; set; }
    public List<UnifiedField> Columns { get; set; }
    public Dictionary<string, UnifiedField> Fields { get; set; }
    public List<Dictionary<string, object>> Records { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public string SortColumn { get; set; }
    public bool SortDescending { get; set; }
    public Dictionary<string, string> Filters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string table = null, int pageNumber = 1, string sortColumn = null, bool sortDesc = false, string app = null)
    {
        // Load table names and set the current table
        TableNames = await _dbHelper.GetAllTableNamesAsync();
        TableName = app ?? TableNames.FirstOrDefault();
        PageNumber = pageNumber;
        SortColumn = sortColumn;
        SortDescending = sortDesc;

        // Collect filters from query string
        Filters = Request.Query
            .Where(q => q.Key.StartsWith("filter_"))
            .ToDictionary(q => q.Key.Substring(7), q => q.Value.ToString());

        // Ensure a valid table is selected
        if (TableName == null)
        {
            return RedirectToPage("/Administration/FieldsEditor");
        }

        // Load fields from the registry
        Fields = Registry.Fields;

        // Redirect to FieldsEditor if no fields are available
        if (Fields == null || !Fields.Any())
        {
            return RedirectToPage("/Administration/FieldsEditor", new { app = TableName });
        }

        Columns = Fields.Values.ToList();

        // Fetch records and metadata for the table
        TotalRecords = await _dbHelper.GetFilteredRecordCountAsync(TableName, Filters, Fields.Values.ToList());
        Records = await _dbHelper.GetPagedRecordsAsync(TableName, PageNumber, PageSize, SortColumn, SortDescending, Filters, Fields.Values.ToList());

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string app, string id)
    {
        // Validate that the ID is an integer
        if (!int.TryParse(id, out var parsedId))
        {
            throw new ArgumentException("Invalid ID provided.");
        }

        // Use the delete service to handle the deletion
        await _deleteService.DeleteRecordAsync(app, parsedId, Registry);

        return RedirectToPage(new { app });
    }
}