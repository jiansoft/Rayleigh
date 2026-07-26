using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E04：Result 入門 —— 當你不只想知道「有沒有」，還想知道「為什麼沒有」。
/// </summary>
/// <remarks>
/// <para><b>Option 不夠用的時候</b></para>
/// <para>
/// <c>Option</c> 只能告訴你「有值」或「沒值」。但很多時候「沒值」的原因很重要：
/// 是使用者不存在？還是帳號被停用？還是資料庫連不上？
/// </para>
/// <para>
/// <c>Result&lt;T, TE&gt;</c> 有兩個型別參數：<c>T</c> 是成功時的值，<c>TE</c> 是失敗時的<b>錯誤資訊</b>。
/// 它也只有兩種狀態：
/// </para>
/// <list type="bullet">
///   <item><description><b>Ok(值)</b>：成功</description></item>
///   <item><description><b>Err(錯誤)</b>：失敗，而且帶著失敗的原因</description></item>
/// </list>
/// <para><b>那為什麼不用 Exception？</b></para>
/// <para>
/// Exception 適合「真的意外」的狀況（記憶體不足、程式邏輯錯誤）。
/// 但「使用者輸入的 Email 格式不對」根本不是意外，那是每天都會發生幾千次的正常情況。
/// 用 Exception 處理這種事情，效能代價非常高——E12 會用實測數字給你看差多少。
/// </para>
/// </remarks>
public static class E04ResultBasics
{
    /// <summary>執行 E04 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E04", "Result 入門：帶著「失敗原因」的回傳值");

        WhyResult();
        CreatingResults();
        CheckingState();
        ChoosingErrorType();
        TheUninitializedTrap();

        ConsoleHelper.Summary(
            "Result<T, TE>：Ok(值) 代表成功，Err(錯誤) 代表失敗且帶原因",
            "錯誤型別建議用 enum 或 record，不要只用 string",
            "建立方式：Result<T,TE>.Ok(x) / .Err(e)、隱式轉換、new Ok<T>() / new Err<TE>()",
            "陷阱：T 和 TE 不能是同一個型別，否則隱式轉換會編譯失敗（CS0457）",
            "陷阱：default(Result<T,TE>) 不是合法的值，碰它會直接拋例外");
    }

    /// <summary>
    /// 第 1 節：為什麼需要 Result。
    /// </summary>
    private static void WhyResult()
    {
        ConsoleHelper.Section("1", "為什麼需要 Result？Option 哪裡不夠用");

        ConsoleHelper.Explain(
            "情境：驗證使用者註冊表單。可能的失敗原因有好幾種——\n" +
            "名字空白、Email 格式錯誤、年齡不合理...\n" +
            "如果用 Option，全部都只會得到一個 None，你根本不知道要跟使用者說什麼。");

        Console.WriteLine();
        ConsoleHelper.Explain("改用 Result，每種失敗都帶著明確的原因：");

        // ValidateAge 定義在檔案下方，回傳 Result<int, ValidationError>。
        foreach (var age in new[] { 25, -5, 200 })
        {
            var result = ValidateAge(age);
            ConsoleHelper.Demo($"ValidateAge({age})", result);
        }

        ConsoleHelper.Good("呼叫端可以針對不同的錯誤，顯示不同的訊息給使用者");
    }

    /// <summary>
    /// 第 2 節：建立 Result 的方式。
    /// </summary>
    private static void CreatingResults()
    {
        ConsoleHelper.Section("2", "建立 Result 的三種方式");

        // -- 方式 1：靜態工廠方法（最明確）-----------------------------------
        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("出事了");
        ConsoleHelper.Demo("Result<int, string>.Ok(42)", ok);
        ConsoleHelper.Demo("Result<int, string>.Err(\"出事了\")", err);

        // -- 方式 2：隱式轉換（最簡潔，但有限制）------------------------------
        ConsoleHelper.Explain(
            "只要 T 和 TE 是不同型別，就可以直接 return 值或錯誤，編譯器會自動判斷。");

        // Divide 定義在下方，示範隱式轉換的寫法。
        ConsoleHelper.Demo("Divide(10, 2)", Divide(10, 2));
        ConsoleHelper.Demo("Divide(10, 0)", Divide(10, 0));

        // -- 方式 3：Ok<T> / Err<TE> 包裹記錄（意圖最明顯）--------------------
        Console.WriteLine();
        ConsoleHelper.Explain(
            "第三種寫法是用 new Ok<T>(值) 和 new Err<TE>(錯誤)。\n" +
            "它比隱式轉換多打幾個字，但意圖非常清楚，而且——\n" +
            "當 T 和 TE 剛好是同一個型別時，這是唯一可用的簡潔寫法（原因見第 4 節）。");

        ConsoleHelper.Demo("ParseName(\"Alice\")", ParseName("Alice"));
        ConsoleHelper.Demo("ParseName(\"\")", ParseName(""));
    }

    /// <summary>
    /// 第 3 節：判斷成功或失敗。
    /// </summary>
    private static void CheckingState()
    {
        ConsoleHelper.Section("3", "判斷成功或失敗：IsOk / IsErr / Contains / IsOkAnd");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("boom");

        ConsoleHelper.Demo("Ok(42).IsOk", ok.IsOk);
        ConsoleHelper.Demo("Ok(42).IsErr", ok.IsErr);
        ConsoleHelper.Demo("Err(\"boom\").IsOk", err.IsOk);
        ConsoleHelper.Demo("Err(\"boom\").IsErr", err.IsErr);

        Console.WriteLine();
        ConsoleHelper.Explain("和 Option 一樣，也有「一步完成」的判斷方法。");

        // Contains：成功 而且 值等於指定的東西。
        ConsoleHelper.Demo("Ok(42).Contains(42)", ok.Contains(42));
        ConsoleHelper.Demo("Err(...).Contains(42)", err.Contains(42), "失敗時一律 false");

        // ContainsErr：失敗 而且 錯誤等於指定的東西。
        ConsoleHelper.Demo("Err(\"boom\").ContainsErr(\"boom\")", err.ContainsErr("boom"));

        // IsOkAnd / IsErrAnd：帶條件的判斷。
        ConsoleHelper.Demo("Ok(42).IsOkAnd(x => x > 40)", ok.IsOkAnd(x => x > 40));
        ConsoleHelper.Demo("Err(\"boom\").IsErrAnd(e => e.Length > 3)", err.IsErrAnd(e => e.Length > 3));

        ConsoleHelper.Tip("IsErrAnd 很適合用來做「只重試逾時錯誤」這類判斷");
    }

    /// <summary>
    /// 第 4 節：錯誤型別該怎麼選 —— 這個決定會影響整個專案。
    /// </summary>
    private static void ChoosingErrorType()
    {
        ConsoleHelper.Section("4", "重要：錯誤型別（TE）該用什麼？");

        ConsoleHelper.Explain(
            "很多人一開始會用 string 當錯誤型別，因為最方便。但這樣有兩個問題：");

        Console.WriteLine();
        ConsoleHelper.Bad("問題一：呼叫端只能比對字串，打錯字編譯器也不會提醒");
        ConsoleHelper.Bad("問題二：如果 T 也是 string，隱式轉換會直接編譯失敗（CS0457）");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "為什麼會編譯失敗？因為 Result 同時定義了「從 T 轉換」和「從 TE 轉換」兩個隱式轉換。\n" +
            "當 T 和 TE 都是 string 時，這兩個轉換的簽章完全一樣，編譯器不知道該選哪個。\n" +
            "\n" +
            "  Result<string, string> Bad(bool ok) => ok ? \"值\" : \"錯誤\";   // 編譯失敗！\n" +
            "\n" +
            "解法一：改用明確的工廠方法或包裹記錄（下面示範）\n" +
            "解法二（更好）：為錯誤定義專用型別");

        // 解法一：用包裹記錄，即使 T 和 TE 同型別也能運作。
        ConsoleHelper.Demo("用 new Ok/Err 包裹記錄", ParseName("Alice"), "T 和 TE 同為 string 也沒問題");

        Console.WriteLine();
        ConsoleHelper.Explain("解法二示範：用 enum 當錯誤型別，型別安全又好讀。");
        ConsoleHelper.Demo("ValidateAge(-5)", ValidateAge(-5));
        ConsoleHelper.Demo("ValidateAge(-5).UnwrapErr()", ValidateAge(-5).UnwrapErr(), "拿到的是 enum，不是字串");

        ConsoleHelper.Good("小專案用 enum；需要附帶細節時用 record，例如 record ValidationError(string Field, string Reason)");
    }

    /// <summary>
    /// 第 5 節：未初始化的 Result 陷阱。
    /// </summary>
    private static void TheUninitializedTrap()
    {
        ConsoleHelper.Section("5", "陷阱：default(Result<T,TE>) 不是合法的值");

        ConsoleHelper.Explain(
            "Result 是 struct，而 C# 允許任何 struct 被「零值初始化」——\n" +
            "陣列的元素、沒有指派的欄位，都會是 default 狀態。\n" +
            "但一個沒經過 Ok() 或 Err() 建立的 Result，它既不是成功也不是失敗，根本沒有意義。");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Rayleigh 的做法是把這種狀態「毒化」：任何想讀取內容的操作都會直接拋例外，\n" +
            "讓問題在第一時間爆出來，而不是安靜地傳播下去。");

        var uninitialized = default(Result<int, ValidationError>);

        ConsoleHelper.Demo("default(Result<...>).ToString()", uninitialized.ToString(), "明確標示為未初始化");
        ConsoleHelper.Boom("default(Result<...>).Unwrap()", () => uninitialized.Unwrap());
        ConsoleHelper.Boom("default(Result<...>).UnwrapErr()", () => uninitialized.UnwrapErr());
        ConsoleHelper.Boom("default(Result<...>).Map(x => x)", () => uninitialized.Map(x => x));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "特別注意：這個保護對「錯誤型別是 enum」的情況也有效。\n" +
            "ValidationError.NameRequired 的底層值是 0，和 default 一模一樣，\n" +
            "但 Rayleigh 仍然能分辨兩者：");

        var legitError = Result<int, ValidationError>.Err(ValidationError.NameRequired);
        ConsoleHelper.Demo("Err(NameRequired) 是合法的失敗", legitError);
        ConsoleHelper.Demo("它等於 default 嗎？", legitError.Equals(uninitialized), "不相等，兩者能明確區分");

        ConsoleHelper.Good("永遠用 Ok() / Err() 或隱式轉換建立 Result，不要依賴 default");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>
    /// 範例用的驗證錯誤。
    /// </summary>
    /// <remarks>
    /// 用 enum 當錯誤型別的好處：呼叫端可以用 switch 窮舉處理，
    /// 漏掉某個成員時編譯器會提醒（搭配 switch 運算式）。
    /// </remarks>
    private enum ValidationError
    {
        NameRequired,   // 底層值是 0，刻意用來示範第 5 節的陷阱
        AgeTooSmall,
        AgeTooLarge
    }

    /// <summary>
    /// 驗證年齡。示範用 enum 當錯誤型別，並使用隱式轉換簡化寫法。
    /// </summary>
    private static Result<int, ValidationError> ValidateAge(int age)
    {
        // 因為 int 和 ValidationError 是不同型別，
        // 編譯器能自動判斷 return 的是「成功值」還是「錯誤」。
        if (age < 0)
        {
            return ValidationError.AgeTooSmall;
        }

        if (age > 130)
        {
            return ValidationError.AgeTooLarge;
        }

        return age;
    }

    /// <summary>
    /// 除法。示範最基本的隱式轉換寫法。
    /// </summary>
    private static Result<int, string> Divide(int a, int b)
    {
        if (b == 0)
        {
            return "除數不能為零";   // 自動變成 Err
        }

        return a / b;                // 自動變成 Ok
    }

    /// <summary>
    /// 驗證名字。
    /// </summary>
    /// <remarks>
    /// 這裡 T 和 TE 都是 <c>string</c>，所以<b>不能</b>用隱式轉換
    /// （會得到編譯錯誤 CS0457）。改用 <c>Ok&lt;T&gt;</c> / <c>Err&lt;TE&gt;</c> 包裹記錄，
    /// 因為它們的轉換來源型別不同，不會衝突。
    /// </remarks>
    private static Result<string, string> ParseName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Err<string>("名字不可為空白");
        }

        return new Ok<string>(input.Trim());
    }
}
