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
                var name = form.TryGetValue("Section.Name", out var rawName) ? rawName.ToString() : "";
                var sectionId = Guid.TryParse(form["sectionId"], out var parsedSectionId)
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
                    SectionId = sectionId,
                    Item = item
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
                if (form.TryGetValue("Item.Name", out var rawName))
                {
                    item.Name = rawName.ToString();
                }
                if (form.TryGetValue("Item.Count", out var rawCount) && int.TryParse(rawCount, out var count))
                {
                    item.Count = count;
                }
                if (form.TryGetValue("Item.IsCollected", out var rawIsCollected) && bool.TryParse(rawIsCollected, out var isCollected))
                {
                    item.IsCollected = isCollected;
                }
                await db.SaveChangesAsync();

                return Results.Ok();
            });
        
            return app;
        }
    }
}
