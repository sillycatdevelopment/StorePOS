using StorePos.Data;
using StorePos.Models;
namespace StorePos.Services;

public record ProfitReport(decimal Revenue, decimal Cost)
{
    public decimal Profit => Revenue - Cost;
}

public record DailySummary(DateTime Day, decimal Revenue, decimal Cost)
{
    public decimal Profit => Revenue - Cost;
}

public class ReportService
{
    private readonly InventoryData _data;
    public ReportService(InventoryData data) => _data = data;

    private IEnumerable<StockMovement> Sales(DateTime from, DateTime to, int? ownerId = null) =>
        _data.Movements.Where(m => m.Type == MovementType.Sale
                                   && m.Date >= from && m.Date <= to
                                   && (ownerId is null || m.OwnerId == ownerId));

    public ProfitReport GetProfit(DateTime from, DateTime to, int? ownerId = null)
    {
        var sales = Sales(from, to, ownerId).ToList();
        return new ProfitReport(
            Math.Round(sales.Sum(m => m.UnitPrice * m.Quantity), 2),
            Math.Round(sales.Sum(m => m.UnitCost * m.Quantity), 2));
    }

    public IEnumerable<DailySummary> GetDailySummaries(DateTime from, DateTime to) =>
        Sales(from, to)
            .GroupBy(m => m.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySummary(
                g.Key,
                Math.Round(g.Sum(m => m.UnitPrice * m.Quantity), 2),
                Math.Round(g.Sum(m => m.UnitCost * m.Quantity), 2)));

    public IEnumerable<StockMovement> GetMovementsOn(DateTime day) =>
        _data.Movements.Where(m => m.Date.Date == day.Date).OrderBy(m => m.Date);
}