using System.Globalization;
using StorePos.Models;
using StorePos.Services;
namespace StorePos.UI;

public class ReportScreen
{
    private readonly InventoryService _inv;
    private readonly ReportService _reports;

    public ReportScreen(InventoryService inv, ReportService reports)
    {
        _inv = inv;
        _reports = reports;
    }

    public void Profit()
    {
        var today = DateTime.Today;
        string[] periods = { "Hoy", "Esta semana", "Este mes", "Todo el historial", "Volver" };

        int choice = ConsoleIO.Select("GANANCIAS - ¿De qué periodo?", periods);
        if (choice is < 0 or 4) return;

        DateTime from = choice switch
        {
            0 => today,
            1 => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),   // lunes
            2 => new DateTime(today.Year, today.Month, 1),
            _ => DateTime.MinValue
        };

        Console.Clear();
        Console.WriteLine($"=== {periods[choice].ToUpper()} ===\n");
        Console.WriteLine($"{"Dueño",-14} {"Ingresos",12} {"Costo",12} {"Ganancia",12}");

        foreach (var o in _inv.Owners)
        {
            var r = _reports.GetProfit(from, DateTime.Now, o.Id);
            Console.WriteLine($"{o.Name,-14} {r.Revenue,12:C} {r.Cost,12:C} {r.Profit,12:C}");
        }

        var total = _reports.GetProfit(from, DateTime.Now);
        Console.WriteLine(new string('-', 53));
        Console.WriteLine($"{"Total",-14} {total.Revenue,12:C} {total.Cost,12:C} {total.Profit,12:C}");
        ConsoleIO.Pause();
    }

    public void History()
    {
        string[] options = { "Últimos 7 días", "Este mes", "Ver un día específico", "Volver" };
        int choice = ConsoleIO.Select("HISTORIAL - ¿Qué quieres ver?", options);
        if (choice is < 0 or 3) return;

        if (choice == 2) { ShowDay(); return; }

        var today = DateTime.Today;
        DateTime from = choice == 0 ? today.AddDays(-6) : new DateTime(today.Year, today.Month, 1);

        Console.Clear();
        Console.WriteLine($"=== {options[choice].ToUpper()} ===\n");
        Console.WriteLine($"{"Fecha",-12} {"Ingresos",12} {"Ganancia",12}");

        var days = _reports.GetDailySummaries(from, DateTime.Now).ToList();
        if (days.Count == 0) Console.WriteLine("No hay ventas en este periodo.");

        foreach (var d in days)
            Console.WriteLine($"{d.Day:dd/MM/yyyy} {d.Revenue,12:C} {d.Profit,12:C}");

        if (days.Count > 0)
        {
            Console.WriteLine(new string('-', 38));
            Console.WriteLine($"{"Total",-12} {days.Sum(d => d.Revenue),12:C} {days.Sum(d => d.Profit),12:C}");
        }
        ConsoleIO.Pause();
    }

    private void ShowDay()
    {
        Console.Clear();
        string? text = ConsoleIO.ReadText("Fecha (dd/mm/aaaa, Enter para hoy): ");

        DateTime day = DateTime.Today;
        if (text is not null && !DateTime.TryParseExact(text, new[] { "d/M/yyyy", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
        {
            Console.WriteLine("\nFecha no válida.");
            ConsoleIO.Pause();
            return;
        }

        Console.Clear();
        Console.WriteLine($"=== MOVIMIENTOS DEL {day:dd/MM/yyyy} ===\n");

        var movements = _reports.GetMovementsOn(day).ToList();
        if (movements.Count == 0) Console.WriteLine("No hay movimientos ese día.");

        foreach (var m in movements)
        {
            var product = _inv.Products.FirstOrDefault(p => p.Id == m.ProductId);
            string owner = _inv.Owners.FirstOrDefault(o => o.Id == m.OwnerId)?.Name ?? "?";
            string qty = product?.Format(m.Quantity) ?? m.Quantity.ToString("0.###");
            string type = m.Type switch
            {
                MovementType.Sale => "Venta",
                MovementType.Restock => "Compra",
                MovementType.Opening => "Inicial",
                MovementType.TransferIn => "Traspaso +",
                MovementType.TransferOut => "Traspaso -",
                _ => "Ajuste"
            };
            Console.WriteLine($"{m.Date:HH:mm}  {type,-10} {qty,10}  {product?.Name ?? "(borrado)",-24} [{owner}]");
        }
        ConsoleIO.Pause();
    }
}