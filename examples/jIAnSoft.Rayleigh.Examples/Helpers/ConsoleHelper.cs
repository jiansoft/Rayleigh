namespace Rayleigh.Examples.Helpers;

internal static class ConsoleHelper
{
    internal static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{"".PadRight(60, '=')}");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"{"".PadRight(60, '=')}");
        Console.ResetColor();
    }

    internal static void PrintSubSection(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n--- {title} ---");
        Console.ResetColor();
    }

    internal static void PrintResult(string label, object? value)
    {
        Console.WriteLine($"  {label} => {value}");
    }

    internal static void PrintCorrect(string description)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  [OK]   ");
        Console.ResetColor();
        Console.WriteLine(description);
    }

    internal static void PrintWrong(string description)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  [BAD]  ");
        Console.ResetColor();
        Console.WriteLine(description);
    }

    internal static void CatchAndPrint(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("  [BOOM] ");
            Console.ResetColor();
            Console.WriteLine($"{label} -> {ex.GetType().Name}: {ex.Message}");
        }
    }
}
