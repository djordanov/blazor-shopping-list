using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class Item : AEntity
{
    public ShoppingList? ShoppingList { get; set; }
    public Guid ShoppingListId { get; set; }
    public Section? Section { get; set; }
    public Guid? SectionId { get; set; }
    [MaxLength(256)] public required string Name { get; set; }
    public int Count { get; set; } = 1;
    public bool IsCollected { get; set; }
}