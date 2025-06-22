using DynamoForms.Data;
using DynamoForms.Models;

public class FieldDefinitions
{
    private readonly DatabaseHelper _dbHelper;
    private readonly AppRegistry _registry;

    public FieldDefinitions(DatabaseHelper dbHelper, AppRegistry registry)
    {
        _dbHelper = dbHelper;
        _registry = registry;
    }

    public async Task<Dictionary<string, DynamicFormField>> LoadAsync(string tableName)
    {
        // Log the settings to the console for debugging
        if (_registry.Settings == null)
        {
            throw new Exception("Registry settings are null. Ensure AppRegistry is properly initialized.");
        }

        foreach (var setting in _registry.Settings)
        {
            Console.WriteLine($"Key: {setting.Key}, Value: {setting.Value}");
        }

        if (!_registry.Settings.TryGetValue("Id", out var appIdObj) || appIdObj is not int appId)
        {
            throw new Exception("AppId is missing or invalid in the registry settings.");
        }

        // SQL query to fetch data from the fld table
        string sql = @"
            SELECT 
                fld_column AS ColumnName,
                fld_type AS DataType,
                fld_length AS Length,
                fld_precision AS Precision,
                fld_unique AS IsUnique,
                fld_required AS Required,
                fld_enable AS Enabled,
                fld_human AS Label,
                fld_opt AS IsOption,
                fld_icon_set AS IconSet,
                fld_regex AS Regex,
                fld_uom AS UnitOfMeasure,
                fld_placeholder AS Placeholder,
                fld_usr_ID AS IsUserId,
                fld_link AS IsLink,
                fld_index AS ShowInList,
                fld_detail AS ShowInDetail,
                fld_form AS ShowInForm,
                fld_order AS [Order],
                fld_title AS IsTitle,
                fld_pass AS IsPassword,
                fld_double AS IsDouble,
                fld_encrypt AS IsEncrypted,
                fld_time AS IsTime,
                fld_image AS IsImage,
                fld_json AS IsJson
            FROM fld
            WHERE fld_app = @AppId";

        // Fetch data from the fld table
        var parameters = new { AppId = appId };
        var fldData = await _dbHelper.FetchDataAsync(sql, parameters: parameters);

        // Map the data to a dictionary of DynamicFormField
        var fields = ((List<Dictionary<string, object>>)fldData).ToDictionary(
            row => row["ColumnName"].ToString(),
            row => new DynamicFormField
            {
                Name = row["ColumnName"].ToString(),
                Type = row["DataType"]?.ToString(),
                Length = row["Length"]?.ToString(),
                Precision = row["Precision"]?.ToString(),
                IsUnique = Convert.ToBoolean(row["IsUnique"]),
                Required = Convert.ToBoolean(row["Required"]),
                Enabled = Convert.ToBoolean(row["Enabled"]),
                Label = row["Label"]?.ToString(),
                IsOption = Convert.ToBoolean(row["IsOption"]),
                IconSet = row["IconSet"]?.ToString(),
                Regex = row["Regex"]?.ToString(),
                UnitOfMeasure = row["UnitOfMeasure"]?.ToString(),
                Placeholder = row["Placeholder"]?.ToString(),
                IsUserId = Convert.ToBoolean(row["IsUserId"]),
                IsLink = Convert.ToBoolean(row["IsLink"]),
                ShowInList = Convert.ToBoolean(row["ShowInList"]),
                ShowInDetail = Convert.ToBoolean(row["ShowInDetail"]),
                ShowInForm = Convert.ToBoolean(row["ShowInForm"]),
                Order = row["Order"] as int?,
                IsTitle = Convert.ToBoolean(row["IsTitle"]),
                IsPassword = Convert.ToBoolean(row["IsPassword"]),
                IsDouble = Convert.ToBoolean(row["IsDouble"]),
                IsEncrypted = Convert.ToBoolean(row["IsEncrypted"]),
                IsTime = Convert.ToBoolean(row["IsTime"]),
                IsImage = Convert.ToBoolean(row["IsImage"]),
                IsJson = Convert.ToBoolean(row["IsJson"])
            }
        );

        return fields;
    }

    public List<TableColumnMeta> ToColumnMeta(Dictionary<string, DynamicFormField> fields)
    {
        if (fields == null) return new List<TableColumnMeta>();

        // Map the DynamicFormField data to TableColumnMeta using the fld table data
        return fields.Values.Select(f =>
        {
            return new TableColumnMeta
            {
                ColumnName = f.Name, // fld_column
                DataType = f.Type, // fld_type
                IsNullable = f.Required ? false : true, // fld_required
                IsIdentity = f.IsIdentity, // fld_usr_ID (or similar)
                IsPrimaryKey = f.Name.Equals("ID", StringComparison.OrdinalIgnoreCase),
                MaxLength = int.TryParse(f.Length, out var len) ? len : null // fld_length
            };
        }).ToList();
    }
}