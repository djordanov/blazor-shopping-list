using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class Item
{
    public Guid Id { get; init; }
    [StringLength(64)] public required string Name { get; set; }
    public ListItem? ListItem { get; set; }
    public Guid ListItemId { get; set; }
}