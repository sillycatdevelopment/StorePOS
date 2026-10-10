namespace StorePos.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Barcode { get; set; }              // optional
    public UnitType Unit { get; set; } = UnitType.Piece;
    public decimal Price { get; set; }                // per piece or per kg
    public int? PriorityOwnerId { get; set; }         // null = proportional sales

    public string UnitName() => Unit == UnitType.Piece ? "pieza" : "kg";

    public string Format(decimal qty) =>
        Unit == UnitType.Piece ? $"{qty:0} pza" : $"{qty:0.###} kg";
}