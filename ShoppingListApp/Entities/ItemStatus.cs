using System.Text.Json.Serialization;

namespace ShoppingListApp.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]                                                                                                                                                                         
public enum ItemStatus
{
    Noted,
    Planned,
    Collected
}