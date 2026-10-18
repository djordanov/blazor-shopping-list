using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Data;
using ShoppingListApp.Entities;
// ReSharper disable RedundantAnonymousTypePropertyName

namespace ShoppingListApp.Extensions;

public sealed class PatchItemRequest
{
    public int CountDelta { get; set; } = 0;
    public ItemStatus? ItemStatus { get; set; }
}

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
        
        [UsedImplicitly]
        public WebApplication MapItemEndpoints()
        {
            app.MapPatch("/api/shopping-list/{shoppingListId:Guid}/item/{itemId:Guid}", async (IDbContextFactory<AppDbContext> dbFactory,
                Guid shoppingListId,
                Guid itemId,
                [FromBody] PatchItemRequest patchItemRequest) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var item = await db.Item.FindAsync(itemId);
                if (item is null || item.ShoppingListId != shoppingListId)
                {
                    return Results.NotFound();
                }

                item.Count += patchItemRequest.CountDelta;
                if (item.Count < 0)
                {
                    item.Count = 0;
                }
                
                if (patchItemRequest.ItemStatus is not null)
                {
                    item.ItemStatus = (ItemStatus)patchItemRequest.ItemStatus;
                }
                
                await db.SaveChangesAsync();
                return Results.Ok(item);
            });
        
            return app;
        }
    }
}
