using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class Section
{
    public Guid Id { get; init; }
    [StringLength(64)] public required string Name { get; set; }
    public required ShoppingList ShoppingList { get; set; }
    public Guid ShoppingListId { get; set; }
    public List<ListItem> ListItems { get; set; } = []; 
}