using System.Globalization;
using System.Text;
using StorePos.Data;
using StorePos.Services;
using StorePos.UI;

Console.OutputEncoding = Encoding.UTF8;                    // ñ, á y flechas
CultureInfo.CurrentCulture = new CultureInfo("es-MX");     // $ y formato de números

string folder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StorePos");
Directory.CreateDirectory(folder);

var repo = new JsonInventoryRepository(Path.Combine(folder, "inventory.json"));
var data = repo.Load();

var inventory = new InventoryService(repo, data);
var reports = new ReportService(data);

new MainMenu(inventory, reports).Run();