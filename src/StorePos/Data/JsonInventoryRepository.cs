using System.Text.Json;
using System.Text.Json.Serialization;
namespace StorePos.Data;

public class JsonInventoryRepository : IInventoryRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    public JsonInventoryRepository(string path) => _path = path;

    public InventoryData Load() =>
        File.Exists(_path)
            ? JsonSerializer.Deserialize<InventoryData>(File.ReadAllText(_path), Options) ?? new()
            : new();

    public void Save(InventoryData data)
    {
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, _path, overwrite: true);   // never leaves a half-written file
    }
}