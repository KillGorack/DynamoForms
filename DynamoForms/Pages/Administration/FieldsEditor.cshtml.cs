using DynamoForms.Data;
using DynamoForms.Models;
using DynamoForms.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamoForms.Pages.Administration
{
    public class FieldsEditorModel : abstract_BasePageModel
    {
        private readonly DatabaseHelper _databaseHelper;
        private int _appId; // Class-level variable for appId
        public int AppId { get; set; }

        public FieldsEditorModel(DatabaseHelper databaseHelper, AppRegistryService registryService)
            : base(registryService) // Pass the registry service to the base class
        {
            _databaseHelper = databaseHelper;
        }

        [BindProperty]
        public string TableName { get; set; }

        [BindProperty]
        public List<UnifiedField> Attributes { get; set; }

        public async Task OnGetAsync(string app)
        {
            if (Registry == null)
            {
                throw new Exception("Registry is not initialized. Please ensure the 'app' query parameter is provided.");
            }

            // Use the Registry property from the base class
            if (Registry.Settings.TryGetValue("Var", out var varValue) && varValue is string tableName)
            {
                TableName = tableName;
            }
            else
            {
                throw new Exception("The 'Var' key is missing or not a string in the Registry.Settings.");
            }

            // Retrieve the app ID from the Registry
            if (!Registry.Settings.TryGetValue("Id", out var appIdValue) || appIdValue is not int appId)
            {
                throw new Exception("The 'ID' key is missing or not an integer in the Registry.Settings.");
            }

            _appId = appId; // Store the appId as a class-level variable

            // Ensure data exists in the database
            await EnsureDataExistsAsync();

            // Load attributes for the table
            Attributes = await GetAttributesAsync();
        }

        private async Task EnsureDataExistsAsync()
        {
            // Check if records exist for the app using the appId
            var sqlCheck = "SELECT COUNT(*) FROM fld WHERE fld_app = @AppId";
            var parameters = new { AppId = _appId };
            var result = await _databaseHelper.FetchDataAsync(sqlCheck, dim: 0, parameters);
            var recordCount = result != null ? Convert.ToInt32(result) : 0;

            if (recordCount == 0)
            {
                // Get the list of fields (columns) for the table
                var fields = await _databaseHelper.GetTableMetaAsync(TableName);

                // Insert default records for each field
                foreach (var field in fields)
                {
                    var sqlInsert = @"
                    INSERT INTO fld (fld_app, fld_human, fld_column, fld_enable, fld_type, fld_length, fld_precision, fld_opt)
                    VALUES (@AppId, @HumanName, @ColumnName, @Enabled, @Type, @Length, @Precision, @Required)";
                    var insertParameters = new
                    {
                        AppId = _appId,
                        HumanName = field.Label,
                        ColumnName = field.Label,
                        Enabled = false,
                        Type = field.Type,
                        Length = field.Length?.ToString(),
                        Precision = (string)null,
                        Required = field.IsNullable,
                    };

                    await _databaseHelper.ExecuteAsync(sqlInsert, insertParameters);
                }
            }
        }

        private async Task<List<UnifiedField>> GetAttributesAsync()
        {
            // SQL query to retrieve all attributes for the specified app
            var sql = "SELECT * FROM fld WHERE fld_app = @AppId";
            var parameters = new { AppId = _appId };

            // Fetch data from the database
            var result = await _databaseHelper.FetchDataAsync(sql, dim: 2, parameters) as List<Dictionary<string, object>>;

            // Map the result to a list of UnifiedField objects
            return result?.Select(row => new UnifiedField
            {
                ID = row.ContainsKey("ID") ? Convert.ToInt32(row["ID"]) : 0,
                AppId = row.ContainsKey("fld_app") ? Convert.ToInt32(row["fld_app"]) : 0,
                Name = row.ContainsKey("fld_column") ? row["fld_column"]?.ToString() : null,
                Label = row.ContainsKey("fld_human") ? row["fld_human"]?.ToString() : null,
                Enabled = row.ContainsKey("fld_enable") && row["fld_enable"] is bool enable ? enable : false,
                Type = row.ContainsKey("fld_type") ? row["fld_type"]?.ToString() : null,
                Length = row.ContainsKey("fld_length") ? row["fld_length"]?.ToString() : null,
                Precision = row.ContainsKey("fld_precision") ? row["fld_precision"]?.ToString() : null,
                IsNullable = row.ContainsKey("fld_required") && row["fld_required"] is bool required ? required : false,
                IsOption = row.ContainsKey("fld_opt") && row["fld_opt"] is bool opt ? opt : false,
                IconSet = row.ContainsKey("fld_icon_set") ? row["fld_icon_set"]?.ToString() : null,
                Regex = row.ContainsKey("fld_regex") ? row["fld_regex"]?.ToString() : null,
                UnitOfMeasure = row.ContainsKey("fld_uom") ? row["fld_uom"]?.ToString() : null,
                Placeholder = row.ContainsKey("fld_placeholder") ? row["fld_placeholder"]?.ToString() : null,
                IsUserId = row.ContainsKey("fld_usr_ID") && row["fld_usr_ID"] is bool userId ? userId : false,
                IsLink = row.ContainsKey("fld_link") && row["fld_link"] is bool link ? link : false,
                ShowInList = row.ContainsKey("fld_index") && row["fld_index"] is bool index ? index : false,
                ShowInDetail = row.ContainsKey("fld_detail") && row["fld_detail"] is bool detail ? detail : false,
                ShowInForm = row.ContainsKey("fld_form") && row["fld_form"] is bool form ? form : false,
                Order = row.ContainsKey("fld_order") && row["fld_order"] != DBNull.Value ? Convert.ToInt32(row["fld_order"]) : (int?)null,
                IsTitle = row.ContainsKey("fld_title") && row["fld_title"] is bool title ? title : false,
                IsPassword = row.ContainsKey("fld_pass") && row["fld_pass"] is bool password ? password : false,
                IsDouble = row.ContainsKey("fld_double") && row["fld_double"] is bool doubleValue ? doubleValue : false,
                IsEncrypted = row.ContainsKey("fld_encrypt") && row["fld_encrypt"] is bool encrypt ? encrypt : false,
                IsTime = row.ContainsKey("fld_time") && row["fld_time"] is bool time ? time : false,
                IsImage = row.ContainsKey("fld_image") && row["fld_image"] is bool image ? image : false,
                IsUnique = row.ContainsKey("fld_unique") && row["fld_unique"] is bool unique ? unique : false,
                IsJson = row.ContainsKey("fld_json") && row["fld_json"] is bool json ? json : false
            }).ToList() ?? new List<UnifiedField>();
        }

        public async Task<IActionResult> OnPostAsync(string app)
        {
            TableName = app;
            var groupedAttributes = new Dictionary<int, Dictionary<string, object>>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("Attributes["))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(key, @"Attributes\[(\d+)\]\.(\w+)");
                    if (match.Success)
                    {
                        var id = int.Parse(match.Groups[1].Value); // Extracted ID (e.g., 28)
                        var propertyName = match.Groups[2].Value;  // Extracted property name (e.g., "HumanName")

                        if (!groupedAttributes.ContainsKey(id))
                        {
                            groupedAttributes[id] = new Dictionary<string, object>();
                        }
                        groupedAttributes[id][propertyName] = Request.Form[key];
                    }
                }
            }
            foreach (var record in groupedAttributes)
            {
                await UpdateRecordAsync(record.Key, record.Value);
            }
            return RedirectToPage("/Administration/FieldsEditor", new { app = TableName });
        }

        private async Task UpdateRecordAsync(int recordId, Dictionary<string, object> attributes)
        {
            Console.WriteLine($"UpdateRecordAsync called for Record ID: {recordId}");

            // Add the record ID to the attributes dictionary
            attributes["ID"] = recordId;

            // Define default values for all expected parameters
            var defaultValues = new Dictionary<string, object>
            {
                { "HumanName", "" },
                { "ColumnName", "" },
                { "Enabled", false },
                { "Type", "" },
                { "Length", "" },
                { "Precision", "" },
                { "Required", false },
                { "Option", false },
                { "IconSet", "" },
                { "Regex", "" },
                { "UnitOfMeasure", "" },
                { "Placeholder", "" },
                { "UserId", false },
                { "Link", false },
                { "Index", false },
                { "Detail", false },
                { "Form", false },
                { "Order", 0 },
                { "Title", false },
                { "Password", false },
                { "Double", false },
                { "Encrypt", false },
                { "Time", false },
                { "Image", false },
                { "Unique", false },
                { "Json", false }
            };

            foreach (var key in defaultValues.Keys)
            {
                if (!attributes.ContainsKey(key))
                {
                    attributes[key] = defaultValues[key];
                }
            }

            var sql = @"
            UPDATE fld
            SET 
                fld_human = @HumanName,
                fld_column = @ColumnName,
                fld_enable = @Enabled,
                fld_type = @Type,
                fld_length = @Length,
                fld_precision = @Precision,
                fld_required = @Required,
                fld_opt = @Option,
                fld_icon_set = @IconSet,
                fld_regex = @Regex,
                fld_uom = @UnitOfMeasure,
                fld_placeholder = @Placeholder,
                fld_usr_ID = @UserId,
                fld_link = @Link,
                fld_index = @Index,
                fld_detail = @Detail,
                fld_form = @Form,
                fld_order = @Order,
                fld_title = @Title,
                fld_pass = @Password,
                fld_double = @Double,
                fld_encrypt = @Encrypt,
                fld_time = @Time,
                fld_image = @Image,
                fld_unique = @Unique,
                fld_json = @Json
            WHERE ID = @ID;";

            var success = await _databaseHelper.ExecuteAsync(sql, attributes);

            if (!success)
            {
                throw new Exception($"Failed to update the record with ID {recordId}.");
            }
        }
    }
}