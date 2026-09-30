using ShoppingListApp.Components;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Data;
using ShoppingListApp.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(o
    => o.UseNpgsql(configuration.GetConnectionString("Default"))
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapShoppingListEndpoints();
app.MapSectionEndpoints();
app.MapItemEndpoints();

if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
