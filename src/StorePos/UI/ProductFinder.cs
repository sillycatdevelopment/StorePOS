using StorePos.Models;
using StorePos.Services;
namespace StorePos.UI;

public class ProductFinder
{
    private readonly InventoryService _inv;
    public ProductFinder(InventoryService inv) => _inv = inv;

    /// <summary>
    /// Un solo campo: escanea el código o escribe el nombre.
    /// Solo dígitos = código de barras. Enter vacío = lista completa
    /// (o null si emptyReturnsNull, usado en el carrito para "cobrar").
    /// </summary>
    public Product? FindOrPick(string header, bool emptyReturnsNull = false)
    {
        Console.Clear();
        Console.WriteLine(header);
        Console.WriteLine();
        Console.Write(emptyReturnsNull
            ? "Escanea o escribe el nombre (Enter vacío = terminar): "
            : "Escanea o escribe el nombre (Enter vacío = ver lista): ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return emptyReturnsNull ? null : PickFromList(header, _inv.Products);

        if (input.All(char.IsDigit))
        {
            var byCode = _inv.FindByBarcode(input);
            if (byCode is not null) return byCode;

            string[] options =
            {
                "Crear un producto nuevo con este código",
                "Asignar este código a un producto existente",
                "Cancelar"
            };
            return ConsoleIO.Select($"El código {input} no está registrado", options) switch
            {
                0 => CreateProduct(barcode: input),
                1 => AssignToExisting(input),
                _ => null
            };
        }

        var matches = _inv.SearchByName(input);
        if (matches.Count == 1) return matches[0];
        if (matches.Count > 1) return PickFromList($"{header}\nResultados para \"{input}\"", matches);

        return ConsoleIO.Select($"No encontré \"{input}\"",
            new[] { "Crear un producto con este nombre", "Cancelar" }) == 0
            ? CreateProduct(name: input)
            : null;
    }

    public Product? PickFromList(string title, IReadOnlyList<Product> products)
    {
        if (products.Count == 0)
        {
            Console.Clear();
            Console.WriteLine("Primero agrega un producto.");
            ConsoleIO.Pause();
            return null;
        }

        var labels = products
            .Select(p => $"{p.Name,-28} {p.Price,9:C}/{p.UnitName(),-6} Stock: {p.Format(_inv.GetStock(p.Id))}")
            .ToList();

        int index = ConsoleIO.Select(title, labels);
        return index < 0 ? null : products[index];
    }

    public Product? CreateProduct(string? barcode = null, string? name = null)
    {
        Console.Clear();
        Console.WriteLine("=== NUEVO PRODUCTO ===\n");
        if (barcode is not null) Console.WriteLine($"Código: {barcode}");

        name ??= ConsoleIO.ReadText("Nombre (ej. Coca-Cola 600ml): ");
        if (name is null) return null;
        Console.WriteLine($"Nombre: {name}\n");

        int unitChoice = ConsoleIO.Select($"¿Cómo se vende {name}?",
            new[] { "Por pieza", "Por kilo (se pesa en la báscula)" });
        if (unitChoice < 0) return null;
        var unit = unitChoice == 0 ? UnitType.Piece : UnitType.Kilogram;

        Console.Clear();
        Console.WriteLine($"=== {name} ===\n");
        decimal? price = ConsoleIO.ReadMoney($"Precio de venta por {(unit == UnitType.Piece ? "pieza" : "kilo")}: ");
        if (price is null) return null;

        int? priorityId = null;
        if (_inv.Owners.Count > 0)
        {
            var options = new List<string> { "Ninguno (se reparte proporcional)" };
            options.AddRange(_inv.Owners.Select(o => o.Name));
            int pick = ConsoleIO.Select($"¿Qué dueño vende primero {name}?", options);
            if (pick > 0) priorityId = _inv.Owners[pick - 1].Id;
        }

        try
        {
            return _inv.AddProduct(name, unit, price.Value, barcode, priorityId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            ConsoleIO.Pause();
            return null;
        }
    }

    private Product? AssignToExisting(string barcode)
    {
        var product = PickFromList("¿A qué producto pertenece este código?", _inv.Products);
        if (product is null) return null;

        try
        {
            _inv.AssignBarcode(product.Id, barcode);
            return product;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            ConsoleIO.Pause();
            return null;
        }
    }
}