namespace ShoppingListApp.Entities;

public class ListItem
{
    public Guid Id { get; init; }
    public ShoppingList? ShoppingList { get; set; }
    public Guid ShoppingListId { get; set; }
    public Section? Section { get; set; }
    public Guid? SectionId { get; set; }
    public Item? Item { get; set; }
    public Guid ItemId { get; set; }
    public bool IsPurchased { get; set; } // TODO this attribute doesn't make a lot of sense
}