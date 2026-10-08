using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Components.Partials;
using ShoppingListApp.Data;
using ShoppingListApp.Entities;
// ReSharper disable RedundantAnonymousTypePropertyName

namespace ShoppingListApp.Extensions;

public static class WebApplicationExtensions
{
    private static string GetStage(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue("stage", out var stage))
        {
            return stage.ToString();
        }

        // HTMX requests do not carry the page's query string, but do send the current URL
        if (httpContext.Request.Headers.TryGetValue("HX-Current-URL", out var currentUrl) &&
            Uri.TryCreate(currentUrl.ToString(), UriKind.Absolute, out var uri) &&
            Uri.UnescapeDataString(uri.Query) is { Length: > 0 } query &&
            System.Web.HttpUtility.ParseQueryString(query)["stage"] is { Length: > 0 } htmxStage)
        {
            return htmxStage;
        }

        return "note";
    }
    extension(WebApplication app)
    {
        [UsedImplicitly]
        public WebApplication MapShoppingListEndpoints()
        {
            app.MapPost("/htmx/shopping-list", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var form = await httpContext.Request.ReadFormAsync();
                var name = form["name"].ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Results.BadRequest();
                }

                var shoppingList = new ShoppingList
                {
                    Name = name
                };
    
                db.ShoppingList.Add(shoppingList);
                await db.SaveChangesAsync();
                return new RazorComponentResult<ShoppingListOptionComponent>(new
                {
                    ShoppingList = shoppingList,
                    IsSelected = false
                });
            });
            
            app.MapDelete("/htmx/shopping-list/{id:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, Guid id) =>
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
            
            app.MapPatch("/htmx/shopping-list/{id:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid id) =>
                {
                    var db = await dbFactory.CreateDbContextAsync();
                    var shoppingList = await db.ShoppingList.FindAsync(id);
                    if (shoppingList is null)
                    {
                        return Results.NotFound();
                    }

                    var form = await httpContext.Request.ReadFormAsync();
                    if (!form.TryGetValue("name", out var rawName) || string.IsNullOrWhiteSpace(rawName.ToString()))
                    {
                        return Results.BadRequest();
                    }
                    shoppingList.Name = rawName.ToString();
                    await db.SaveChangesAsync();

                    return Results.Ok();
                });

            return app;
        }

        [UsedImplicitly]
        public WebApplication MapSectionEndpoints()
        {
            app.MapPost("/htmx/shopping-list/{shoppingListId:Guid}/section/{sectionId:Guid?}/sort",
                async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId, Guid? sectionId) =>
                {
                    var db = await dbFactory.CreateDbContextAsync();
                    var form = await httpContext.Request.ReadFormAsync();
                    form.TryGetValue("Id", out var rawIds);
                    var ids = rawIds
                        .Select(idString => Guid.TryParse(idString, out var id) ? id : Guid.Empty)
                        .Where(id => id != Guid.Empty)
                        .ToList(); // need to preserve sort order

                    var items = await db.Item
                        .Where(i => ids.Contains(i.Id))
                        .ToListAsync();
                    var i = 0;
                    foreach (var itemId in ids)
                    {
                        var item = items.First(item => item.Id == itemId); // bit roundabout to preserve sort order
                        item.SectionId = sectionId;
                        item.SortOrder = i;
                        i++;
                    }
                    
                    await db.SaveChangesAsync();
                    return Results.Ok();
                });
            
            app.MapPost("/htmx/shopping-list/{shoppingListId:Guid}/section",
                async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var form = await httpContext.Request.ReadFormAsync();
                var name = form.TryGetValue("Section.Name", out var rawName) ? rawName.ToString() : "";
                
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
                    Mode = "edit",
                    Stage = GetStage(httpContext),
                    Items = new List<Item>()
                });
            });
            
            app.MapDelete("/htmx/shopping-list/{shoppingListId:Guid}/section/{sectionId:Guid}",
                async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, Guid sectionId) =>
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
        
            app.MapPatch("/htmx/shopping-list/{shoppingListId:Guid}/section/{sectionId:Guid}",
                async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId, Guid sectionId) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var section = await db.Section.FindAsync(sectionId);
                if (section is null || section.ShoppingListId != shoppingListId)
                {
                    return Results.NotFound();
                }

                var form = await httpContext.Request.ReadFormAsync();
                if (form.TryGetValue("Section.Name", out var rawName))
                {
                    section.Name = rawName.ToString();
                }
                await db.SaveChangesAsync();

                return new RazorComponentResult<SectionComponent>(new
                {
                    ShoppingListId = shoppingListId,
                    Section = section,
                    Mode = "default",
                    Stage = GetStage(httpContext),
                    Items = new List<Item>()
                });
            });
            
            return app;
        }
        
        [UsedImplicitly]
        public WebApplication MapItemEndpoints()
        {
            app.MapPost("/htmx/shopping-list/{shoppingListId:Guid}/item", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var form = await httpContext.Request.ReadFormAsync();
                var name = form.TryGetValue("Name", out var rawName) ? rawName.ToString() : "";
                var sectionId = Guid.TryParse(form["SectionId"], out var parsedSectionId)
                    ? parsedSectionId
                    : (Guid?)null;
                
                var item = new Item
                {
                    ShoppingListId = shoppingListId,
                    SectionId = sectionId,
                    Name = name
                };
    
                db.Item.Add(item);
                await db.SaveChangesAsync();
                return new RazorComponentResult<ItemCardComponent>(new
                {
                    ShoppingListId = shoppingListId,
                    Item = item,
                    Mode = "edit",
                    Stage = GetStage(httpContext)
                });
            });
            
            app.MapDelete("/htmx/shopping-list/{shoppingListId:Guid}/item/{itemId:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, Guid itemId) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var item = await db.Item.FindAsync(itemId);
                if (item is null || item.ShoppingListId != shoppingListId)
                {
                    return Results.NotFound();
                }

                db.Item.Remove(item);
                await db.SaveChangesAsync();
    
                // one could argue for 204 NoContent instead of 200 Ok
                // however, this is primarily for HTMX and HTMX ignores 204 NoContent
                return Results.Ok(); 
            });
            
            app.MapPatch("/htmx/shopping-list/{shoppingListId:Guid}/item/{itemId:Guid}", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext, Guid shoppingListId, Guid itemId) =>
            {
                var db = await dbFactory.CreateDbContextAsync();
                var item = await db.Item.FindAsync(itemId);
                if (item is null || item.ShoppingListId != shoppingListId)
                {
                    return Results.NotFound();
                }

                var form = await httpContext.Request.ReadFormAsync();
                if (form.TryGetValue("Name", out var rawName))
                {
                    item.Name = rawName.ToString();
                }
                if (form.TryGetValue("CountDelta", out var rawCount) && int.TryParse(rawCount, out var countDelta))
                {
                    item.Count += countDelta;
                    if (item.Count < 0)
                    {
                        item.Count = 0;
                    }
                }
                if (form.TryGetValue("IsCollected", out var rawIsCollected) && bool.TryParse(rawIsCollected, out var isCollected))
                {
                    item.IsCollected = isCollected;
                }
                await db.SaveChangesAsync();

                return new RazorComponentResult<ItemCardComponent>(new
                {
                    ShoppingListId = shoppingListId,
                    Item = item,
                    Stage = GetStage(httpContext)
                });
            });
        
            return app;
        }
    }
}
