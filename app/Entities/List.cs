using System.ComponentModel.DataAnnotations;

public class ShoppingList
{
    public Guid Id { get; init; }
    [StringLength(64)] public string Name { get; set; }
}