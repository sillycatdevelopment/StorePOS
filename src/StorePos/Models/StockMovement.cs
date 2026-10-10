namespace StorePos.Models;

public class StockMovement
{
    public int ProductId { get; set; }
    public int OwnerId { get; set; }          // whose stock this affects
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }     // pieces, or kilos for weighed products
    public decimal UnitPrice { get; set; }    // sale price at that moment (sales)
    public decimal UnitCost { get; set; }     // cost for that owner
    public DateTime Date { get; set; } = DateTime.Now;
}