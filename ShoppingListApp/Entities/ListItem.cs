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
    public int Count { get; set; }
}