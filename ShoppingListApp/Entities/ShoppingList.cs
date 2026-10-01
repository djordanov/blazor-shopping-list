using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class ShoppingList
{
    public Guid Id { get; init; }
    [StringLength(64)] public required string Name { get; set; }
    public List<Section> Sections { get; set; } = [];
    public List<Item> ListItems { get; set; } = []; 
}