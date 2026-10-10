using System.Globalization;
using StorePos.Data;
using StorePos.Models;
namespace StorePos.Services;

public class InventoryService
{
    private readonly IInventoryRepository _repo;
    private readonly InventoryData _data;

    public InventoryService(IInventoryRepository repo, InventoryData data)
    {
        _repo = repo;
        _data = data;
    }

    public IReadOnlyList<Owner> Owners => _data.Owners;
    public IReadOnlyList<Product> Products => _data.Products;

    // ---------- Dueños ----------

    public Owner AddOwner(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre no puede estar vacío.");
        int id = _data.Owners.Count == 0 ? 1 : _data.Owners.Max(o => o.Id) + 1;
        var owner = new Owner { Id = id, Name = name.Trim() };
        _data.Owners.Add(owner);
        _repo.Save(_data);
        return owner;
    }

    // ---------- Productos ----------

    public Product? FindByBarcode(string barcode)
    {
        barcode = barcode.Trim();
        return _data.Products.FirstOrDefault(p => p.Barcode == barcode);
    }

    /// <summary>Búsqueda por nombre sin importar mayúsculas ni acentos.</summary>
    public IReadOnlyList<Product> SearchByName(string term)
    {
        var compare = CultureInfo.CurrentCulture.CompareInfo;
        term = term.Trim();
        return _data.Products
            .Where(p => compare.IndexOf(p.Name, term,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
            .OrderBy(p => p.Name)
            .ToList();
    }

    public Product AddProduct(string name, UnitType unit, decimal price,
                              string? barcode = null, int? priorityOwnerId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre no puede estar vacío.");
        if (price < 0) throw new ArgumentException("El precio no puede ser negativo.");

        barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        if (barcode is not null && FindByBarcode(barcode) is not null)
            throw new InvalidOperationException("Ese código de barras ya está registrado.");

        int id = _data.Products.Count == 0 ? 1 : _data.Products.Max(p => p.Id) + 1;
        var product = new Product
        {
            Id = id, Name = name.Trim(), Unit = unit, Price = price,
            Barcode = barcode, PriorityOwnerId = priorityOwnerId
        };
        _data.Products.Add(product);
        _repo.Save(_data);
        return product;
    }

    public void AssignBarcode(int productId, string barcode)
    {
        barcode = barcode.Trim();
        var existing = FindByBarcode(barcode);
        if (existing is not null && existing.Id != productId)
            throw new InvalidOperationException($"Ese código ya pertenece a {existing.Name}.");

        GetProduct(productId).Barcode = barcode;
        _repo.Save(_data);
    }

    /// <summary>Las ventas pasadas conservan el precio con el que se hicieron.</summary>
    public void SetPrice(int productId, decimal price)
    {
        if (price < 0) throw new ArgumentException("El precio no puede ser negativo.");
        GetProduct(productId).Price = price;
        _repo.Save(_data);
    }

    public void SetPriorityOwner(int productId, int? ownerId)
    {
        GetProduct(productId).PriorityOwnerId = ownerId;
        _repo.Save(_data);
    }

    public Product GetProduct(int productId) => _data.Products.First(p => p.Id == productId);

    // ---------- Inventario ----------

    private static decimal Signed(StockMovement m) =>
        m.Type is MovementType.Sale or MovementType.TransferOut ? -m.Quantity : m.Quantity;

    /// <summary>Inventario total del producto, o solo la parte de un dueño.</summary>
    public decimal GetStock(int productId, int? ownerId = null) =>
        _data.Movements
            .Where(m => m.ProductId == productId && (ownerId is null || m.OwnerId == ownerId))
            .Sum(Signed);

    /// <summary>Costo promedio ponderado de lo que ese dueño ha comprado o recibido.</summary>
    public decimal AverageCost(int productId, int ownerId)
    {
        var inflows = _data.Movements.Where(m =>
            m.ProductId == productId && m.OwnerId == ownerId &&
            m.Type is MovementType.Restock or MovementType.Opening or MovementType.TransferIn).ToList();

        decimal units = inflows.Sum(m => m.Quantity);
        return units == 0 ? 0 : inflows.Sum(m => m.UnitCost * m.Quantity) / units;
    }

    // ---------- Operaciones ----------

    /// <summary>Mercancía que ya existía en el estante (ej. lo que ya tenía mamá).</summary>
    public void AddOpeningStock(int productId, int ownerId, decimal quantity, decimal unitCost) =>
        RecordInflow(productId, ownerId, MovementType.Opening, quantity, unitCost);

    /// <summary>Compra nueva pagada por un dueño, con el costo que él pagó.</summary>
    public void Restock(int productId, int ownerId, decimal quantity, decimal unitCost) =>
        RecordInflow(productId, ownerId, MovementType.Restock, quantity, unitCost);

    private void RecordInflow(int productId, int ownerId, MovementType type, decimal quantity, decimal unitCost)
    {
        var product = GetProduct(productId);
        quantity = Normalize(product, quantity);
        _data.Movements.Add(new StockMovement
        {
            ProductId = productId, OwnerId = ownerId, Type = type,
            Quantity = quantity, UnitCost = unitCost
        });
        _repo.Save(_data);
    }

    /// <summary>Un dueño le compra inventario existente a otro, al precio acordado.</summary>
    public void Transfer(int productId, int fromOwnerId, int toOwnerId, decimal quantity, decimal unitPrice)
    {
        if (fromOwnerId == toOwnerId) throw new ArgumentException("Elige dueños distintos.");
        var product = GetProduct(productId);
        quantity = Normalize(product, quantity);
        if (GetStock(productId, fromOwnerId) < quantity)
            throw new InvalidOperationException("Ese dueño no tiene suficiente inventario.");

        var now = DateTime.Now;
        _data.Movements.Add(new StockMovement
        {
            ProductId = productId, OwnerId = fromOwnerId, Type = MovementType.TransferOut,
            Quantity = quantity, UnitCost = AverageCost(productId, fromOwnerId), Date = now
        });
        _data.Movements.Add(new StockMovement
        {
            ProductId = productId, OwnerId = toOwnerId, Type = MovementType.TransferIn,
            Quantity = quantity, UnitCost = unitPrice, Date = now
        });
        _repo.Save(_data);
    }

    /// <summary>Registra una venta y devuelve el total cobrado (precio x cantidad).</summary>
    public decimal Sell(int productId, decimal quantity)
    {
        var product = GetProduct(productId);
        quantity = Normalize(product, quantity);

        var split = Allocate(product, quantity);   // ownerId -> cantidad que sale de su parte
        var now = DateTime.Now;

        foreach (var (ownerId, qty) in split)
        {
            _data.Movements.Add(new StockMovement
            {
                ProductId = productId, OwnerId = ownerId, Type = MovementType.Sale,
                Quantity = qty, UnitPrice = product.Price,
                UnitCost = AverageCost(productId, ownerId), Date = now
            });
        }
        _repo.Save(_data);
        return Math.Round(product.Price * quantity, 2);
    }

    private static decimal Normalize(Product product, decimal quantity)
    {
        if (product.Unit == UnitType.Piece)
        {
            if (quantity != Math.Floor(quantity))
                throw new ArgumentException("Este producto se vende por pieza (número entero).");
        }
        else
        {
            quantity = Math.Round(quantity, 3);   // precisión de 1 gramo
        }

        if (quantity <= 0) throw new ArgumentException("La cantidad debe ser mayor a cero.");
        return quantity;
    }

    private static long Scale(Product product) => product.Unit == UnitType.Kilogram ? 1000 : 1;

    /// <summary>
    /// LA REGLA: decide de quién sale cada venta.
    /// 1) Primero el dueño prioritario del producto (si tiene).
    /// 2) Lo que falte se reparte proporcionalmente entre el inventario restante de todos.
    /// Trabaja en unidades enteras (piezas o gramos) para no perder centésimas.
    /// </summary>
    private Dictionary<int, decimal> Allocate(Product product, decimal quantity)
    {
        long scale = Scale(product);
        long want = (long)Math.Round(quantity * scale);

        var stock = _data.Owners.ToDictionary(
            o => o.Id, o => (long)Math.Round(GetStock(product.Id, o.Id) * scale));

        if (stock.Values.Where(v => v > 0).Sum() < want)
            throw new InvalidOperationException("No hay suficiente inventario.");

        var taken = new Dictionary<int, long>();
        long remaining = want;

        if (product.PriorityOwnerId is int priorityId && stock.TryGetValue(priorityId, out long mine) && mine > 0)
        {
            long take = Math.Min(remaining, mine);
            taken[priorityId] = take;
            stock[priorityId] -= take;
            remaining -= take;
        }

        if (remaining > 0)
        {
            var balances = stock.Where(kv => kv.Value > 0).ToList();
            long total = balances.Sum(kv => kv.Value);

            var share = balances.ToDictionary(kv => kv.Key, kv => remaining * kv.Value / total);
            long left = remaining - share.Values.Sum();

            // El sobrante por redondeo va a quien tenga mayor residuo
            foreach (var kv in balances
                         .OrderByDescending(kv => remaining * kv.Value % total)
                         .ThenByDescending(kv => kv.Value)
                         .Take((int)left))
                share[kv.Key]++;

            foreach (var (id, units) in share.Where(s => s.Value > 0))
                taken[id] = taken.GetValueOrDefault(id) + units;
        }

        return taken.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value / scale);
    }
}