using StorePos.Services;
namespace StorePos.UI;

public class MainMenu
{
    private readonly InventoryService _inv;
    private readonly SalesScreen _sales;
    private readonly StockScreen _stock;
    private readonly ProductScreen _products;
    private readonly OwnerScreen _owners;
    private readonly ReportScreen _reports;

    public MainMenu(InventoryService inv, ReportService reports)
    {
        _inv = inv;
        var finder = new ProductFinder(inv);
        _sales = new SalesScreen(inv, finder);
        _stock = new StockScreen(inv, finder);
        _products = new ProductScreen(inv, finder);
        _owners = new OwnerScreen(inv);
        _reports = new ReportScreen(inv, reports);
    }

    public void Run()
    {
        if (_inv.Owners.Count == 0) _owners.FirstSetup();

        string[] options =
        {
            "Registrar venta",
            "Recibir mercancía",
            "Ver inventario",
            "Comprar inventario a otro dueño",
            "Productos",
            "Ver ganancias",
            "Historial por día",
            "Dueños",
            "Salir"
        };

        while (true)
        {
            switch (ConsoleIO.Select("=== PUNTO DE VENTA ===", options))
            {
                case 0: _sales.Run(); break;
                case 1: _stock.Receive(); break;
                case 2: _stock.ShowInventory(); break;
                case 3: _stock.Transfer(); break;
                case 4: _products.Run(); break;
                case 5: _reports.Profit(); break;
                case 6: _reports.History(); break;
                case 7: _owners.Run(); break;
                default: return;   // "Salir" o Esc
            }
        }
    }
}