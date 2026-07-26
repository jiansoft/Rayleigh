using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E01：Option 入門 —— 這是整套範例的起點。
/// </summary>
/// <remarks>
/// <para><b>看這個模組之前，你需要知道的事</b></para>
/// <para>
/// C# 有一個存在幾十年的老問題：<c>null</c>。當一個方法回傳 <c>string</c> 時，
/// 你無法從型別看出「它可能回傳 null 嗎？」——只能去翻文件、翻原始碼，或是等它在正式環境炸掉。
/// </para>
/// <para>
/// <c>Option&lt;T&gt;</c> 就是用來解決這件事的。它只有兩種狀態：
/// </para>
/// <list type="bullet">
///   <item><description><b>Some(值)</b>：有值，而且保證不是 null</description></item>
///   <item><description><b>None</b>：沒有值</description></item>
/// </list>
/// <para>
/// 當一個方法回傳 <c>Option&lt;User&gt;</c>，呼叫端<b>一看型別就知道</b>「這可能找不到」，
/// 而且編譯器會逼你處理「沒有值」的情況。這就是它的全部價值。
/// </para>
/// </remarks>
public static class E01OptionBasics
{
    /// <summary>執行 E01 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E01", "Option 入門：什麼是 Option，以及怎麼建立它");

        WhyOption();
        CreatingOptions();
        CheckingState();
        ZeroAndEmptyAreValid();
        ComparingOptions();

        ConsoleHelper.Summary(
            "Option<T> 只有兩種狀態：Some(值) 或 None",
            "Some 裡面的值保證不是 null——傳 null 進去會直接拋 ArgumentNullException",
            "Some(0)、Some(\"\") 都是合法的「有值」，不要跟 None 搞混",
            "建立方式：Option<T>.Some(x)、Option<T>.None、隱式轉換、Option.None 萬用標記",
            "下一步請看 E02，學習如何安全地把值取出來");
    }

    /// <summary>
    /// 第 1 節：先講清楚「為什麼需要 Option」，而不是一上來就教 API。
    /// </summary>
    private static void WhyOption()
    {
        ConsoleHelper.Section("1", "為什麼需要 Option？先看 null 的問題");

        ConsoleHelper.Explain(
            "假設有一個方法叫 FindUser(id)，回傳型別是 User。\n" +
            "問題來了：找不到使用者的時候，它回傳什麼？\n" +
            "  - 回傳 null？  那呼叫端忘記檢查就會 NullReferenceException\n" +
            "  - 拋例外？    但「找不到」是很正常的情況，不算「例外」\n" +
            "光看 User 這個型別，你完全看不出來會發生哪一種。");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "改成回傳 Option<User> 之後，型別本身就說明了一切：\n" +
            "「這個方法可能找不到東西，你必須處理這種情況。」");

        // 這兩個方法定義在本檔案最下方，模擬「查得到」與「查不到」兩種情況。
        var found = FindUserById(1);
        var notFound = FindUserById(999);

        ConsoleHelper.Demo("FindUserById(1)", found, "查到了，包在 Some 裡面");
        ConsoleHelper.Demo("FindUserById(999)", notFound, "查不到，回傳 None（不是 null！）");

        ConsoleHelper.Good("回傳型別 Option<User> 讓「可能找不到」變成型別的一部分");
    }

    /// <summary>
    /// 第 2 節：建立 Option 的四種方式。
    /// </summary>
    private static void CreatingOptions()
    {
        ConsoleHelper.Section("2", "建立 Option 的四種方式");

        // -- 方式 1：靜態工廠方法（最明確，推薦新手先用這種）--------------------
        // Option<T>.Some(值) 建立「有值」的 Option。
        // 注意泛型參數 <int> 要自己寫出來。
        var someInt = Option<int>.Some(42);
        ConsoleHelper.Demo("Option<int>.Some(42)", someInt);

        // Option<T>.None 建立「沒有值」的 Option。
        // 它是一個屬性，不是方法，所以後面沒有括號。
        var noneInt = Option<int>.None;
        ConsoleHelper.Demo("Option<int>.None", noneInt);

        // -- 方式 2：型別推斷版工廠（少打一個泛型參數）--------------------------
        // Option.Some(x) 會自動從參數推斷型別，適合型別名稱很長的時候。
        var inferred = Option.Some("hello");
        ConsoleHelper.Demo("Option.Some(\"hello\")", inferred, "型別由參數自動推斷為 Option<string>");

        // -- 方式 3：隱式轉換（最簡潔）----------------------------------------
        // 只要左邊宣告成 Option<T>，右邊直接給值，編譯器會自動包起來。
        // 這在 return 的時候特別好用：return 42; 就等於 return Option<int>.Some(42);
        Option<int> implicitSome = 42;
        ConsoleHelper.Demo("Option<int> x = 42;", implicitSome, "自動包成 Some(42)");

        // Option.None 是一個「萬用 None 標記」，可以指派給任何 Option<T>。
        // 好處是你不用寫出那串很長的泛型型別名稱。
        Option<Dictionary<string, List<int>>> universalNone = Option.None;
        ConsoleHelper.Demo("Option<很長的型別> x = Option.None;", universalNone, "不必重複寫一次長型別名稱");

        // -- 方式 4：從既有的可為 null 的值轉換（最常用於整合舊程式碼）----------
        // ToOption() 是擴充方法，會把 null 轉成 None、非 null 轉成 Some。
        string? nameFromDatabase = "Alice";
        string? missingName = null;
        ConsoleHelper.Demo("\"Alice\".ToOption()", nameFromDatabase.ToOption());
        ConsoleHelper.Demo("((string?)null).ToOption()", missingName.ToOption(), "null 自動變成 None");

        // 對可為 null 的實值型別（int?、DateTime? 之類）也適用。
        int? age = 30;
        int? unknownAge = null;
        ConsoleHelper.Demo("((int?)30).ToOption()", age.ToOption());
        ConsoleHelper.Demo("((int?)null).ToOption()", unknownAge.ToOption());

        ConsoleHelper.Tip("接手舊有 API 時，第一步就是用 .ToOption() 把 null 擋在邊界上");

        // -- 重要：Some 不接受 null -------------------------------------------
        // 如果放行 Some(null)，Option 就失去意義了——你還是可能拿到 null。
        // 所以這裡會直接拋例外，強迫你改用 None 表達「沒有值」。
        ConsoleHelper.Boom("Option<string>.Some(null!)", () => Option<string>.Some(null!));
        ConsoleHelper.Good("想表達「沒有值」請用 None，不要試圖把 null 塞進 Some");
    }

    /// <summary>
    /// 第 3 節：怎麼判斷一個 Option 有沒有值。
    /// </summary>
    private static void CheckingState()
    {
        ConsoleHelper.Section("3", "判斷有沒有值：IsSome / IsNone / IsSomeAnd / Contains");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // IsSome / IsNone 是最基本的兩個布林屬性，互為相反。
        ConsoleHelper.Demo("Some(42).IsSome", some.IsSome);
        ConsoleHelper.Demo("Some(42).IsNone", some.IsNone);
        ConsoleHelper.Demo("None.IsSome", none.IsSome);
        ConsoleHelper.Demo("None.IsNone", none.IsNone);

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Contains 和 IsSomeAnd 讓你「一步完成」判斷，不用先確認有值再取出來比較。");

        // Contains：有值 而且 值等於指定的東西。
        ConsoleHelper.Demo("Some(42).Contains(42)", some.Contains(42));
        ConsoleHelper.Demo("Some(42).Contains(99)", some.Contains(99), "有值，但值不對");
        ConsoleHelper.Demo("None.Contains(42)", none.Contains(42), "沒有值，一律 false");

        // IsSomeAnd：有值 而且 值符合你給的條件。
        // 沒有值的時候一律回傳 false，不會執行你傳進去的判斷式。
        ConsoleHelper.Demo("Some(42).IsSomeAnd(x => x > 40)", some.IsSomeAnd(x => x > 40));
        ConsoleHelper.Demo("Some(42).IsSomeAnd(x => x > 100)", some.IsSomeAnd(x => x > 100));
        ConsoleHelper.Demo("None.IsSomeAnd(x => true)", none.IsSomeAnd(_ => true), "條件式根本不會被執行");

        ConsoleHelper.Tip("if (opt.IsSomeAnd(u => u.IsActive)) 比「先檢查有值、再取出、再判斷」乾淨得多");
    }

    /// <summary>
    /// 第 4 節：新手最常搞混的地方 —— 0 和空字串是「有值」。
    /// </summary>
    private static void ZeroAndEmptyAreValid()
    {
        ConsoleHelper.Section("4", "容易搞混：Some(0) 和 Some(\"\") 都是「有值」");

        ConsoleHelper.Explain(
            "很多語言會把 0、空字串、空陣列視為「假值」而當成不存在。\n" +
            "Option 完全不是這樣——它只在意「你有沒有給我一個值」，不在意那個值長什麼樣。");

        var zero = Option<int>.Some(0);
        var emptyString = Option<string>.Some("");
        var emptyList = Option<List<int>>.Some([]);

        ConsoleHelper.Demo("Option<int>.Some(0).IsSome", zero.IsSome, "0 是一個合法的值");
        ConsoleHelper.Demo("Option<string>.Some(\"\").IsSome", emptyString.IsSome, "空字串也是");
        ConsoleHelper.Demo("Option<List<int>>.Some([]).IsSome", emptyList.IsSome, "空清單也是");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "這個特性正是 Option 比 null 或預設值更精確的地方。\n" +
            "以「查詢某人的帳戶餘額」為例：");

        ConsoleHelper.Compare(
            "回傳 int，找不到就給 0", "0（餘額是 0？還是查不到？分不出來）",
            "回傳 Option<int>", $"{Option<int>.Some(0)} 或 {Option<int>.None}（清清楚楚）");

        ConsoleHelper.Good("餘額為 0 是 Some(0)；查不到這個帳戶才是 None——兩者意義完全不同");
    }

    /// <summary>
    /// 第 5 節：Option 之間怎麼比較。
    /// </summary>
    private static void ComparingOptions()
    {
        ConsoleHelper.Section("5", "比較兩個 Option");

        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);
        var c = Option<int>.Some(99);
        var none1 = Option<int>.None;
        var none2 = Option<int>.None;

        ConsoleHelper.Explain("相等比較：兩邊都有值就比值，兩邊都沒值就算相等。");
        ConsoleHelper.Demo("Some(42) == Some(42)", a == b);
        ConsoleHelper.Demo("Some(42) == Some(99)", a == c);
        ConsoleHelper.Demo("None == None", none1 == none2, "兩個 None 視為相等");
        ConsoleHelper.Demo("Some(42) == None", a == none1);

        Console.WriteLine();
        ConsoleHelper.Explain(
            "排序比較：規則是「None 比任何 Some 都小」。\n" +
            "這讓你可以直接對一堆 Option 排序，沒有值的會排在最前面。");

        ConsoleHelper.Demo("None < Some(42)", none1 < a);
        ConsoleHelper.Demo("Some(42) < Some(99)", a < c);

        // 實際排序一次給你看。
        var mixed = new[] { Option<int>.Some(30), Option<int>.None, Option<int>.Some(10) };
        Array.Sort(mixed);
        ConsoleHelper.Demo("排序後", string.Join(", ", mixed), "None 排最前面");

        ConsoleHelper.Tip("因為有實作 IEquatable/IComparable，Option 也能直接當 Dictionary 的 Key");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>範例用的使用者資料。<c>record</c> 讓 ToString() 自動產生好讀的輸出。</summary>
    private sealed record User(int Id, string Name);

    /// <summary>假的資料來源，模擬資料庫。</summary>
    private static readonly User[] Users = [new(1, "Alice"), new(2, "Bob")];

    /// <summary>
    /// 依 Id 尋找使用者。這就是「回傳 Option 的方法」長什麼樣子。
    /// </summary>
    /// <param name="id">要尋找的使用者 Id。</param>
    /// <returns>找到則為 <c>Some(user)</c>；找不到則為 <c>None</c>。</returns>
    private static Option<User> FindUserById(int id)
    {
        foreach (var user in Users)
        {
            if (user.Id == id)
            {
                // 這裡用了隱式轉換：直接 return user，編譯器自動包成 Some。
                return user;
            }
        }

        // 找不到就回傳 None——注意這裡不是 return null。
        return Option.None;
    }
}
