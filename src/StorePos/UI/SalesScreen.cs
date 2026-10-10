using StorePos.Models;
using StorePos.Services;
namespace StorePos.UI;

public class SalesScreen
{
    private readonly InventoryService _inv;
    private readonly ProductFinder _finder;

    public SalesScreen(InventoryService inv, ProductFinder finder)
    {
        _inv = inv;
        _finder = finder;
    }

    public void Run()
    {
        var cart = new List<(Product Product, decimal Qty)>();

        while (true)
        {
            string header = BuildHeader(cart);
            var product = _finder.FindOrPick(header, emptyReturnsNull: cart.Count > 0);

            if (product is null)
            {
                if (cart.Count == 0) return;

                int next = ConsoleIO.Select(header + "\n¿Qué sigue?",
                    new[] { "Cobrar", "Seguir agregando", "Cancelar venta" });
                if (next == 0) { Checkout(cart); return; }
                if (next == 2) return;
                continue;
            }

            decimal inCart = cart.Where(l => l.Product.Id == product.Id).Sum(l => l.Qty);
            decimal available = _inv.GetStock(product.Id) - inCart;

            Console.WriteLine($"\n{product.Name} - {product.Price:C} por {product.UnitName()}  (disponible: {product.Format(available)})");
            decimal? qty = ConsoleIO.ReadQuantity(product);
            if (qty is null) continue;

            if (qty > available)
            {
                Console.WriteLine("\nNo hay suficiente inventario.");
                ConsoleIO.Pause();
                continue;
            }

            cart.Add((product, qty.Value));
        }
    }

    private void Checkout(List<(Product Product, decimal Qty)> cart)
    {
        decimal total = 0;
        try
        {
            foreach (var (product, qty) in cart)
                total += _inv.Sell(product.Id, qty);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            ConsoleIO.Pause();
            return;
        }

        Console.Clear();
        Console.WriteLine("=== VENTA REGISTRADA ===\n");
        Console.WriteLine($"Total a cobrar: {total:C}");
        ConsoleIO.Pause();
    }

    private static string BuildHeader(List<(Product Product, decimal Qty)> cart)
    {
        var lines = new List<string> { "=== REGISTRAR VENTA ===" };
        decimal total = 0;

        foreach (var (p, qty) in cart)
        {
            decimal line = Math.Round(p.Price * qty, 2);
            total += line;
            lines.Add($"  {p.Format(qty),-10} {p.Name,-26} {line,10:C}");
        }

        if (cart.Count > 0)
        {
            lines.Add(new string('-', 50));
            lines.Add($"  TOTAL: {total:C}");
        }
        return string.Join("\n", lines);
    }
}