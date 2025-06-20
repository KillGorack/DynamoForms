using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DynamoForms.Data;
using System.Collections.Generic;

public class NaviViewComponent : ViewComponent
{
    private readonly DatabaseHelper _dbHelper;

    public NaviViewComponent(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Use FetchDataAsync to get all enabled applications
        var sql = "SELECT Name, Var FROM Application WHERE Enabled = 1 ORDER BY Name";
        var result = await _dbHelper.FetchDataAsync(sql, 2);

        var apps = new List<(string Name, string Var)>();
        if (result is List<Dictionary<string, object>> rows)
        {
            foreach (var row in rows)
            {
                apps.Add((
                    row["Name"]?.ToString() ?? "",
                    row["Var"]?.ToString() ?? ""
                ));
            }
        }

        return View(apps);
    }
}