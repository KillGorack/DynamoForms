using DynamoForms.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class NaviiViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Retrieve the Registry from ViewData
        var registry = ViewData["Registry"] as AppRegistry;

        if (registry == null || registry.Settings == null)
        {
            Console.WriteLine("Registry or Settings is null in NaviiViewComponent.");
            return View("Default", "default"); // Provide a fallback value
        }

        // Retrieve "Var" from Registry.Settings
        if (registry.Settings.TryGetValue("Var", out var varValue) && varValue is string appVar)
        {
            Console.WriteLine($"Model being passed to the view: {appVar} (Type: {appVar.GetType().Name})");
            return View("Default", appVar); // Pass the "Var" value to the view
        }

        Console.WriteLine("Registry.Settings does not contain 'Var' or it is not a string.");
        return View("Default", "default"); // Provide a fallback value
    }
}