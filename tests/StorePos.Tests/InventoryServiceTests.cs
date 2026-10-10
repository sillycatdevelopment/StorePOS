using StorePos.Data;
using StorePos.Models;
using StorePos.Services;

namespace StorePos.Tests;

public class InMemoryRepository : IInventoryRepository
{
    public InventoryData Load() => new();
    public void Save(InventoryData data) { }
}

public class InventoryServiceTests
{
    private static InventoryService NewService() =>
        new(new InMemoryRepository(), new InventoryData());

    [Fact]
    public void WeighedSale_500g_At100PerKg_Charges50()
    {
        var inv = NewService();
        var mom = inv.AddOwner("Mamá");
        var tomato = inv.AddProduct("Jitomate", UnitType.Kilogram, 100m);
        inv.AddOpeningStock(tomato.Id, mom.Id, 2m, 60m);

        decimal total = inv.Sell(tomato.Id, 0.5m);

        Assert.Equal(50m, total);
        Assert.Equal(1.5m, inv.GetStock(tomato.Id));
    }

    [Fact]
    public void NoPriority_SaleIsSplitProportionally()
    {
        var inv = NewService();
        var mom = inv.AddOwner("Mamá");
        var me = inv.AddOwner("Yo");
        var coca = inv.AddProduct("Coca 600ml", UnitType.Piece, 18m, "7501055300000");
        inv.AddOpeningStock(coca.Id, mom.Id, 24, 12m);
        inv.Restock(coca.Id, me.Id, 12, 13m);

        inv.Sell(coca.Id, 6);

        Assert.Equal(20, inv.GetStock(coca.Id, mom.Id));
        Assert.Equal(10, inv.GetStock(coca.Id, me.Id));
    }

    [Fact]
    public void PriorityOwner_SellsFirst()
    {
        var inv = NewService();
        var mom = inv.AddOwner("Mamá");
        var me = inv.AddOwner("Yo");
        var coca = inv.AddProduct("Coca 600ml", UnitType.Piece, 18m, priorityOwnerId: me.Id);
        inv.AddOpeningStock(coca.Id, mom.Id, 24, 12m);
        inv.Restock(coca.Id, me.Id, 12, 13m);

        inv.Sell(coca.Id, 10);

        Assert.Equal(2, inv.GetStock(coca.Id, me.Id));
        Assert.Equal(24, inv.GetStock(coca.Id, mom.Id));
    }

    [Fact]
    public void Sell_MoreThanStock_Throws()
    {
        var inv = NewService();
        var mom = inv.AddOwner("Mamá");
        var p = inv.AddProduct("Galletas", UnitType.Piece, 10m);
        inv.AddOpeningStock(p.Id, mom.Id, 3, 6m);

        Assert.Throws<InvalidOperationException>(() => inv.Sell(p.Id, 4));
    }
}