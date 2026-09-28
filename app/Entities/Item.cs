using System.ComponentModel.DataAnnotations;

public class Item
{
    public Guid Id { get; init; }
    [StringLength(64)] public string Name { get; set; }
}