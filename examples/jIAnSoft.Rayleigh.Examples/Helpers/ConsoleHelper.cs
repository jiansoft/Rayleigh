namespace jIAnSoft.Rayleigh.Examples.Helpers;

/// <summary>
/// 範例專案的主控台排版工具。
/// </summary>
/// <remarks>
/// <para>
/// 這個類別本身<b>不是</b>學習重點，它只負責把範例的輸出排得整齊好讀。
/// 如果你是第一次看這個專案，可以直接跳過本檔案，從 <c>E01OptionBasics.cs</c> 開始讀。
/// </para>
/// <para>
/// 各方法的用途一覽：
/// </para>
/// <list type="table">
///   <item><term>Header</term><description>整個範例模組的大標題</description></item>
///   <item><term>Section</term><description>模組內的小節標題</description></item>
///   <item><term>Explain</term><description>純文字說明（觀念解釋）</description></item>
///   <item><term>Demo</term><description>「程式碼 → 執行結果」對照，最常用</description></item>
///   <item><term>Good / Bad</term><description>標示正確做法與應避免的做法</description></item>
///   <item><term>Tip</term><description>實務建議</description></item>
///   <item><term>Boom</term><description>刻意示範會拋例外的程式碼（不會中斷範例）</description></item>
///   <item><term>Compare</term><description>兩種寫法的並排對照</description></item>
///   <item><term>Summary</term><description>模組結尾的重點整理</description></item>
/// </list>
/// </remarks>
internal static class ConsoleHelper
{
    private const int Width = 78;
    private const int CodeColumnWidth = 46;

    /// <summary>印出整個範例模組的大標題。</summary>
    /// <param name="code">模組代號，例如 <c>"E01"</c>。</param>
    /// <param name="title">模組主題。</param>
    internal static void Header(string code, string title)
    {
        Console.WriteLine();
        WriteLine(ConsoleColor.Cyan, new string('=', Width));
        WriteLine(ConsoleColor.Cyan, $"  {code}  {title}");
        WriteLine(ConsoleColor.Cyan, new string('=', Width));
    }

    /// <summary>印出模組內的小節標題。</summary>
    /// <param name="number">小節編號，例如 <c>"1"</c>。</param>
    /// <param name="title">小節主題。</param>
    internal static void Section(string number, string title)
    {
        Console.WriteLine();
        var prefix = $"-- {number}. {title} ";
        WriteLine(ConsoleColor.Yellow, prefix + new string('-', Math.Max(0, Width - prefix.Length)));
    }

    /// <summary>
    /// 印出一段純文字說明，用來解釋「為什麼」而不只是「怎麼做」。
    /// </summary>
    /// <param name="text">說明文字。可用 <c>\n</c> 換行，每行都會自動縮排對齊。</param>
    internal static void Explain(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            WriteLine(ConsoleColor.DarkGray, $"   {line.TrimEnd()}");
        }
    }

    /// <summary>
    /// 印出「程式碼 → 執行結果」的對照，這是範例中最常用的輸出方式。
    /// </summary>
    /// <param name="code">要展示的程式碼片段（原始碼字面文字）。</param>
    /// <param name="result">該程式碼實際執行後的結果，會以一般字串格式化輸出。</param>
    internal static void Demo(string code, object? result)
    {
        Console.Write("   ");
        Write(ConsoleColor.White, code.PadRight(CodeColumnWidth));
        Write(ConsoleColor.DarkGray, " => ");
        WriteLine(ConsoleColor.Green, result?.ToString() ?? "(null)");
    }

    /// <summary>
    /// 印出「程式碼 → 執行結果」對照，並附帶一行補充說明。
    /// </summary>
    /// <param name="code">要展示的程式碼片段。</param>
    /// <param name="result">執行結果。</param>
    /// <param name="note">針對這一行結果的補充說明。</param>
    internal static void Demo(string code, object? result, string note)
    {
        Demo(code, result);
        WriteLine(ConsoleColor.DarkGray, $"   {new string(' ', CodeColumnWidth)}    ^ {note}");
    }

    /// <summary>標示一個正確、推薦的做法。</summary>
    /// <param name="text">說明文字。</param>
    internal static void Good(string text)
    {
        Write(ConsoleColor.Green, "   [推薦] ");
        Console.WriteLine(text);
    }

    /// <summary>標示一個應該避免的做法。</summary>
    /// <param name="text">說明文字。</param>
    internal static void Bad(string text)
    {
        Write(ConsoleColor.Red, "   [避免] ");
        Console.WriteLine(text);
    }

    /// <summary>印出一則實務建議。</summary>
    /// <param name="text">建議內容。</param>
    internal static void Tip(string text)
    {
        Write(ConsoleColor.Magenta, "   [提示] ");
        Console.WriteLine(text);
    }

    /// <summary>
    /// 執行一段<b>預期會拋出例外</b>的示範程式碼，並印出例外資訊而不中斷整個範例。
    /// </summary>
    /// <param name="code">要展示的程式碼片段。</param>
    /// <param name="action">實際執行的程式碼；拋出的例外會被攔截並印出。</param>
    internal static void Boom(string code, Action action)
    {
        try
        {
            action();
            Console.Write("   ");
            Write(ConsoleColor.White, code.PadRight(CodeColumnWidth));
            Write(ConsoleColor.DarkGray, " => ");
            WriteLine(ConsoleColor.DarkYellow, "(預期會拋例外，但沒有發生)");
        }
        catch (Exception ex)
        {
            Console.Write("   ");
            Write(ConsoleColor.White, code.PadRight(CodeColumnWidth));
            Write(ConsoleColor.DarkGray, " => ");
            WriteLine(ConsoleColor.Red, $"[爆炸] {ex.GetType().Name}");
            WriteLine(ConsoleColor.DarkGray, $"   {new string(' ', CodeColumnWidth)}    {ex.Message}");
        }
    }

    /// <summary>
    /// 並排對照兩種寫法，用於凸顯「舊寫法 vs 新寫法」或「BCL vs Rayleigh」的差異。
    /// </summary>
    /// <param name="leftLabel">左側寫法的名稱。</param>
    /// <param name="leftResult">左側寫法的結果。</param>
    /// <param name="rightLabel">右側寫法的名稱。</param>
    /// <param name="rightResult">右側寫法的結果。</param>
    internal static void Compare(string leftLabel, object? leftResult, string rightLabel, object? rightResult)
    {
        Console.Write("   ");
        Write(ConsoleColor.DarkYellow, leftLabel.PadRight(CodeColumnWidth));
        Write(ConsoleColor.DarkGray, " => ");
        WriteLine(ConsoleColor.DarkYellow, leftResult?.ToString() ?? "(null)");

        Console.Write("   ");
        Write(ConsoleColor.Cyan, rightLabel.PadRight(CodeColumnWidth));
        Write(ConsoleColor.DarkGray, " => ");
        WriteLine(ConsoleColor.Cyan, rightResult?.ToString() ?? "(null)");
    }

    /// <summary>印出模組結尾的重點整理。</summary>
    /// <param name="points">要條列的重點，每項一行。</param>
    internal static void Summary(params string[] points)
    {
        Console.WriteLine();
        var title = "   +- 本節重點 ";
        WriteLine(ConsoleColor.Cyan, title + new string('-', Math.Max(0, Width - title.Length)));
        foreach (var point in points)
        {
            WriteLine(ConsoleColor.Cyan, $"   | * {point}");
        }

        WriteLine(ConsoleColor.Cyan, "   +" + new string('-', Width - 4));
    }

    private static void Write(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    private static void WriteLine(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
