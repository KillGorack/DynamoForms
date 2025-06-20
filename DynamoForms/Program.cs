using DynamoForms.Data;
using DynamoForms.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register DatabaseHelper as a scoped service
builder.Services.AddScoped<DatabaseHelper>();
builder.Services.AddScoped<AppRegistryService>();
builder.Services.AddScoped<AppRegistry>();
builder.Services.AddScoped<DeleteService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Content/Index");
    return Task.CompletedTask;
});

app.Run();
