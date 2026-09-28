using ShoppingListApp.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingList> ShoppingList { get; set; } = default!;
    public DbSet<Section> Section { get; set; } = default!;
    public DbSet<Item> Item { get; set; } = default!;
    public DbSet<ListItem> ListItem { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}