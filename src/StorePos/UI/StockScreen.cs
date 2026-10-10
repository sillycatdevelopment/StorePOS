using StorePos.Models;
using StorePos.Services;
namespace StorePos.UI;

public class StockScreen
{
    private readonly InventoryService _inv;
    private readonly ProductFinder _finder;

    public StockScreen(InventoryService inv, ProductFinder finder)
    {
        _inv = inv;
        _finder = finder;
    }

    public void Receive()
    {
        var product = _finder.FindOrPick("=== RECIBIR MERCANCÍA ===");
        if (product is null) return;

        var owner = ConsoleIO.PickOwner($"{product.Name}\n¿De quién es esta mercancía (quién la pagó)?", _inv.Owners);
        if (owner is null) return;

        int kind = ConsoleIO.Select($"{product.Name} - {owner.Name}\nTipo de entrada",
            new[] { "Compra nueva (la acabo de comprar)", "Inventario que ya existía en el estante" });
        if (kind < 0) return;

        Console.Clear();
        Console.WriteLine($"=== {product.Name} - {owner.Name} ===\n");

        decimal? qty = ConsoleIO.ReadQuantity(product);
        if (qty is null) return;

        decimal? cost = ConsoleIO.ReadMoney($"Costo por {product.UnitName()}: ");
        if (cost is null) return;

        try
        {
            if (kind == 0) _inv.Restock(product.Id, owner.Id, qty.Value, cost.Value);
            else _inv.AddOpeningStock(product.Id, owner.Id, qty.Value, cost.Value);

            Console.WriteLine($"\nListo. Ahora hay {product.Format(_inv.GetStock(product.Id))} en total " +
                              $"({owner.Name}: {product.Format(_inv.GetStock(product.Id, owner.Id))}).");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        ConsoleIO.Pause();
    }

    /// <summary>Un dueño le compra inventario existente a otro (así vas quedándote con el producto).</summary>
    public void Transfer()
    {
        var product = _finder.FindOrPick("=== COMPRAR INVENTARIO A OTRO DUEÑO ===");
        if (product is null) return;

        var sellers = _inv.Owners.Where(o => _inv.GetStock(product.Id, o.Id) > 0).ToList();
        var from = ConsoleIO.PickOwner($"{product.Name}\n¿Quién vende su inventario?", sellers,
            o => $"{o.Name} ({product.Format(_inv.GetStock(product.Id, o.Id))})");
        if (from is null) return;

        var buyers = _inv.Owners.Where(o => o.Id != from.Id).ToList();
        var to = ConsoleIO.PickOwner($"{product.Name}\n¿Quién compra?", buyers);
        if (to is null) return;

        Console.Clear();
        Console.WriteLine($"=== {from.Name} → {to.Name}: {product.Name} ===");
        Console.WriteLine($"Disponible de {from.Name}: {product.Format(_inv.GetStock(product.Id, from.Id))}\n");

        decimal? qty = ConsoleIO.ReadQuantity(product);
        if (qty is null) return;

        decimal avg = _inv.AverageCost(product.Id, from.Id);
        decimal? price = ConsoleIO.ReadMoney($"Precio acordado por {product.UnitName()} (costo promedio de {from.Name}: {avg:C}): ");
        if (price is null) return;

        try
        {
            _inv.Transfer(product.Id, from.Id, to.Id, qty.Value, price.Value);
            Console.WriteLine($"\nListo. {to.Name} ahora tiene {product.Format(_inv.GetStock(product.Id, to.Id))}.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        ConsoleIO.Pause();
    }

    public void ShowInventory()
    {
        Console.Clear();
        Console.WriteLine("=== INVENTARIO ===\n");

        if (_inv.Products.Count == 0) Console.WriteLine("Todavía no hay productos.");

        foreach (var p in _inv.Products)
        {
            var shares = _inv.Owners
                .Select(o => (o.Name, Qty: _inv.GetStock(p.Id, o.Id)))
                .Where(x => x.Qty != 0)
                .Select(x => $"{x.Name} {p.Format(x.Qty)}");

            Console.WriteLine($"{p.Name,-26} {p.Barcode ?? "-",-14} {p.Format(_inv.GetStock(p.Id)),10}   ({string.Join(", ", shares)})");
        }
        ConsoleIO.Pause();
    }
}