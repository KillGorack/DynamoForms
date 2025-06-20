using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DynamoForms.Data
{
    public class QueryStringValidator
    {
        private readonly Dictionary<string, List<string>> WhiteList = new()
        {
            ["sortdir"] = new() { "asc", "desc" },
            ["action"] = new() { "add", "edit", "delete" }
        };

        private readonly HashSet<string> IntegerList = new()
        {
            "id", "page", "limit"
        };

        private readonly DatabaseHelper _databaseHelper;

        public QueryStringValidator(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        public async Task<Dictionary<string, object>> ValidateAsync(IQueryCollection query)
        {
            var result = new Dictionary<string, object>();

            // Whitelist checks
            foreach (var (key, allowed) in WhiteList)
            {
                if (query.TryGetValue(key, out var value) && allowed.Contains(value.ToString()))
                {
                    result[key] = value.ToString();
                }
            }

            // Integer checks
            foreach (var key in IntegerList)
            {
                if (query.TryGetValue(key, out var value) && int.TryParse(value, out var intVal))
                {
                    result[key] = intVal;
                }
            }

            // "app" check against Application.var in DB
            if (query.TryGetValue("app", out var appValue))
            {
                // SQL is here in the validator
                var sql = "SELECT [var] FROM [Application]";
                var data = await _databaseHelper.FetchDataAsync(sql, 2) as List<Dictionary<string, object>>;
                var allVars = data?.Select(d => d["var"]?.ToString()).Where(v => v != null).ToList() ?? new List<string>();

                if (allVars.Contains(appValue.ToString()))
                {
                    result["app"] = appValue.ToString();
                }
            }

            return result;
        }
    }
}