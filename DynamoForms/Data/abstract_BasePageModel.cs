using Microsoft.AspNetCore.Mvc.RazorPages;
using DynamoForms.Services;
using System.Threading.Tasks;
using DynamoForms.Data;

public abstract class abstract_BasePageModel : PageModel
{
    protected readonly AppRegistryService _registryService;
    public AppRegistry Registry { get; private set; }

    public abstract_BasePageModel(AppRegistryService registryService)
    {
        _registryService = registryService;
    }


    private void LogRequestData(Microsoft.AspNetCore.Http.HttpRequest request)
    {
        if (request.Method == "POST")
        {
            Console.WriteLine("POST Data Debug:");
            foreach (var key in request.Form.Keys)
            {
                Console.WriteLine($"Key: {key}, Value: {request.Form[key]}");
            }
        }
    }


    public override async Task OnPageHandlerExecutionAsync(
        Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context,
        Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutionDelegate next)
    {
        var appVar = context.HttpContext.Request.Query["app"].ToString();


        if (string.IsNullOrEmpty(appVar))
        {
            throw new System.Exception("The 'app' query parameter is required but was not provided.");
        }

        Registry = await _registryService.BuildAsync(appVar, context.HttpContext.Request.Query);

        if (Registry == null)
        {
            throw new System.Exception($"Failed to build registry for app '{appVar}'.");
        }

        ViewData["Registry"] = Registry;

        await next();
    }
}