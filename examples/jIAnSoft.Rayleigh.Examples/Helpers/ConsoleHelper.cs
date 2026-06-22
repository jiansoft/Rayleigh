namespace jIAnSoft.Rayleigh.Examples.Helpers;

/// <summary>
/// Provides small console-formatting helpers used by the example walkthroughs to keep headings, results, and expected mistakes visually consistent.
/// This helper exists only for the examples project; it centralizes console color changes and output labels so each sample can focus on Option and Result behavior.
/// All members write to <see cref="Console"/> as a side effect and do not return data to callers.
/// </summary>
internal static class ConsoleHelper
{
    /// <summary>
    /// Prints a large cyan section header around a sample title so a new walkthrough is easy to find in console output.
    /// Use this at the start of an example module; <paramref name="title"/> is written exactly as the visible heading text and no value is returned.
    /// </summary>
    /// <param name="title">The human-readable title for the current example section, including any numbering or topic text that should appear in the console.</param>
    internal static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{"".PadRight(60, '=')}");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"{"".PadRight(60, '=')}");
        Console.ResetColor();
    }

    /// <summary>
    /// Prints a yellow subsection heading for a smaller group of related example statements.
    /// Use this inside an example module before a focused demo; <paramref name="title"/> describes the local topic and the method returns no value.
    /// </summary>
    /// <param name="title">The subsection label that will be written after the separator marker in the console.</param>
    internal static void PrintSubSection(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n--- {title} ---");
        Console.ResetColor();
    }

    /// <summary>
    /// Prints a labeled result line so an expression and its observed value are shown together.
    /// Use this for ordinary sample output; <paramref name="label"/> describes the expression being demonstrated and <paramref name="value"/> is converted with normal string formatting.
    /// </summary>
    /// <param name="label">The left-side description of the expression, scenario, or API call whose result is being displayed.</param>
    /// <param name="value">The observed value to display; null is allowed because some examples intentionally demonstrate absent values.</param>
    internal static void PrintResult(string label, object? value)
    {
        Console.WriteLine($"  {label} => {value}");
    }

    /// <summary>
    /// Prints a green success note for behavior that the example wants the reader to treat as correct or expected.
    /// Use this after a demonstration confirms an intended rule; <paramref name="description"/> is the explanatory text and the method returns no value.
    /// </summary>
    /// <param name="description">The message explaining the correct behavior that was just demonstrated.</param>
    internal static void PrintCorrect(string description)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  [OK]   ");
        Console.ResetColor();
        Console.WriteLine(description);
    }

    /// <summary>
    /// Prints a red warning note for behavior that the example wants the reader to recognize as unsafe, surprising, or incorrect.
    /// Use this in pitfall sections; <paramref name="description"/> explains the mistake and the method only writes to the console.
    /// </summary>
    /// <param name="description">The message explaining the incorrect or risky behavior being highlighted.</param>
    internal static void PrintWrong(string description)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  [BAD]  ");
        Console.ResetColor();
        Console.WriteLine(description);
    }

    /// <summary>
    /// Executes a demonstration action and prints any thrown exception instead of letting the examples program stop.
    /// Use this when a sample intentionally calls an unsafe API such as Unwrap on an empty value; <paramref name="label"/> identifies the operation and <paramref name="action"/> contains the code that may throw.
    /// </summary>
    /// <param name="label">The text used to identify the operation if an exception is caught and printed.</param>
    /// <param name="action">The synchronous demonstration code to run; any thrown exception is caught and displayed as example output.</param>
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
