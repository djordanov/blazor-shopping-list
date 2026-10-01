using JetBrains.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Components.Partials;
using ShoppingListApp.Data;
using ShoppingListApp.Entities;

namespace ShoppingListApp.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        [UsedImplicitly]
        public WebApplication MapShoppingListEndpoints()
        {
            app.MapPost("/shopping-list/", async (IDbContextFactory<AppDbContext> dbFactory, HttpContext httpContext) =>
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
                return new RazorComponentResult<ShoppingListOption>(new
                {
                    ShoppingList = shoppingList,
                    IsSelected = false
                });
            });
            
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

            return app;
        }

        [UsedImplicitly]
        public WebApplication MapSectionEndpoints()
        {
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
        
            return app;
        }
        
        [UsedImplicitly]
        public WebApplication MapItemEndpoints()
        {
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
        
            return app;
        }
    }
}