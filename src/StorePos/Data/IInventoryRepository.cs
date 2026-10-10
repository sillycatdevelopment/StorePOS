namespace StorePos.Data;

public interface IInventoryRepository
{
    InventoryData Load();
    void Save(InventoryData data);
}