using Microsoft.AspNetCore.Http.HttpResults;
using ShoppingListApp.Components;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Components.Partials;
using ShoppingListApp.Data;
using ShoppingListApp.Entities;

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

app.MapDelete("/shopping-list/{id:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, Guid id) =>
{
    var db = await dbFactory.CreateDbContextAsync();
    var shoppingList = await db.ShoppingList.FindAsync(id);
    if (shoppingList is null)
    {
        return Results.NotFound();
    }

    db.ShoppingList.Remove(shoppingList);
    await db.SaveChangesAsync();
    
    // one could argue for 204 NoContent instead of 200 Ok
    // however, this is primarily for HTMX and HTMX ignores 204 NoContent
    return Results.Ok(); 
});
app.MapPost("/shopping-list/{shoppingListId:Guid}/section", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId) =>
{
    var db = await dbFactory.CreateDbContextAsync();
    var form = await httpContext.Request.ReadFormAsync();
    var name = form["name"].ToString();
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest();
    }

    var section = new Section
    {
        ShoppingListId = shoppingListId,
        Name = name
    };
    
    db.Section.Add(section);
    await db.SaveChangesAsync();
    return new RazorComponentResult<SectionComponent>(new
    {
        ShoppingListId = shoppingListId,
        Section = section,
        Items = new List<ListItem>()
    });
});
app.MapDelete("/shopping-list/{shoppingListId:Guid}/section/{sectionId:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, Guid sectionId) =>
{
    var db = await dbFactory.CreateDbContextAsync();
    var section = await db.Section.FindAsync(sectionId);
    if (section is null || section.ShoppingListId != shoppingListId)
    {
        return Results.NotFound();
    }

    db.Section.Remove(section);
    await db.SaveChangesAsync();
    
    // one could argue for 204 NoContent instead of 200 Ok
    // however, this is primarily for HTMX and HTMX ignores 204 NoContent
    return Results.Ok(); 
});
app.MapPost("/shopping-list/{shoppingListId:Guid}/item", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId) =>
{
    var db = await dbFactory.CreateDbContextAsync();
    var form = await httpContext.Request.ReadFormAsync();
    var name = form["name"].ToString();
    var sectionId = new Guid(form["sectionId"].ToString());
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest();
    }

    var item = await db.Item.FirstOrDefaultAsync(i => i.Name == name);
    if (item is null)
    {
        item = new Item
        {
            Name = name
        };
        db.Add(item);
    }
    
    var listItem = new ListItem
    {
        ShoppingListId = shoppingListId,
        SectionId = sectionId,
        Item = item
    };
    
    db.ListItem.Add(listItem);
    await db.SaveChangesAsync();
    return new RazorComponentResult<ItemCard>(new
    {
        ShoppingListId = shoppingListId,
        SectionId = sectionId,
        Item = listItem
    });
});
app.MapDelete("/shopping-list/{shoppingListId:Guid}/item/{itemId:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, Guid itemId) =>
{
    var db = await dbFactory.CreateDbContextAsync();
    var item = await db.ListItem.FindAsync(itemId);
    if (item is null || item.ShoppingListId != shoppingListId)
    {
        return Results.NotFound();
    }

    db.ListItem.Remove(item);
    await db.SaveChangesAsync();
    
    // one could argue for 204 NoContent instead of 200 Ok
    // however, this is primarily for HTMX and HTMX ignores 204 NoContent
    return Results.Ok(); 
});

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
