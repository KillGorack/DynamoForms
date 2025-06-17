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
        List<string> tableNames = await _dbHelper.GetAllTableNamesAsync();
        return View(tableNames);
    }
}