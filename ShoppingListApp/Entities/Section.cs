using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class Section
{
    public Guid Id { get; init; }
    [StringLength(64)] public required string Name { get; set; }
    public ShoppingList? ShoppingList { get; set; }
    public required Guid ShoppingListId { get; set; }
    public List<Item> ListItems { get; set; } = []; 
}