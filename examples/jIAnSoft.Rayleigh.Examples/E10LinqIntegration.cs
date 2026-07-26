using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E10：LINQ 查詢語法 —— 用 from / where / select 操作 Option 和 Result。
/// </summary>
/// <remarks>
/// <para><b>為什麼 LINQ 語法能用在 Option 上？</b></para>
/// <para>
/// C# 的 <c>from ... select ...</c> 語法其實只是語法糖。編譯器看到它時，
/// 會自動去找型別上有沒有叫做 <c>Select</c>、<c>SelectMany</c>、<c>Where</c> 的方法。
/// 只要有，就能用查詢語法——不一定要是集合。
/// </para>
/// <para>
/// <c>Option&lt;T&gt;</c> 和 <c>Result&lt;T, E&gt;</c> 都實作了這三個方法，對應關係是：
/// </para>
/// <list type="table">
///   <item><term>Select</term><description>就是 Map</description></item>
///   <item><term>SelectMany</term><description>就是 Bind</description></item>
///   <item><term>Where</term><description>就是 Filter（只有 Option 有）</description></item>
/// </list>
/// <para>
/// <b>什麼時候用查詢語法比較好？</b>當你需要「同時用到好幾個中間值」的時候。
/// 用 Bind 串接時，前面步驟的變數會被巢狀的 lambda 蓋住；用 from 就都在同一層，隨手可取。
/// </para>
/// </remarks>
public static class E10LinqIntegration
{
    /// <summary>執行 E10 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E10", "LINQ 查詢語法：from / where / select");

        OptionSelectAndWhere();
        OptionMultipleFrom();
        ResultQuerySyntax();
        WhyQuerySyntaxWins();
        BothStylesAreEquivalent();

        ConsoleHelper.Summary(
            "Select = Map、SelectMany = Bind、Where = Filter",
            "多重 from 就是連續的 Bind，但所有中間變數都留在同一層可以直接用",
            "任何一步是 None / Err，整個查詢就會短路",
            "Result 沒有 Where（因為過濾掉之後不知道該給什麼錯誤）",
            "兩種寫法完全等價，選你團隊讀起來順的那種");
    }

    /// <summary>
    /// 第 1 節：Option 的基本查詢語法。
    /// </summary>
    private static void OptionSelectAndWhere()
    {
        ConsoleHelper.Section("1", "Option 的 from / select / where");

        ConsoleHelper.Explain("最簡單的形式：from 一個 Option，select 一個轉換結果。");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // 這段等同於 some.Map(x => x * 2)
        var doubled =
            from x in some
            select x * 2;

        ConsoleHelper.Demo("from x in Some(42) select x * 2", doubled);

        var fromNone =
            from x in none
            select x * 2;

        ConsoleHelper.Demo("from x in None select x * 2", fromNone, "None 進 None 出");

        Console.WriteLine();
        ConsoleHelper.Explain("加上 where 就等同於 Filter。");

        // 這段等同於 some.Filter(x => x > 40).Map(x => x * 2)
        var filtered =
            from x in some
            where x > 40
            select x * 2;

        var filteredOut =
            from x in some
            where x > 100
            select x * 2;

        ConsoleHelper.Demo("where x > 40（成立）", filtered);
        ConsoleHelper.Demo("where x > 100（不成立）", filteredOut, "被過濾掉，變成 None");
    }

    /// <summary>
    /// 第 2 節：多重 from —— 查詢語法真正發揮價值的地方。
    /// </summary>
    private static void OptionMultipleFrom()
    {
        ConsoleHelper.Section("2", "多重 from：這才是查詢語法的價值所在");

        ConsoleHelper.Explain(
            "情境：算訂單的總價，需要「單價」和「數量」兩個資料，兩個都可能查不到。");

        // 每多一個 from，就等同於多一次 Bind。
        // 關鍵是：price 和 quantity 在 select 那一行都還拿得到。
        var total =
            from price in GetPrice("keyboard")
            from quantity in GetQuantity("keyboard")
            select price * quantity;

        ConsoleHelper.Demo("查得到單價和數量", total);

        var missingQuantity =
            from price in GetPrice("mouse")
            from quantity in GetQuantity("mouse")     // 這個查不到
            select price * quantity;

        ConsoleHelper.Demo("數量查不到", missingQuantity, "任一步 None，整個查詢就是 None");

        Console.WriteLine();
        ConsoleHelper.Explain("三個以上也一樣，而且可以在中途用 where 篩選：");

        var withDiscount =
            from price in GetPrice("keyboard")
            from quantity in GetQuantity("keyboard")
            from discount in GetDiscount("keyboard")
            where quantity > 0
            select (price * quantity) * (1 - discount);

        ConsoleHelper.Demo("含折扣的總價", withDiscount);
    }

    /// <summary>
    /// 第 3 節：Result 的查詢語法。
    /// </summary>
    private static void ResultQuerySyntax()
    {
        ConsoleHelper.Section("3", "Result 的查詢語法（錯誤會自動傳遞）");

        ConsoleHelper.Explain(
            "Result 也支援 from / select，用法一模一樣。\n" +
            "差別是失敗時會帶著錯誤原因，而不是單純的 None。");

        var ok =
            from user in FindUser(1)
            from order in FindLatestOrder(user.Id)
            select $"{user.Name} 的最新訂單金額：{order.Amount:C}";

        ConsoleHelper.Demo("兩步都成功", ok);

        var userNotFound =
            from user in FindUser(999)
            from order in FindLatestOrder(user.Id)
            select $"{user.Name}：{order.Amount:C}";

        ConsoleHelper.Demo("第一步失敗", userNotFound, "錯誤原因被完整保留");

        var noOrder =
            from user in FindUser(2)
            from order in FindLatestOrder(user.Id)
            select $"{user.Name}：{order.Amount:C}";

        ConsoleHelper.Demo("第二步失敗", noOrder);

        Console.WriteLine();
        ConsoleHelper.Explain(
            "注意：Result 沒有提供 Where。\n" +
            "原因很單純——如果一筆資料被 where 過濾掉，該回傳什麼錯誤？沒有合理答案。\n" +
            "需要條件判斷時，請用 Bind 明確指定失敗時的錯誤。");
    }

    /// <summary>
    /// 第 4 節：對照 Bind 寫法，說明查詢語法的優勢。
    /// </summary>
    private static void WhyQuerySyntaxWins()
    {
        ConsoleHelper.Section("4", "對照：同一件事，Bind 寫法 vs 查詢語法");

        ConsoleHelper.Explain(
            "需求：拿到「使用者名稱」和「訂單金額」，兩個都要用在最後的字串裡。");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "用 Bind 的寫法——注意為了同時用到 user 和 order，\n" +
            "第二個 lambda 必須巢狀在第一個裡面：");

        // Bind 寫法：需要巢狀才能同時存取 user 和 order。
        var withBind = FindUser(1)
            .Bind(user => FindLatestOrder(user.Id)
                .Map(order => $"{user.Name} 花了 {order.Amount:C}"));

        ConsoleHelper.Demo("Bind 巢狀寫法", withBind);

        Console.WriteLine();
        ConsoleHelper.Explain("查詢語法——兩個變數都在同一層，不需要巢狀：");

        var withQuery =
            from user in FindUser(1)
            from order in FindLatestOrder(user.Id)
            select $"{user.Name} 花了 {order.Amount:C}";

        ConsoleHelper.Demo("查詢語法", withQuery);

        Console.WriteLine();
        ConsoleHelper.Good("只有兩層時差別不大，但到了三、四層，巢狀寫法會嚴重影響可讀性");
        ConsoleHelper.Tip("判斷原則：後面的步驟需不需要用到前面的中間值？需要就用查詢語法");
    }

    /// <summary>
    /// 第 5 節：兩種寫法完全等價。
    /// </summary>
    private static void BothStylesAreEquivalent()
    {
        ConsoleHelper.Section("5", "驗證：兩種寫法產生完全相同的結果");

        ConsoleHelper.Explain(
            "查詢語法只是編譯器的語法糖，最終會被轉換成方法呼叫。\n" +
            "下面用同一組資料跑兩種寫法，結果應該完全一致。");

        Console.WriteLine();

        foreach (var id in new[] { 1, 2, 999 })
        {
            // 寫法 A：方法鏈
            var fluent = FindUser(id)
                .Bind(user => FindLatestOrder(user.Id))
                .Map(order => order.Amount);

            // 寫法 B：查詢語法
            var query =
                from user in FindUser(id)
                from order in FindLatestOrder(user.Id)
                select order.Amount;

            ConsoleHelper.Demo(
                $"id={id}：兩種寫法相等嗎？",
                fluent.Equals(query),
                $"方法鏈={Show(fluent)}，查詢語法={Show(query)}");
        }

        ConsoleHelper.Good("兩種寫法可以在同一個專案裡混用，沒有效能差異");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    private sealed record User(int Id, string Name);

    private sealed record Order(int Id, decimal Amount);

    private static readonly User[] Users = [new(1, "Alice"), new(2, "Bob")];

    private static readonly Dictionary<string, decimal> Prices = new()
    {
        ["keyboard"] = 2500m,
        ["mouse"] = 890m
    };

    private static readonly Dictionary<string, int> Quantities = new()
    {
        ["keyboard"] = 2
        // mouse 刻意沒有數量資料，用來示範短路
    };

    private static readonly Dictionary<string, decimal> Discounts = new()
    {
        ["keyboard"] = 0.1m
    };

    /// <summary>查詢單價。</summary>
    private static Option<decimal> GetPrice(string sku) => Prices.GetValueOrNone(sku);

    /// <summary>查詢庫存數量。</summary>
    private static Option<int> GetQuantity(string sku) => Quantities.GetValueOrNone(sku);

    /// <summary>查詢折扣。</summary>
    private static Option<decimal> GetDiscount(string sku) => Discounts.GetValueOrNone(sku);

    /// <summary>查詢使用者，回傳 Result。</summary>
    private static Result<User, string> FindUser(int id)
    {
        foreach (var user in Users)
        {
            if (user.Id == id)
            {
                return user;
            }
        }

        return $"找不到 Id={id} 的使用者";
    }

    /// <summary>查詢最新訂單。Bob（id=2）沒有訂單。</summary>
    private static Result<Order, string> FindLatestOrder(int userId)
        => userId == 1
            ? new Order(101, 3350m)
            : $"使用者 {userId} 沒有任何訂單";

    /// <summary>把 Result 轉成簡短字串，純粹為了範例輸出。</summary>
    private static string Show(Result<decimal, string> result)
        => result.Match(ok: v => $"Ok({v})", err: e => "Err");
}
