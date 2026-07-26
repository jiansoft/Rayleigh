using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E03：把值安全地取出來 —— 這是每個人最後都要面對的一步。
/// </summary>
/// <remarks>
/// <para><b>本模組的核心問題</b></para>
/// <para>
/// 值包在 <c>Option</c> 裡面很安全，但總有一刻你得把它拿出來用——
/// 要顯示在畫面上、要存進資料庫、要回傳給呼叫端。這時候有很多種取法，
/// 選錯就會讓前面所有的安全性白費。
/// </para>
/// <para><b>取值方法的安全性排行（由安全到危險）</b></para>
/// <list type="number">
///   <item><description><c>Match</c>：強迫你同時處理兩種狀態，最安全</description></item>
///   <item><description><c>TryGetValue</c>：搭配 guard clause，很適合早期返回</description></item>
///   <item><description><c>UnwrapOr</c> / <c>UnwrapOrElse</c>：給預設值，不會爆炸</description></item>
///   <item><description><c>Expect</c>：會爆炸，但你可以寫清楚錯誤訊息</description></item>
///   <item><description><c>Unwrap</c>：會爆炸，訊息還很籠統——非必要不要用</description></item>
/// </list>
/// </remarks>
public static class E03OptionAdvanced
{
    /// <summary>執行 E03 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E03", "把值取出來：Match / TryGetValue / Unwrap 家族");

        MatchIsTheSafestWay();
        TryGetValueGuardClause();
        UnwrapFamily();
        DeconstructAndPatternMatching();
        OrAndOrElse();
        TapForSideEffects();
        ZipCombinesTwoOptions();

        ConsoleHelper.Summary(
            "Match：同時處理 Some 和 None，編譯器保證你不會漏掉任何一邊",
            "TryGetValue：搭配 if (!opt.TryGetValue(out var v)) return; 寫 guard clause",
            "UnwrapOr(預設值) 安全；Unwrap() 會拋例外，只在「不可能沒值」時用",
            "Or / OrElse：提供備援 Option；OrElse 是惰性的，備援昂貴時用它",
            "Tap：只是偷看一眼（例如寫 log），不改變 Option 的內容");
    }

    /// <summary>
    /// 第 1 節：Match —— 最安全的取值方式。
    /// </summary>
    private static void MatchIsTheSafestWay()
    {
        ConsoleHelper.Section("1", "Match：同時處理「有值」和「沒值」兩種情況");

        ConsoleHelper.Explain(
            "Match 要你提供兩個函式：有值時做什麼、沒值時做什麼。\n" +
            "因為兩邊都必須寫，你不可能「忘記處理沒值的情況」——這就是它最安全的原因。");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Match 的兩個參數可以用具名參數寫，可讀性更好。
        var someMessage = some.Match(
            some: value => $"拿到了 {value}",
            none: () => "什麼都沒有");

        var noneMessage = none.Match(
            some: value => $"拿到了 {value}",
            none: () => "什麼都沒有");

        ConsoleHelper.Demo("Some(42).Match(...)", someMessage);
        ConsoleHelper.Demo("None.Match(...)", noneMessage);

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Match 也有「不回傳值」的版本，用來執行副作用（例如印東西、發通知）。");

        some.Match(
            some: value => ConsoleHelper.Demo("  有值分支被執行", value),
            none: () => ConsoleHelper.Demo("  沒值分支被執行", "(不會看到這行)"));

        ConsoleHelper.Tip("Match 最適合用在「最終要把 Option 轉成別的東西」的地方，例如轉成 HTTP 回應");
    }

    /// <summary>
    /// 第 2 節：TryGetValue —— C# 開發者最熟悉的模式。
    /// </summary>
    private static void TryGetValueGuardClause()
    {
        ConsoleHelper.Section("2", "TryGetValue：寫 guard clause，減少巢狀縮排");

        ConsoleHelper.Explain(
            "這個模式和 int.TryParse、Dictionary.TryGetValue 完全一樣，\n" +
            "C# 開發者一看就懂，不需要適應期。");

        // DescribeProduct 示範了 guard clause 的寫法，定義在檔案下方。
        ConsoleHelper.Demo("DescribeProduct(1)", DescribeProduct(1));
        ConsoleHelper.Demo("DescribeProduct(999)", DescribeProduct(999));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "關鍵在於「先處理失敗、提早 return」，讓主要邏輯留在最外層不縮排。\n" +
            "對照組（巢狀寫法）雖然結果一樣，但多一層縮排；\n" +
            "當你有三、四個這種檢查時，差別就非常明顯了。");

        ConsoleHelper.Good("if (!opt.TryGetValue(out var v)) { return 失敗; }  // 之後 v 保證可用");
    }

    /// <summary>
    /// 第 3 節：Unwrap 家族 —— 從安全到危險。
    /// </summary>
    private static void UnwrapFamily()
    {
        ConsoleHelper.Section("3", "Unwrap 家族：UnwrapOr / UnwrapOrElse / Expect / Unwrap");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // -- UnwrapOr：最常用，給一個預設值，絕對不會爆炸 --------------------
        ConsoleHelper.Explain("UnwrapOr(預設值)：有值就給值，沒值就給你指定的預設值。永遠安全。");
        ConsoleHelper.Demo("Some(42).UnwrapOr(-1)", some.UnwrapOr(-1));
        ConsoleHelper.Demo("None.UnwrapOr(-1)", none.UnwrapOr(-1));

        Console.WriteLine();

        // -- UnwrapOrElse：預設值改用函式提供，需要時才計算 -------------------
        ConsoleHelper.Explain(
            "UnwrapOrElse(函式)：和上面一樣，但預設值改由函式產生。\n" +
            "差別在於「有值的時候函式不會被執行」，適合預設值很昂貴的情況。");

        var computeCount = 0;
        int ExpensiveDefault()
        {
            computeCount++;
            return -999;
        }

        ConsoleHelper.Demo("Some(42).UnwrapOrElse(昂貴計算)", some.UnwrapOrElse(ExpensiveDefault));
        ConsoleHelper.Demo("昂貴計算被執行幾次？", computeCount, "0 次");
        ConsoleHelper.Demo("None.UnwrapOrElse(昂貴計算)", none.UnwrapOrElse(ExpensiveDefault));
        ConsoleHelper.Demo("現在呢？", computeCount, "1 次");

        Console.WriteLine();

        // -- Unwrap / Expect：會拋例外的版本 ---------------------------------
        ConsoleHelper.Explain(
            "Unwrap() 和 Expect(訊息) 在沒有值的時候會直接拋例外。\n" +
            "它們的意思是「我確定這裡一定有值，如果沒有就是程式有 bug」。");

        ConsoleHelper.Demo("Some(42).Unwrap()", some.Unwrap(), "有值時正常回傳");
        ConsoleHelper.Boom("None.Unwrap()", () => none.Unwrap());
        ConsoleHelper.Boom("None.Expect(\"設定檔一定要有 Port\")", () => none.Expect("設定檔一定要有 Port"));

        ConsoleHelper.Bad("在一般業務流程中使用 Unwrap()——那等於把 Option 的保護全部丟掉");
        ConsoleHelper.Good("非用不可時請改用 Expect(\"清楚的原因\")，出事時才知道發生什麼");
    }

    /// <summary>
    /// 第 4 節：解構與模式比對。
    /// </summary>
    private static void DeconstructAndPatternMatching()
    {
        ConsoleHelper.Section("4", "解構（Deconstruct）與 switch 模式比對");

        ConsoleHelper.Explain(
            "Option 支援解構成 (bool 有沒有值, 值)，\n" +
            "所以可以直接用在 C# 的 switch 運算式裡。");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // 直接解構成兩個變數。
        var (hasValue, value) = some;
        ConsoleHelper.Demo("var (hasValue, value) = Some(42);", $"hasValue={hasValue}, value={value}");

        // 用在 switch 運算式中。
        // 注意：因為有解構，可以直接用 tuple 模式 (true, var v)。
        string Describe(Option<int> option) => option switch
        {
            (true, var v) when v > 100 => $"很大的數字：{v}",
            (true, var v) => $"數字：{v}",
            (false, _) => "沒有數字"
        };

        ConsoleHelper.Demo("Describe(Some(42))", Describe(some));
        ConsoleHelper.Demo("Describe(Some(500))", Describe(Option<int>.Some(500)));
        ConsoleHelper.Demo("Describe(None)", Describe(none));

        ConsoleHelper.Tip("解構出來的 value 在 hasValue 為 false 時是型別預設值，別直接拿來用");
    }

    /// <summary>
    /// 第 5 節：Or / OrElse —— 提供備援。
    /// </summary>
    private static void OrAndOrElse()
    {
        ConsoleHelper.Section("5", "Or / OrElse：這個沒有就用另一個");

        ConsoleHelper.Explain(
            "注意這兩個和 UnwrapOr 的差別：\n" +
            "  UnwrapOr  -> 回傳「普通的值」，離開 Option 世界\n" +
            "  Or/OrElse -> 回傳「另一個 Option」，還留在 Option 世界");

        var none = Option<int>.None;
        var backup = Option<int>.Some(99);

        ConsoleHelper.Demo("None.Or(Some(99))", none.Or(backup));
        ConsoleHelper.Demo("Some(42).Or(Some(99))", Option<int>.Some(42).Or(backup), "本來就有值，不理會備援");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Or 的參數會「立刻求值」——就算用不到，備援也已經算好了。\n" +
            "如果備援很貴（例如查資料庫），請改用 OrElse 傳函式進去。");

        var dbHitCount = 0;
        Option<int> QueryDatabase()
        {
            dbHitCount++;
            return Option<int>.Some(777);
        }

        ConsoleHelper.Demo("Some(42).OrElse(查資料庫)", Option<int>.Some(42).OrElse(QueryDatabase));
        ConsoleHelper.Demo("資料庫被查了幾次？", dbHitCount, "0 次——快取命中，不需要查");
        ConsoleHelper.Demo("None.OrElse(查資料庫)", none.OrElse(QueryDatabase));
        ConsoleHelper.Demo("現在呢？", dbHitCount, "1 次");

        ConsoleHelper.Good("多層備援可以一直串：快取.OrElse(查DB).OrElse(叫API).UnwrapOr(預設值)");
    }

    /// <summary>
    /// 第 6 節：Tap —— 偷看但不改變。
    /// </summary>
    private static void TapForSideEffects()
    {
        ConsoleHelper.Section("6", "Tap：在鏈中間插入副作用（例如寫 log）而不改變內容");

        ConsoleHelper.Explain(
            "Tap 會在「有值」時執行你給的動作，然後原封不動把 Option 傳下去。\n" +
            "典型用途是在處理鏈中間加日誌，而不打斷整條鏈。");

        var log = new List<string>();

        var result = Option<int>.Some(10)
            .Tap(v => log.Add($"初始值：{v}"))
            .Map(v => v * 3)
            .Tap(v => log.Add($"乘以 3 之後：{v}"))
            .Filter(v => v > 100)
            .Tap(v => log.Add($"通過篩選：{v}"))   // 這行不會執行，因為 30 沒有 > 100
            .UnwrapOr(-1);

        ConsoleHelper.Demo("整條鏈的最終結果", result);
        ConsoleHelper.Explain("記錄下來的日誌：");
        foreach (var entry in log)
        {
            ConsoleHelper.Demo("  log", entry);
        }

        ConsoleHelper.Explain(
            "注意第三個 Tap 沒有出現在日誌裡——\n" +
            "因為 Filter 已經把它變成 None，之後的 Tap 就不會執行了。");
    }

    /// <summary>
    /// 第 7 節：Zip —— 需要兩個值同時存在。
    /// </summary>
    private static void ZipCombinesTwoOptions()
    {
        ConsoleHelper.Section("7", "Zip / ZipWith：兩個 Option 都有值才能繼續");

        ConsoleHelper.Explain(
            "有時候你需要兩份資料都拿到才能做事（例如寬和高都有才能算面積）。\n" +
            "Zip 會把兩個 Option 合併成一個裝著 tuple 的 Option。");

        var width = Option<int>.Some(10);
        var height = Option<int>.Some(20);
        var missing = Option<int>.None;

        ConsoleHelper.Demo("Some(10).Zip(Some(20))", width.Zip(height), "兩個都有值");
        ConsoleHelper.Demo("Some(10).Zip(None)", width.Zip(missing), "任一個沒值就是 None");

        Console.WriteLine();
        ConsoleHelper.Explain("ZipWith 更進一步：合併的同時直接算出你要的結果。");
        ConsoleHelper.Demo("width.ZipWith(height, (w, h) => w * h)", width.ZipWith(height, (w, h) => w * h));
        ConsoleHelper.Demo("width.ZipWith(missing, (w, h) => w * h)", width.ZipWith(missing, (w, h) => w * h));

        ConsoleHelper.Tip("要合併三個以上的 Option，用 LINQ 的多重 from 更清楚——見 E09");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>範例用的商品資料。</summary>
    private sealed record Product(int Id, string Name, decimal Price);

    private static readonly Product[] Products =
    [
        new(1, "鍵盤", 2500m),
        new(2, "滑鼠", 890m)
    ];

    /// <summary>依 Id 尋找商品。</summary>
    private static Option<Product> FindProduct(int id)
    {
        foreach (var product in Products)
        {
            if (product.Id == id)
            {
                return product;
            }
        }

        return Option.None;
    }

    /// <summary>
    /// 示範 guard clause 寫法：先處理「找不到」並提早 return，主要邏輯不必縮排。
    /// </summary>
    private static string DescribeProduct(int id)
    {
        // 這一行就是重點：取不到值就立刻返回，不進入下面的邏輯。
        if (!FindProduct(id).TryGetValue(out var product))
        {
            return $"找不到 Id={id} 的商品";
        }

        // 走到這裡，編譯器知道 product 一定有值，可以安心使用。
        return $"{product.Name} 售價 {product.Price:C}";
    }
}
