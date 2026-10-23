using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Data;
// ReSharper disable RedundantAnonymousTypePropertyName

namespace ShoppingListApp.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        [UsedImplicitly]
        public WebApplication MapShoppingListEndpoints()
        {
            app.MapPost("/api/shopping-list/{shoppingListId:Guid}/sort",
                // ReSharper disable once UnusedParameter.Local
                async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, [FromBody] List<Guid> ids) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var sections = await db.Section
                    .Where(section => ids.Contains(section.Id))
                    .ToListAsync();
                var i = 0;
                foreach (var sectionId in ids)
                {
                    var section = sections.First(section => section.Id == sectionId); // bit roundabout to preserve sort order
                    section.SortOrder = i;
                    i++;
                }
                
                await db.SaveChangesAsync();
                return Results.Ok();
            });
            
            return app;
        }

        [UsedImplicitly]
        public WebApplication MapSectionEndpoints()
        {
            app.MapPost("/api/shopping-list/{shoppingListId:Guid}/section/{sectionId:Guid}/sort",
                // ReSharper disable once UnusedParameter.Local
                async (IDbContextFactory<AppDbContext> dbFactory, Guid shoppingListId, Guid sectionId, [FromBody] List<Guid> ids) =>
                {
                    await using var db = await dbFactory.CreateDbContextAsync();
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
            
            return app;
        }
    }
}
