using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class ShoppingList
{
    public Guid Id { get; init; }
    [StringLength(64)] public string Name { get; set; }
}