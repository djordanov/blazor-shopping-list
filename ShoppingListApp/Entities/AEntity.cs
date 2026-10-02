namespace ShoppingListApp.Entities;

public abstract class AEntity
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}