using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E08：集合操作 —— 把 LINQ 世界和 Option / Result 世界接起來。
/// </summary>
/// <remarks>
/// <para><b>這個模組解決兩個問題</b></para>
/// <para>
/// <b>問題一：怎麼從集合「進入」Option 世界？</b><br/>
/// LINQ 的 <c>FirstOrDefault()</c> 有個致命缺陷——它分不出「找不到」和「找到了一個剛好等於預設值的元素」。
/// 對 <c>int</c> 來說，空清單和 <c>[0]</c> 都會回傳 <c>0</c>。本模組的 <c>FirstOrNone()</c> 系列解決這件事。
/// </para>
/// <para>
/// <b>問題二：手上有一堆 Option / Result，怎麼合併處理？</b><br/>
/// 例如驗證表單的 10 個欄位，得到 10 個 <c>Result</c>。你想要的是
/// 「全部通過才算成功」或「一次列出所有錯誤」——這就是 <c>Sequence()</c> 和 <c>Partition()</c>。
/// </para>
/// </remarks>
public static class E08CollectionOperations
{
    /// <summary>執行 E08 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E08", "集合操作：FirstOrNone / Sequence / Partition / Values");

        TheFirstOrDefaultProblem();
        EntryPointMethods();
        DictionaryLookup();
        ValuesFiltersSome();
        SequenceAllOrNothing();
        PartitionCollectsEverything();
        SequenceVersusPartition();

        ConsoleHelper.Summary(
            "FirstOrNone / SingleOrNone / ElementAtOrNone：取代會說謊的 FirstOrDefault",
            "GetValueOrNone：取代 GetValueOrDefault，能區分「沒這個 key」和「值就是 0」",
            "Values()：一堆 Option 中，把有值的挑出來（忽略 None）",
            "Sequence()：全部成功才成功，遇到第一個失敗就短路",
            "Partition()：走完全部，同時拿到「所有成功值」和「所有錯誤」");
    }

    /// <summary>
    /// 第 1 節：先示範 FirstOrDefault 的問題，讓你知道為什麼需要 FirstOrNone。
    /// </summary>
    private static void TheFirstOrDefaultProblem()
    {
        ConsoleHelper.Section("1", "為什麼需要 FirstOrNone？先看 FirstOrDefault 的問題");

        ConsoleHelper.Explain(
            "假設你在查詢「今天的第一筆交易金額」，用 LINQ 的 FirstOrDefault：");

        var noTransactions = Array.Empty<int>();
        var freeTransaction = new[] { 0, 500, 1200 };   // 第一筆是免費的，金額 0

        Console.WriteLine();
        ConsoleHelper.Compare(
            "空清單.FirstOrDefault()", noTransactions.FirstOrDefault(),
            "[0, 500, 1200].FirstOrDefault()", freeTransaction.FirstOrDefault());

        ConsoleHelper.Bad("兩者都回傳 0——你分不出「今天沒有交易」和「第一筆是免費的」");

        Console.WriteLine();
        ConsoleHelper.Explain("換成 FirstOrNone，兩種情況就清清楚楚：");

        ConsoleHelper.Compare(
            "空清單.FirstOrNone()", noTransactions.FirstOrNone(),
            "[0, 500, 1200].FirstOrNone()", freeTransaction.FirstOrNone());

        ConsoleHelper.Good("None 代表「真的沒有」，Some(0) 代表「有，而且值是 0」");

        ConsoleHelper.Tip("只要元素型別是實值型別（int、decimal、DateTime、enum...），就一定要注意這個問題");
    }

    /// <summary>
    /// 第 2 節：所有的「進入點」方法。
    /// </summary>
    private static void EntryPointMethods()
    {
        ConsoleHelper.Section("2", "進入點方法：FirstOrNone / SingleOrNone / ElementAtOrNone");

        var numbers = new[] { 10, 20, 30 };
        var empty = Array.Empty<int>();

        // -- FirstOrNone：取第一個 -------------------------------------------
        ConsoleHelper.Demo("[10,20,30].FirstOrNone()", numbers.FirstOrNone());
        ConsoleHelper.Demo("[].FirstOrNone()", empty.FirstOrNone());

        // 也可以帶條件，找第一個符合的。
        ConsoleHelper.Demo("FirstOrNone(x => x > 15)", numbers.FirstOrNone(x => x > 15));
        ConsoleHelper.Demo("FirstOrNone(x => x > 100)", numbers.FirstOrNone(x => x > 100), "沒有符合的");

        Console.WriteLine();

        // -- SingleOrNone：必須「剛好一個」-----------------------------------
        ConsoleHelper.Explain(
            "SingleOrNone 要求「剛好一個元素」。和 LINQ 的 Single() 不同的是——\n" +
            "元素太多的時候它不會拋例外，而是回傳 None。");

        ConsoleHelper.Demo("[42].SingleOrNone()", new[] { 42 }.SingleOrNone(), "剛好一個");
        ConsoleHelper.Demo("[10,20,30].SingleOrNone()", numbers.SingleOrNone(), "太多個，回傳 None");
        ConsoleHelper.Demo("[].SingleOrNone()", empty.SingleOrNone(), "沒有，回傳 None");

        ConsoleHelper.Tip("如果你需要區分「沒有」和「太多」，就別用 SingleOrNone，自己寫 Result 版本");

        Console.WriteLine();

        // -- ElementAtOrNone：依索引取值 -------------------------------------
        ConsoleHelper.Explain("ElementAtOrNone 依索引取值，超出範圍回傳 None 而不是拋例外。");
        ConsoleHelper.Demo("[10,20,30].ElementAtOrNone(1)", numbers.ElementAtOrNone(1));
        ConsoleHelper.Demo("[10,20,30].ElementAtOrNone(99)", numbers.ElementAtOrNone(99), "超出範圍");
        ConsoleHelper.Demo("[10,20,30].ElementAtOrNone(-1)", numbers.ElementAtOrNone(-1), "負數也安全");
    }

    /// <summary>
    /// 第 3 節：字典查詢。
    /// </summary>
    private static void DictionaryLookup()
    {
        ConsoleHelper.Section("3", "GetValueOrNone：字典查詢也有同樣的問題");

        // 這個字典裡，"guest" 的權限等級就是 0——這是有意義的值，不是「沒有資料」。
        var permissions = new Dictionary<string, int>
        {
            ["admin"] = 9,
            ["guest"] = 0
        };

        ConsoleHelper.Explain("情境：查詢使用者的權限等級。guest 的等級剛好是 0。");
        Console.WriteLine();

        ConsoleHelper.Compare(
            "GetValueOrDefault(\"guest\")", permissions.GetValueOrDefault("guest"),
            "GetValueOrNone(\"guest\")", permissions.GetValueOrNone("guest"));

        ConsoleHelper.Compare(
            "GetValueOrDefault(\"nobody\")", permissions.GetValueOrDefault("nobody"),
            "GetValueOrNone(\"nobody\")", permissions.GetValueOrNone("nobody"));

        ConsoleHelper.Bad("GetValueOrDefault 對兩者都回傳 0，你會誤以為 nobody 也是 guest 等級");
        ConsoleHelper.Good("GetValueOrNone 明確區分「等級是 0」和「這個人不存在」");

        Console.WriteLine();
        ConsoleHelper.Explain("而且它可以直接串接後續操作：");
        var level = permissions.GetValueOrNone("nobody")
            .Filter(l => l > 0)
            .MapOr("無權限", l => $"等級 {l}");
        ConsoleHelper.Demo("查詢 nobody 並格式化", level);
    }

    /// <summary>
    /// 第 4 節：Values。
    /// </summary>
    private static void ValuesFiltersSome()
    {
        ConsoleHelper.Section("4", "Values()：從一堆 Option 中挑出有值的");

        ConsoleHelper.Explain(
            "當你有一個 Option 的集合，而且想「忽略沒有值的，只留下有值的」，就用 Values()。");

        // 模擬：解析一批使用者輸入的數字，有些格式不對。
        var inputs = new[] { "10", "abc", "30", "", "50" };
        var parsed = inputs.Select(ParseNumber).ToArray();

        ConsoleHelper.Demo("原始輸入", string.Join(", ", inputs.Select(s => $"\"{s}\"")));
        ConsoleHelper.Demo("逐一解析的結果", string.Join(", ", parsed));

        // Values() 把 None 濾掉，只留下 Some 裡面的值。
        var validNumbers = parsed.Values().ToList();
        ConsoleHelper.Demo("parsed.Values()", string.Join(", ", validNumbers), "壞掉的輸入被安靜地忽略了");
        ConsoleHelper.Demo("合計", validNumbers.Sum());

        ConsoleHelper.Tip("Values() 是「盡力而為」的語意——如果壞資料不能忽略，請看下一節的 Sequence()");
    }

    /// <summary>
    /// 第 5 節：Sequence。
    /// </summary>
    private static void SequenceAllOrNothing()
    {
        ConsoleHelper.Section("5", "Sequence()：全部成功才算成功");

        ConsoleHelper.Explain(
            "Sequence 會把「集合的 Option」翻轉成「Option 的集合」：\n" +
            "\n" +
            "  IEnumerable<Option<T>>  ->  Option<List<T>>\n" +
            "\n" +
            "只要有任何一個是 None，整體就是 None。這叫「全有或全無」。");

        Console.WriteLine();

        var allValid = new[] { "10", "20", "30" }.Select(ParseNumber).ToArray();
        var hasInvalid = new[] { "10", "abc", "30" }.Select(ParseNumber).ToArray();

        ConsoleHelper.Demo("全部有效 -> Sequence()", allValid.Sequence());
        ConsoleHelper.Demo("含無效值 -> Sequence()", hasInvalid.Sequence(), "只要一個壞掉，整批作廢");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Result 版本更實用——它會告訴你「第一個」失敗的原因是什麼：");

        var results = new[] { "10", "abc", "-5" }.Select(ParseChecked).ToArray();
        ConsoleHelper.Demo("逐一驗證的結果", string.Join(", ", results));
        ConsoleHelper.Demo("results.Sequence()", results.Sequence(), "回傳第一個錯誤，不是最後一個");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "重點：Sequence 會「短路」。遇到第一個失敗就立刻停止，\n" +
            "後面的元素連看都不會看——這在處理大量資料或昂貴驗證時很重要。");

        var visited = 0;
        var shortCircuited = new[] { 1, 2, 3, 4, 5 }
            .Select(x =>
            {
                visited++;
                return x >= 3 ? Option<int>.None : Option<int>.Some(x);
            })
            .Sequence();

        ConsoleHelper.Demo("處理 [1..5]，第 3 個開始失敗", shortCircuited);
        ConsoleHelper.Demo("實際走訪了幾個元素？", visited, "只有 3 個，4 和 5 沒被碰到");
    }

    /// <summary>
    /// 第 6 節：Partition。
    /// </summary>
    private static void PartitionCollectsEverything()
    {
        ConsoleHelper.Section("6", "Partition()：一次拿到所有成功值和所有錯誤");

        ConsoleHelper.Explain(
            "Sequence 遇到第一個錯誤就停了，但表單驗證需要的是相反的行為——\n" +
            "使用者填錯三個欄位，你應該一次告訴他三個問題，而不是改一個再說下一個。\n" +
            "這就是 Partition 的用途。");

        Console.WriteLine();

        var formInputs = new[] { "25", "abc", "-5", "40", "xyz" };
        var validations = formInputs.Select(ParseChecked).ToArray();

        // Partition 回傳一個 tuple，可以直接解構成兩個清單。
        var (values, errors) = validations.Partition();

        ConsoleHelper.Demo("輸入", string.Join(", ", formInputs.Select(s => $"\"{s}\"")));
        ConsoleHelper.Demo("成功的值 (Values)", string.Join(", ", values));
        ConsoleHelper.Demo("所有的錯誤 (Errors)", string.Join(" | ", errors));

        Console.WriteLine();
        ConsoleHelper.Explain("實際的表單處理會長這樣：");

        if (errors.Count > 0)
        {
            ConsoleHelper.Demo("回傳給使用者", $"400 Bad Request，共 {errors.Count} 個問題");
            foreach (var error in errors)
            {
                ConsoleHelper.Demo("  問題", error);
            }
        }

        ConsoleHelper.Good("使用者一次看到所有問題，不用來回修改五次");
    }

    /// <summary>
    /// 第 7 節：兩者的取捨。
    /// </summary>
    private static void SequenceVersusPartition()
    {
        ConsoleHelper.Section("7", "怎麼選？Sequence 還是 Partition");

        var data = new[] { "1", "bad1", "3", "bad2" }.Select(ParseChecked).ToArray();

        var sequenced = data.Sequence();
        var (values, errors) = data.Partition();

        ConsoleHelper.Compare(
            "Sequence() 的結果", sequenced,
            "Partition() 的錯誤清單", string.Join(" | ", errors));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "同一批資料，兩種方法給你不同的東西：\n" +
            "\n" +
            "  Sequence   -> 只有第一個錯誤，遇到就停止走訪\n" +
            "  Partition  -> 全部的錯誤，一定會走訪完整個集合\n" +
            "\n" +
            "選擇原則：");

        Console.WriteLine();
        ConsoleHelper.Good("用 Sequence：批次匯入資料，有問題就整批退回（而且想早點停下來省時間）");
        ConsoleHelper.Good("用 Partition：表單驗證、資料檢查報表，需要一次列出所有問題");
        ConsoleHelper.Good("用 Values：容錯處理，壞掉的資料直接忽略就好");

        Console.WriteLine();
        ConsoleHelper.Tip($"順帶一提，成功的值 Partition 也給你了：[{string.Join(", ", values)}]");
    }

    // ================================================================
    // 以下是本範例用到的輔助方法
    // ================================================================

    /// <summary>
    /// 把字串解析成數字。<b>回傳 Option</b>——只說成功或失敗，不說原因。
    /// </summary>
    private static Option<int> ParseNumber(string input)
        => int.TryParse(input, out var value) ? Option<int>.Some(value) : Option.None;

    /// <summary>
    /// 把字串解析成正整數。<b>回傳 Result</b>——失敗時附上原因。
    /// </summary>
    private static Result<int, string> ParseChecked(string input)
    {
        if (!int.TryParse(input, out var value))
        {
            return $"\"{input}\" 不是數字";
        }

        if (value <= 0)
        {
            return $"{value} 必須大於 0";
        }

        return value;
    }
}
