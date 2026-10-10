using System.Globalization;
using StorePos.Models;
namespace StorePos.UI;

public static class ConsoleIO
{
    /// <summary>Menú con flechas. Devuelve el índice elegido, o -1 con Esc.</summary>
    public static int Select(string title, IReadOnlyList<string> options)
    {
        if (options.Count == 0) return -1;
        int index = 0;

        while (true)
        {
            Console.Clear();
            Console.WriteLine(title);
            Console.WriteLine("(↑/↓ para moverte, Enter para elegir, Esc para volver)\n");

            // Ventana con scroll para listas largas
            int page = Math.Max(5, Console.WindowHeight - 8);
            int start = Math.Clamp(index - page / 2, 0, Math.Max(0, options.Count - page));
            int end = Math.Min(options.Count, start + page);

            if (start > 0) Console.WriteLine("   ...");
            for (int i = start; i < end; i++)
            {
                if (i == index)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($" > {options[i]} ");
                    Console.ResetColor();
                }
                else Console.WriteLine($"   {options[i]}");
            }
            if (end < options.Count) Console.WriteLine("   ...");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.UpArrow: index = (index - 1 + options.Count) % options.Count; break;
                case ConsoleKey.DownArrow: index = (index + 1) % options.Count; break;
                case ConsoleKey.Enter: return index;
                case ConsoleKey.Escape: return -1;
            }
        }
    }

    public static Owner? PickOwner(string title, IReadOnlyList<Owner> owners, Func<Owner, string>? label = null)
    {
        if (owners.Count == 0)
        {
            Console.Clear();
            Console.WriteLine("No hay dueños disponibles.");
            Pause();
            return null;
        }
        int i = Select(title, owners.Select(o => label?.Invoke(o) ?? o.Name).ToList());
        return i < 0 ? null : owners[i];
    }

    public static string? ReadText(string prompt)
    {
        Console.Write(prompt);
        string? text = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public static decimal? ReadMoney(string prompt)
    {
        while (true)
        {
            string? text = ReadText(prompt + "(vacío = cancelar) ");
            if (text is null) return null;
            if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out decimal value) && value >= 0)
                return value;
            Console.WriteLine("Escribe un número válido.");
        }
    }

    /// <summary>
    /// Piezas: Enter = 1, o un número entero.
    /// Peso: lo que marca la báscula. "500" = 500 g, "1.5kg" = 1.5 kilos.
    /// Devuelve null si se cancela (0).
    /// </summary>
    public static decimal? ReadQuantity(Product product)
    {
        while (true)
        {
            if (product.Unit == UnitType.Piece)
            {
                Console.Write("Cantidad (Enter = 1, 0 = cancelar): ");
                string? text = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(text)) return 1;
                if (int.TryParse(text, out int n))
                {
                    if (n == 0) return null;
                    if (n > 0) return n;
                }
                Console.WriteLine("Escribe un número entero.\n");
            }
            else
            {
                Console.Write("Peso (500 = 500 g, 1.5kg = 1.5 kilos, 0 = cancelar): ");
                string? text = Console.ReadLine();
                if (TryParseWeight(text ?? "", out decimal kg))
                {
                    if (kg == 0) return null;
                    return kg;
                }
                Console.WriteLine("Peso no válido.\n");
            }
        }
    }

    public static bool TryParseWeight(string text, out decimal kilos)
    {
        kilos = 0;
        text = text.Trim().ToLowerInvariant().Replace(',', '.');
        bool isKg = text.EndsWith("kg");

        if (isKg) text = text[..^2];
        else if (text.EndsWith("g")) text = text[..^1];

        if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal v) || v < 0)
            return false;

        kilos = isKg ? v : v / 1000m;   // sin unidad = gramos
        return true;
    }

    public static void Pause()
    {
        Console.WriteLine("\nPresiona cualquier tecla para continuar...");
        Console.ReadKey(true);
    }
}