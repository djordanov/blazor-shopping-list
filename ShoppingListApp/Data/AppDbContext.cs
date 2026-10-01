using Microsoft.EntityFrameworkCore;
using ShoppingListApp.Entities;

namespace ShoppingListApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingList> ShoppingList { get; set; } = default!;
    public DbSet<Section> Section { get; set; } = default!;
    public DbSet<Item> Item { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity
                .HasOne(i => i.ShoppingList)
                .WithMany(sl => sl.ListItems)
                .HasForeignKey(i => i.ShoppingListId);
            entity
                .HasOne(i => i.Section)
                .WithMany(s => s.ListItems)
                .HasForeignKey(i => i.SectionId);
        });

        modelBuilder.Entity<ShoppingList>()
            .HasMany(sl => sl.Sections)
            .WithOne(s => s.ShoppingList)
            .HasForeignKey(s => s.ShoppingListId);
    }
}