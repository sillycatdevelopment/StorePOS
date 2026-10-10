using StorePos.Services;
namespace StorePos.UI;

public class ProductScreen
{
    private readonly InventoryService _inv;
    private readonly ProductFinder _finder;

    public ProductScreen(InventoryService inv, ProductFinder finder)
    {
        _inv = inv;
        _finder = finder;
    }

    public void Run()
    {
        string[] options =
        {
            "Agregar producto nuevo",
            "Cambiar precio",
            "Asignar código de barras",
            "Cambiar dueño prioritario",
            "Volver"
        };

        while (true)
        {
            switch (ConsoleIO.Select("=== PRODUCTOS ===", options))
            {
                case 0: Add(); break;
                case 1: ChangePrice(); break;
                case 2: AssignBarcode(); break;
                case 3: ChangePriority(); break;
                default: return;
            }
        }
    }

    private void Add()
    {
        Console.Clear();
        Console.WriteLine("=== AGREGAR PRODUCTO ===\n");
        string? code = ConsoleIO.ReadText("Escanea el código de barras (Enter para omitir): ");

        if (_finder.CreateProduct(barcode: code) is not null)
        {
            Console.WriteLine("\nProducto agregado.");
            ConsoleIO.Pause();
        }
    }

    private void ChangePrice()
    {
        var product = _finder.FindOrPick("=== CAMBIAR PRECIO ===");
        if (product is null) return;

        Console.WriteLine($"\n{product.Name}: precio actual {product.Price:C} por {product.UnitName()}");
        decimal? price = ConsoleIO.ReadMoney("Nuevo precio: ");
        if (price is null) return;

        _inv.SetPrice(product.Id, price.Value);
        Console.WriteLine("\nPrecio actualizado. Las ventas anteriores conservan su precio.");
        ConsoleIO.Pause();
    }

    private void AssignBarcode()
    {
        var product = _finder.FindOrPick("=== ASIGNAR CÓDIGO DE BARRAS ===");
        if (product is null) return;

        Console.WriteLine();
        string? code = ConsoleIO.ReadText($"Escanea el código para {product.Name}: ");
        if (code is null) return;

        try
        {
            _inv.AssignBarcode(product.Id, code);
            Console.WriteLine("\nCódigo asignado.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        ConsoleIO.Pause();
    }

    private void ChangePriority()
    {
        var product = _finder.FindOrPick("=== DUEÑO PRIORITARIO ===");
        if (product is null) return;

        var options = new List<string> { "Ninguno (se reparte proporcional)" };
        options.AddRange(_inv.Owners.Select(o => o.Name));

        int pick = ConsoleIO.Select($"¿Qué dueño vende primero {product.Name}?", options);
        if (pick < 0) return;

        _inv.SetPriorityOwner(product.Id, pick == 0 ? null : _inv.Owners[pick - 1].Id);
        Console.WriteLine("\nActualizado.");
        ConsoleIO.Pause();
    }
}