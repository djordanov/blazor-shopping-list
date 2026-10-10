using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Entities;

namespace ShoppingListApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingList> ShoppingList { get; set; }
    public DbSet<Section> Section { get; set; }
    public DbSet<Item> Item { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity
                .HasOne(i => i.ShoppingList)
                .WithMany(sl => sl.ListItems)
                .HasForeignKey(i => i.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(i => i.Section)
                .WithMany(s => s.ListItems)
                .HasForeignKey(i => i.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingList>()
            .HasMany(sl => sl.Sections)
            .WithOne(s => s.ShoppingList)
            .HasForeignKey(s => s.ShoppingListId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var now = DateTimeOffset.UtcNow;
        
        foreach (var entityEntry in ChangeTracker.Entries<AEntity>())
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                    entityEntry.Entity.CreatedAt = now;
                    entityEntry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entityEntry.Entity.UpdatedAt = now;
                    break;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}