using StorePos.Models;
namespace StorePos.Data;

public class InventoryData
{
    public List<Owner> Owners { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<StockMovement> Movements { get; set; } = new();
}