using System.ComponentModel.DataAnnotations;

namespace ShoppingListApp.Entities;

public class Item : AEntity
{
    public ShoppingList? ShoppingList { get; set; }
    public Guid ShoppingListId { get; set; }
    public Section? Section { get; set; }
    public required Guid SectionId { get; set; }
    [MaxLength(256)] public required string Name { get; set; }
    public int Count { get; set; } = 1;
    public ItemStatus ItemStatus { get; set; }
    public int SortOrder { get; set; } = int.MaxValue; // start new items at the bottom

    /// <summary>
    /// This should arguably be on an ItemStatus class, or on a service or a helper class
    /// But none of these exist in this project at this point
    /// At least it's not on the UI layer ¯\_(ツ)_/¯
    /// 
    /// </summary>
    /// <param name="itemStatus"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ItemStatus GetNextItemStatus(ItemStatus itemStatus) => itemStatus switch
    {
        ItemStatus.Noted => ItemStatus.Planned,
        ItemStatus.Planned => ItemStatus.Collected,
        ItemStatus.Collected => ItemStatus.Noted,
        _ => throw new ArgumentOutOfRangeException()
    };
}