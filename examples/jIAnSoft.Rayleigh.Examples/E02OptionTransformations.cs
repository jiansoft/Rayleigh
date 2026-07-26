using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E02：轉換 Option 裡的值 —— Map、Filter、Bind、Flatten。
/// </summary>
/// <remarks>
/// <para><b>核心觀念：不要「拆開再包回去」</b></para>
/// <para>
/// 新手最常寫出這種程式碼：
/// </para>
/// <code>
/// // 不好的寫法：先判斷、再取值、再包回去
/// if (option.IsSome)
/// {
///     var value = option.Unwrap();
///     return Option&lt;string&gt;.Some(value.ToString());
/// }
/// return Option&lt;string&gt;.None;
/// </code>
/// <para>
/// 這整段可以縮成一行 <c>option.Map(v =&gt; v.ToString())</c>。
/// 本模組要教的就是：<b>讓值待在 Option 裡面，直接對它做轉換。</b>
/// </para>
/// <para><b>三個方法怎麼選？這是最重要的判斷</b></para>
/// <list type="table">
///   <item>
///     <term>Map</term>
///     <description>你的轉換函式回傳<b>普通的值</b>（一定會成功）→ 用 Map</description>
///   </item>
///   <item>
///     <term>Bind</term>
///     <description>你的轉換函式回傳<b>另一個 Option</b>（可能失敗）→ 用 Bind</description>
///   </item>
///   <item>
///     <term>Filter</term>
///     <description>你不想改變值，只想在不符條件時變成 None → 用 Filter</description>
///   </item>
/// </list>
/// </remarks>
public static class E02OptionTransformations
{
    /// <summary>執行 E02 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E02", "轉換 Option 裡的值：Map / Filter / Bind / Flatten");

        MapBasics();
        FilterBasics();
        BindBasics();
        MapVsBind();
        FlattenBasics();
        MapOrFamily();
        ChainingThemTogether();

        ConsoleHelper.Summary(
            "Map：轉換函式回傳普通值，Option<int> -> Option<string>",
            "Bind：轉換函式回傳 Option，用來串接「也可能失敗」的下一步",
            "Filter：值不符條件就變成 None，型別不變",
            "選錯會怎樣？該用 Bind 卻用 Map，你會拿到 Option<Option<T>>",
            "None 進去，永遠是 None 出來——你的轉換函式根本不會被執行");
    }

    /// <summary>
    /// 第 1 節：Map —— 最常用的轉換。
    /// </summary>
    private static void MapBasics()
    {
        ConsoleHelper.Section("1", "Map：轉換裡面的值（轉換一定會成功時用這個）");

        ConsoleHelper.Explain(
            "Map 的規則只有兩條：\n" +
            "  1. 如果是 Some(x)，就把 x 丟進你的函式，把結果重新包成 Some\n" +
            "  2. 如果是 None，什麼都不做，直接回傳 None（你的函式不會被執行）");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // 最簡單的例子：數字乘以 2。
        ConsoleHelper.Demo("Some(42).Map(x => x * 2)", some.Map(x => x * 2));

        // Map 可以改變型別：Option<int> 變成 Option<string>。
        ConsoleHelper.Demo("Some(42).Map(x => $\"值是 {x}\")", some.Map(x => $"值是 {x}"));

        // None 的情況：函式完全不會執行，直接得到 None。
        ConsoleHelper.Demo("None.Map(x => x * 2)", none.Map(x => x * 2), "函式沒有被呼叫");

        // 用一個計數器實際證明「None 時函式不會執行」。
        var callCount = 0;
        _ = none.Map(x =>
        {
            callCount++; // 這行永遠不會跑到
            return x;
        });
        ConsoleHelper.Demo("None.Map(...) 之後，函式被呼叫幾次？", callCount, "答案是 0 次");

        Console.WriteLine();
        ConsoleHelper.Explain("實務用法：從物件取出某個屬性。");
        var user = Option<User>.Some(new User(1, "Alice"));
        ConsoleHelper.Demo("user.Map(u => u.Name)", user.Map(u => u.Name));
        ConsoleHelper.Demo("user.Map(u => u.Name.Length)", user.Map(u => u.Name.Length));
    }

    /// <summary>
    /// 第 2 節：Filter —— 加上條件。
    /// </summary>
    private static void FilterBasics()
    {
        ConsoleHelper.Section("2", "Filter：值不符合條件就變成 None");

        ConsoleHelper.Explain(
            "Filter 不會改變值，也不會改變型別。\n" +
            "它只做一件事：如果值不符合你的條件，就把 Some 降級成 None。");

        var some = Option<int>.Some(42);

        ConsoleHelper.Demo("Some(42).Filter(x => x > 40)", some.Filter(x => x > 40), "42 > 40 成立，保持原樣");
        ConsoleHelper.Demo("Some(42).Filter(x => x > 100)", some.Filter(x => x > 100), "不成立，變成 None");
        ConsoleHelper.Demo("None.Filter(x => true)", Option<int>.None.Filter(_ => true), "本來就是 None");

        Console.WriteLine();
        ConsoleHelper.Explain("實務用法：把「不合格的資料」當成沒有資料。");

        var ages = new[] { 25, -5, 200 };
        foreach (var age in ages)
        {
            // 年齡必須介於 0 到 130 之間才算有效。
            var valid = Option<int>.Some(age).Filter(a => a is >= 0 and <= 130);
            ConsoleHelper.Demo($"Some({age}).Filter(合理年齡)", valid);
        }

        ConsoleHelper.Tip("Filter 等同於 LINQ 的 Where，兩者可以互換使用");
    }

    /// <summary>
    /// 第 3 節：Bind —— 串接可能失敗的下一步。
    /// </summary>
    private static void BindBasics()
    {
        ConsoleHelper.Section("3", "Bind：串接「下一步也可能沒有結果」的操作");

        ConsoleHelper.Explain(
            "當你的轉換函式本身就回傳 Option 時（例如「用 Id 查使用者」），\n" +
            "就要用 Bind 而不是 Map。Bind 會幫你把結果「攤平」，不會多包一層。");

        // FindUser 和 GetEmail 都定義在檔案下方，兩者都回傳 Option。
        Console.WriteLine();
        ConsoleHelper.Explain("情境：先用 Id 查使用者，再從使用者查 Email。兩步都可能查不到。");

        // 情況 A：兩步都成功。
        ConsoleHelper.Demo("FindUser(1).Bind(GetEmail)", FindUser(1).Bind(GetEmail), "兩步都成功");

        // 情況 B：第一步就失敗，第二步不會執行。
        ConsoleHelper.Demo("FindUser(999).Bind(GetEmail)", FindUser(999).Bind(GetEmail), "查無此人，GetEmail 不會執行");

        // 情況 C：第一步成功，但第二步失敗。
        ConsoleHelper.Demo("FindUser(2).Bind(GetEmail)", FindUser(2).Bind(GetEmail), "Bob 沒有留 Email");

        Console.WriteLine();
        ConsoleHelper.Explain("Bind 可以一直串下去，任何一步是 None，整條鏈就是 None。");
        ConsoleHelper.Demo(
            "FindUser(1).Bind(GetEmail).Bind(GetDomain)",
            FindUser(1).Bind(GetEmail).Bind(GetDomain));
    }

    /// <summary>
    /// 第 4 節：Map 和 Bind 選錯會怎樣 —— 這是新手最容易踩的坑。
    /// </summary>
    private static void MapVsBind()
    {
        ConsoleHelper.Section("4", "重要：Map 和 Bind 選錯會發生什麼事");

        ConsoleHelper.Explain(
            "GetEmail 這個函式回傳的是 Option<string>。\n" +
            "如果你對它用 Map 而不是 Bind，會發生什麼？");

        // 用 Map：因為 GetEmail 回傳 Option<string>，Map 會把它「再包一層」。
        // 結果型別變成 Option<Option<string>> —— 幾乎不是你要的東西。
        Option<Option<string>> nested = FindUser(1).Map(GetEmail);
        ConsoleHelper.Demo("FindUser(1).Map(GetEmail)", nested);
        ConsoleHelper.Bad("型別變成 Option<Option<string>>，包了兩層，後續很難處理");

        // 用 Bind：正確攤平成一層。
        Option<string> flat = FindUser(1).Bind(GetEmail);
        ConsoleHelper.Demo("FindUser(1).Bind(GetEmail)", flat);
        ConsoleHelper.Good("型別是 Option<string>，只有一層，這才是你要的");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "怎麼記？看你的函式回傳什麼：\n" +
            "  回傳普通值（string、int、User...）      -> 用 Map\n" +
            "  回傳 Option<某型別>                     -> 用 Bind");
    }

    /// <summary>
    /// 第 5 節：Flatten —— 萬一真的包了兩層怎麼辦。
    /// </summary>
    private static void FlattenBasics()
    {
        ConsoleHelper.Section("5", "Flatten：把 Option<Option<T>> 壓成 Option<T>");

        ConsoleHelper.Explain(
            "如果你手上已經有一個雙層的 Option（例如上一節誤用 Map 的結果），\n" +
            "Flatten 可以把它壓平。規則是「兩層都要有值才有值」。");

        var bothSome = Option<Option<int>>.Some(Option<int>.Some(42));
        var innerNone = Option<Option<int>>.Some(Option<int>.None);
        var outerNone = Option<Option<int>>.None;

        ConsoleHelper.Demo("Some(Some(42)).Flatten()", bothSome.Flatten(), "兩層都有值");
        ConsoleHelper.Demo("Some(None).Flatten()", innerNone.Flatten(), "內層沒值");
        ConsoleHelper.Demo("None.Flatten()", outerNone.Flatten(), "外層就沒值");

        ConsoleHelper.Tip("x.Map(f).Flatten() 完全等同於 x.Bind(f)——直接用 Bind 比較好");
    }

    /// <summary>
    /// 第 6 節：MapOr / MapOrElse —— 轉換的同時給預設值。
    /// </summary>
    private static void MapOrFamily()
    {
        ConsoleHelper.Section("6", "MapOr / MapOrElse：轉換並直接得到最終結果（不再是 Option）");

        ConsoleHelper.Explain(
            "Map 的結果還是 Option，你之後還得處理它。\n" +
            "MapOr 則是「轉換 + 給預設值」一次完成，直接吐出普通型別。");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // MapOr(預設值, 轉換函式)
        // 注意參數順序：預設值在前，轉換函式在後。
        ConsoleHelper.Demo("Some(42).MapOr(\"沒有\", x => $\"有 {x}\")", some.MapOr("沒有", x => $"有 {x}"));
        ConsoleHelper.Demo("None.MapOr(\"沒有\", x => $\"有 {x}\")", none.MapOr("沒有", x => $"有 {x}"));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "MapOrElse 則是把「預設值」也改成函式。\n" +
            "當預設值的計算成本很高（例如要查資料庫）時用這個，沒用到就不會執行。");

        var expensiveCallCount = 0;
        string ExpensiveDefault()
        {
            expensiveCallCount++;
            return "（很貴的預設值）";
        }

        ConsoleHelper.Demo("Some(42).MapOrElse(貴的預設值, x => ...)", some.MapOrElse(ExpensiveDefault, x => $"有 {x}"));
        ConsoleHelper.Demo("目前貴的函式被呼叫幾次？", expensiveCallCount, "0 次——有值時根本不需要預設值");

        ConsoleHelper.Demo("None.MapOrElse(貴的預設值, x => ...)", none.MapOrElse(ExpensiveDefault, x => $"有 {x}"));
        ConsoleHelper.Demo("現在呢？", expensiveCallCount, "1 次——這時才真的需要");

        ConsoleHelper.Good("凡是名稱帶 OrElse 的方法，都是「需要時才計算」的惰性版本");
    }

    /// <summary>
    /// 第 7 節：把它們串起來，這才是真實的寫法。
    /// </summary>
    private static void ChainingThemTogether()
    {
        ConsoleHelper.Section("7", "串起來看：一條完整的處理鏈");

        ConsoleHelper.Explain(
            "情境：查使用者 -> 取 Email -> 只要公司信箱 -> 轉成大寫 -> 沒有就顯示提示文字。\n" +
            "注意整段完全沒有 if、沒有 null 檢查、沒有 try/catch。");

        foreach (var id in new[] { 1, 2, 999 })
        {
            var result = FindUser(id)                              // Option<User>
                .Bind(GetEmail)                                    // Option<string>：可能沒有 Email
                .Filter(email => email.EndsWith("@corp.com"))       // 只要公司信箱
                .Map(email => email.ToUpperInvariant())             // 轉大寫
                .UnwrapOr("(無可用的公司信箱)");                     // 取值，沒有就給預設文字

            ConsoleHelper.Demo($"處理 id={id}", result);
        }

        Console.WriteLine();
        ConsoleHelper.Explain(
            "三個 id 分別代表三種情況：\n" +
            "  1   -> 一路暢通，拿到大寫的公司信箱\n" +
            "  2   -> Bob 沒有 Email，在 Bind 那步就變成 None\n" +
            "  999 -> 查無此人，在第一步就是 None");

        ConsoleHelper.Good("鏈中任何一步變成 None，後面所有步驟都會自動跳過");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>範例用的使用者資料。Email 可能是 null，模擬真實資料庫欄位。</summary>
    private sealed record User(int Id, string Name, string? Email = null);

    private static readonly User[] Users =
    [
        new(1, "Alice", "alice@corp.com"),
        new(2, "Bob"),                       // Bob 沒有留 Email
        new(3, "Carol", "carol@gmail.com")   // Carol 用的是個人信箱
    ];

    /// <summary>依 Id 尋找使用者，找不到回傳 None。</summary>
    private static Option<User> FindUser(int id)
    {
        foreach (var user in Users)
        {
            if (user.Id == id)
            {
                return user;
            }
        }

        return Option.None;
    }

    /// <summary>
    /// 取得使用者的 Email。注意<b>回傳型別是 Option</b>，所以呼叫端要用 Bind 而不是 Map。
    /// </summary>
    private static Option<string> GetEmail(User user) => user.Email.ToOption();

    /// <summary>從 Email 取出網域部分，格式不對就回傳 None。</summary>
    private static Option<string> GetDomain(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex >= 0 && atIndex < email.Length - 1
            ? Option<string>.Some(email[(atIndex + 1)..])
            : Option.None;
    }
}
