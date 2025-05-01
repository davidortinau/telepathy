using Microsoft.EntityFrameworkCore;
using Telepathic.Shared.Services;
using Telepathic.Web.Models;
using Telepathic.Shared.Models;
using Telepathic.Web.Components;
using Telepathic.Web.Data;
using Telepathic.Web.Data.Repositories;
using Telepathic.Web.Services;
using Syncfusion.Blazor;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("REDACTED_SECRET");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddSyncfusionBlazor();

// Configure EF Core with SQL Server
builder.Services.AddDbContext<TelepathicDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IRepository<Project>, ProjectRepository>();
builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
builder.Services.AddScoped<IRepository<ProjectTask>, ProjectTaskRepository>();
builder.Services.AddScoped<IRepository<TeamMember>, TeamMemberRepository>();

// Register database initializer
builder.Services.AddScoped<DatabaseInitializer>();

// Add device-specific services used by the Telepathic.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<ITeamDataService, TeamDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Telepathic.Shared._Imports).Assembly);

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var initializer = services.GetRequiredService<DatabaseInitializer>();
        initializer.InitializeDatabaseAsync().Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

//Map minimal APIs to expose data to the client-side MAUI app
app.MapGet("/api/teamtaskload", async (ITeamDataService teamDataService) =>
{
    var teamTaskLoad = await teamDataService.GetTeamTaskLoadAsync();
    return Results.Ok(teamTaskLoad);
});
app.MapGet("/api/categorytaskload", async (ITeamDataService teamDataService) =>
{
    var categoryTaskLoad = await teamDataService.GetCategoryTaskLoadAsync();
    return Results.Ok(categoryTaskLoad);
});
app.MapGet("/api/tasksdue", async (ITeamDataService teamDataService) =>
{
    var tasksDue = await teamDataService.GetTasksDueAsync();
    return Results.Ok(tasksDue);
});

app.Run();
