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

    public async Task<Dictionary<string, UnifiedField>> LoadAsync(string tableName)
    {
        // Log the settings to the console for debugging
        if (_registry.Settings == null)
        {
            throw new Exception("Registry settings are null. Ensure AppRegistry is properly initialized.");
        }

        if (!_registry.Settings.TryGetValue("ID", out var appIdObj) || appIdObj is not int appId)
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

        // Log the raw data for debugging
        if (fldData is List<Dictionary<string, object>> rawData)
        {

        }
        else
        {
            Console.WriteLine("No data returned or unexpected format.");
            return new Dictionary<string, UnifiedField>();
        }

        // Map the data to a dictionary of UnifiedField
        var fields = new Dictionary<string, UnifiedField>();
        foreach (var row in (List<Dictionary<string, object>>)fldData)
        {
            var columnName = row["ColumnName"]?.ToString();

            // Skip rows with empty or null ColumnName
            if (string.IsNullOrWhiteSpace(columnName))
            {
                Console.WriteLine("Skipping row with empty or null ColumnName.");
                continue;
            }

            // Handle duplicate ColumnName values
            if (fields.ContainsKey(columnName))
            {
                Console.WriteLine($"Duplicate ColumnName detected: {columnName}. Skipping...");
                continue;
            }

            // Map the row to a UnifiedField object
            fields[columnName] = new UnifiedField
            {
                Name = columnName,
                Type = row["DataType"]?.ToString(),
                Length = row["Length"]?.ToString(),
                Precision = row["Precision"]?.ToString(),
                IsUnique = row["IsUnique"] != null && Convert.ToBoolean(row["IsUnique"]),
                IsNullable = row["Required"] != null && Convert.ToBoolean(row["Required"]),
                Enabled = row["Enabled"] != null && Convert.ToBoolean(row["Enabled"]),
                Label = row["Label"]?.ToString(),
                IsOption = row["IsOption"] != null && Convert.ToBoolean(row["IsOption"]),
                IconSet = row["IconSet"]?.ToString(),
                Regex = row["Regex"]?.ToString(),
                UnitOfMeasure = row["UnitOfMeasure"]?.ToString(),
                Placeholder = row["Placeholder"]?.ToString(),
                IsUserId = row["IsUserId"] != null && Convert.ToBoolean(row["IsUserId"]),
                IsLink = row["IsLink"] != null && Convert.ToBoolean(row["IsLink"]),
                ShowInList = row["ShowInList"] != null && Convert.ToBoolean(row["ShowInList"]),
                ShowInDetail = row["ShowInDetail"] != null && Convert.ToBoolean(row["ShowInDetail"]),
                ShowInForm = row["ShowInForm"] != null && Convert.ToBoolean(row["ShowInForm"]),
                Order = row["Order"] != null && row["Order"] != DBNull.Value ? Convert.ToInt32(row["Order"]) : (int?)null,
                IsTitle = row["IsTitle"] != null && Convert.ToBoolean(row["IsTitle"]),
                IsPassword = row["IsPassword"] != null && Convert.ToBoolean(row["IsPassword"]),
                IsDouble = row["IsDouble"] != null && Convert.ToBoolean(row["IsDouble"]),
                IsEncrypted = row["IsEncrypted"] != null && Convert.ToBoolean(row["IsEncrypted"]),
                IsTime = row["IsTime"] != null && Convert.ToBoolean(row["IsTime"]),
                IsImage = row["IsImage"] != null && Convert.ToBoolean(row["IsImage"]),
                IsJson = row["IsJson"] != null && Convert.ToBoolean(row["IsJson"])
            };
        }

        return fields;
    }

    public List<UnifiedField> ToColumnMeta(Dictionary<string, UnifiedField> fields)
    {
        if (fields == null) return new List<UnifiedField>();

        // Map the UnifiedField data to TableColumnMeta using the fld table data
        return fields.Values.Select(f =>
        {
            return new UnifiedField
            {
                Label = f.Name, // fld_column
                Type = f.Type, // fld_type
                IsNullable = f.IsNullable ? false : true, // fld_required
                IsPrimaryKey = f.Name.Equals("ID", StringComparison.OrdinalIgnoreCase),
                Length = f.Length
            };
        }).ToList();
    }
}