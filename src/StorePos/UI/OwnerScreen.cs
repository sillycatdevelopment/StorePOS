using StorePos.Services;
namespace StorePos.UI;

public class OwnerScreen
{
    private readonly InventoryService _inv;
    public OwnerScreen(InventoryService inv) => _inv = inv;

    public void FirstSetup()
    {
        Console.Clear();
        Console.WriteLine("Antes de empezar, registra a los dueños del negocio (ej. Mamá, Yo, Hermano).\n");

        while (true)
        {
            string? name = ConsoleIO.ReadText("Nombre del dueño (Enter para terminar): ");
            if (name is null)
            {
                if (_inv.Owners.Count > 0) return;
                Console.WriteLine("Registra al menos un dueño.");
                continue;
            }
            _inv.AddOwner(name);
        }
    }

    public void Run()
    {
        while (true)
        {
            string title = "=== DUEÑOS ===\n" + string.Join("\n", _inv.Owners.Select(o => $"  - {o.Name}"));
            if (ConsoleIO.Select(title, new[] { "Agregar dueño", "Volver" }) != 0) return;

            Console.Clear();
            string? name = ConsoleIO.ReadText("Nombre del nuevo dueño: ");
            if (name is not null) _inv.AddOwner(name);
        }
    }
}