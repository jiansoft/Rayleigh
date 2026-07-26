using System.Diagnostics;
using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E12：常見陷阱 —— 新手（和不少老手）最容易踩的 10 個坑。
/// </summary>
/// <remarks>
/// <para>
/// 前面的模組教你「怎麼做對」，這個模組專門講「怎麼做錯」。
/// 每個陷阱都會實際跑一次錯誤寫法，再給出正確版本。
/// </para>
/// <para>
/// 建議看完前面所有模組再讀這一篇——你會發現很多坑自己已經差點踩了。
/// </para>
/// </remarks>
public static class E12CommonPitfalls
{
    /// <summary>執行 E12 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E12", "常見陷阱：10 個最容易犯的錯");

        Pitfall01_UnwrapWithoutChecking();
        Pitfall02_UninitializedResult();
        Pitfall03_MapWhenYouNeedBind();
        Pitfall04_SameTypeForValueAndError();
        Pitfall05_NestedIfInsteadOfChaining();
        Pitfall06_ExceptionForExpectedErrors();
        Pitfall07_SwallowingErrors();
        Pitfall08_DiscardingErrorTooEarly();
        Pitfall09_EagerFallback();
        Pitfall10_OptionWhenYouNeedResult();

        ConsoleHelper.Summary(
            "不要在業務流程中用 Unwrap()——用 Match / TryGetOk / UnwrapOr",
            "不要依賴 default(Result<T,E>)，它會直接拋例外",
            "函式回傳 Option/Result 時要用 Bind，用 Map 會多包一層",
            "T 和 TE 不能同型別，否則隱式轉換編譯失敗",
            "預期內的錯誤請用 Result，不要用例外（實測慢一萬倍以上）");
    }

    /// <summary>陷阱 1：不檢查就 Unwrap。</summary>
    private static void Pitfall01_UnwrapWithoutChecking()
    {
        ConsoleHelper.Section("陷阱 1", "不檢查就直接 Unwrap()");

        var none = Option<int>.None;
        var err = Result<int, string>.Err("查無資料");

        ConsoleHelper.Bad("直接呼叫 Unwrap()，等於把 Option 的保護全部丟掉");
        ConsoleHelper.Boom("Option<int>.None.Unwrap()", () => none.Unwrap());
        ConsoleHelper.Boom("Result.Err(...).Unwrap()", () => err.Unwrap());

        Console.WriteLine();
        ConsoleHelper.Good("改用這三種安全寫法之一：");
        ConsoleHelper.Demo("none.UnwrapOr(-1)", none.UnwrapOr(-1));
        ConsoleHelper.Demo("none.Match(v => v, () => -1)", none.Match(v => v, () => -1));
        ConsoleHelper.Demo("none.TryGetValue(out var v)", none.TryGetValue(out _));

        ConsoleHelper.Tip("真的確定不可能失敗時，也請用 Expect(\"原因\") 而不是 Unwrap()");
    }

    /// <summary>陷阱 2：未初始化的 Result。</summary>
    private static void Pitfall02_UninitializedResult()
    {
        ConsoleHelper.Section("陷阱 2", "以為 default(Result<T,E>) 可以用");

        ConsoleHelper.Explain(
            "Result 是 struct，所以陣列元素、未指派的欄位都會是 default 狀態。\n" +
            "但那不是 Ok 也不是 Err——它什麼都不是，碰它就會拋例外。");

        var uninitialized = default(Result<int, MyError>);

        ConsoleHelper.Demo("default(Result<...>).ToString()", uninitialized.ToString());
        ConsoleHelper.Boom("default(Result<...>).Unwrap()", () => uninitialized.Unwrap());

        Console.WriteLine();
        ConsoleHelper.Explain(
            "特別注意：即使錯誤型別是 enum，這個保護仍然有效。\n" +
            "MyError.NotFound 的底層值是 0，和 default 一樣——但兩者仍能被區分：");

        var legit = Result<int, MyError>.Err(MyError.NotFound);
        ConsoleHelper.Demo("Err(NotFound) 是合法的失敗", legit.UnwrapErr());
        ConsoleHelper.Demo("它等於 default 嗎？", legit.Equals(uninitialized), "不相等");

        Console.WriteLine();
        ConsoleHelper.Bad("var results = new Result<int, MyError>[10];  // 十個未初始化的地雷");
        ConsoleHelper.Good("建立陣列後請立刻填入 Ok() 或 Err()，或改用 List 動態加入");
    }

    /// <summary>陷阱 3：該用 Bind 卻用 Map。</summary>
    private static void Pitfall03_MapWhenYouNeedBind()
    {
        ConsoleHelper.Section("陷阱 3", "該用 Bind 的地方用了 Map");

        ConsoleHelper.Explain(
            "判斷標準只有一個：看你傳進去的函式回傳什麼。\n" +
            "  回傳普通值        -> Map\n" +
            "  回傳 Option/Result -> Bind");

        var input = Option<string>.Some("42");

        // ParseNumber 回傳 Option<int>，所以用 Map 會包兩層。
        Option<Option<int>> wrong = input.Map(ParseNumber);
        Option<int> right = input.Bind(ParseNumber);

        Console.WriteLine();
        ConsoleHelper.Bad("input.Map(ParseNumber)");
        ConsoleHelper.Demo("  結果型別", "Option<Option<int>>", "包了兩層，後面很難處理");
        ConsoleHelper.Demo("  值", wrong);

        ConsoleHelper.Good("input.Bind(ParseNumber)");
        ConsoleHelper.Demo("  結果型別", "Option<int>", "正確攤平");
        ConsoleHelper.Demo("  值", right);

        ConsoleHelper.Tip("如果已經包了兩層，用 .Flatten() 可以救回來");
    }

    /// <summary>陷阱 4：T 和 TE 同型別。</summary>
    private static void Pitfall04_SameTypeForValueAndError()
    {
        ConsoleHelper.Section("陷阱 4", "Result<string, string> 的隱式轉換會編譯失敗");

        ConsoleHelper.Explain(
            "Result 同時定義了「從 T 轉換」和「從 TE 轉換」兩個隱式轉換運算子。\n" +
            "當 T 和 TE 都是 string 時，這兩個運算子的簽章完全相同，編譯器無法選擇。");

        Console.WriteLine();
        ConsoleHelper.Bad("Result<string, string> F() => ok ? \"值\" : \"錯誤\";   // CS0457 模稜兩可");

        Console.WriteLine();
        ConsoleHelper.Good("解法 1：用明確的工廠方法");
        ConsoleHelper.Demo("Result<string,string>.Ok(\"值\")", Result<string, string>.Ok("值"));

        ConsoleHelper.Good("解法 2：用 Ok<T> / Err<TE> 包裹記錄（推薦，比較簡潔）");
        ConsoleHelper.Demo("ParseName(\"Alice\")", ParseName("Alice"));
        ConsoleHelper.Demo("ParseName(\"\")", ParseName(""));

        Console.WriteLine();
        ConsoleHelper.Good("解法 3（最好）：為錯誤定義專用型別，順便讓錯誤有明確語意");
        ConsoleHelper.Demo("用 enum 當錯誤型別", Result<string, MyError>.Err(MyError.Invalid));
    }

    /// <summary>陷阱 5：巢狀 if。</summary>
    private static void Pitfall05_NestedIfInsteadOfChaining()
    {
        ConsoleHelper.Section("陷阱 5", "用巢狀 if 而不是串接");

        ConsoleHelper.Explain("很多人剛學會 Option 之後，還是照舊寫巢狀判斷：");

        Console.WriteLine();
        ConsoleHelper.Bad(
            "if (a.IsSome) { var x = a.Unwrap();\n" +
            "                 if (b.IsSome) { var y = b.Unwrap();\n" +
            "                                  return Option.Some(x + y); } }\n" +
            "        return Option.None;");

        Console.WriteLine();
        ConsoleHelper.Good("同樣的邏輯，用串接寫成一行：");

        var a = Option<int>.Some(10);
        var b = Option<int>.Some(20);

        ConsoleHelper.Demo("a.ZipWith(b, (x, y) => x + y)", a.ZipWith(b, (x, y) => x + y));

        ConsoleHelper.Good("或是用 LINQ 查詢語法，中間值都在同一層：");
        var sum =
            from x in a
            from y in b
            select x + y;
        ConsoleHelper.Demo("from x in a from y in b select x + y", sum);
    }

    /// <summary>陷阱 6：用例外處理預期內的錯誤（附實測數據）。</summary>
    private static void Pitfall06_ExceptionForExpectedErrors()
    {
        ConsoleHelper.Section("陷阱 6", "用例外處理「預期內」的錯誤");

        ConsoleHelper.Explain(
            "「使用者輸入的 Email 格式不對」不是例外，那是每天發生幾千次的正常情況。\n" +
            "用例外處理這種事，代價非常高——我們實際跑一次給你看。");

        Console.WriteLine();

        const int iterations = 20_000;

        // 方式 A：Result（不拋例外）
        var swResult = Stopwatch.StartNew();
        var resultTotal = 0;
        for (var i = 0; i < iterations; i++)
        {
            resultTotal += ValidateWithResult("bad-input").UnwrapOr(-1);
        }

        swResult.Stop();

        // 方式 B：例外
        var swException = Stopwatch.StartNew();
        var exceptionTotal = 0;
        for (var i = 0; i < iterations; i++)
        {
            try
            {
                exceptionTotal += ValidateWithException("bad-input");
            }
            catch (FormatException)
            {
                exceptionTotal += -1;
            }
        }

        swException.Stop();

        ConsoleHelper.Demo($"Result 寫法（{iterations:N0} 次）", $"{swResult.ElapsedMilliseconds} ms");
        ConsoleHelper.Demo($"例外寫法（{iterations:N0} 次）", $"{swException.ElapsedMilliseconds} ms");

        var ratio = swResult.ElapsedMilliseconds == 0
            ? "超過 1000"
            : (swException.ElapsedMilliseconds / (double)swResult.ElapsedMilliseconds).ToString("N0");
        ConsoleHelper.Demo("倍數差距", $"約 {ratio} 倍", "而且例外還會配置記憶體、抓堆疊追蹤");

        Console.WriteLine();
        ConsoleHelper.Good("例外請留給「真正的意外」：磁碟壞掉、記憶體不足、程式邏輯錯誤");
        ConsoleHelper.Good("預期內的失敗（驗證、查無資料、業務規則）請用 Result");
    }

    /// <summary>陷阱 7：安靜地吞掉錯誤。</summary>
    private static void Pitfall07_SwallowingErrors()
    {
        ConsoleHelper.Section("陷阱 7", "用 UnwrapOr 安靜地吞掉錯誤");

        ConsoleHelper.Explain(
            "UnwrapOr 很方便，但它會讓錯誤完全消失。\n" +
            "如果那個錯誤代表「資料庫連不上」，你的系統會安靜地回傳預設值，沒有人發現異常。");

        var dbError = Result<int, string>.Err("資料庫連線失敗");

        Console.WriteLine();
        ConsoleHelper.Bad("var count = GetUserCount().UnwrapOr(0);   // 連線失敗也顯示 0 個使用者");
        ConsoleHelper.Demo("結果", dbError.UnwrapOr(0), "看起來很正常，但其實系統壞了");

        Console.WriteLine();
        ConsoleHelper.Good("至少先記錄下來再給預設值：");
        var log = new List<string>();
        var safe = dbError
            .TapErr(e => log.Add($"[ERROR] {e}"))
            .UnwrapOr(0);

        ConsoleHelper.Demo("結果", safe);
        ConsoleHelper.Demo("但錯誤有被記錄", log[0]);

        ConsoleHelper.Tip("UnwrapOrElse(e => ...) 也可以——它會把錯誤傳給你，讓你順手處理");
    }

    /// <summary>陷阱 8：太早丟掉錯誤資訊。</summary>
    private static void Pitfall08_DiscardingErrorTooEarly()
    {
        ConsoleHelper.Section("陷阱 8", "太早呼叫 ToOption()，把錯誤原因弄丟");

        ConsoleHelper.Explain("ToOption() 是不可逆的——錯誤原因丟掉之後就找不回來了。");

        Console.WriteLine();

        var loginFailed = Result<string, MyError>.Err(MyError.Disabled);

        ConsoleHelper.Bad("太早轉換：");
        var early = loginFailed.ToOption().UnwrapOr("登入失敗");
        ConsoleHelper.Demo("使用者看到", early, "不知道是密碼錯還是帳號被停用");

        Console.WriteLine();
        ConsoleHelper.Good("保留 Result 到最後一刻：");
        var late = loginFailed.Match(
            ok: name => $"歡迎，{name}",
            err: e => e switch
            {
                MyError.NotFound => "查無此帳號",
                MyError.Disabled => "此帳號已停用，請聯絡客服",
                _ => "登入失敗"
            });
        ConsoleHelper.Demo("使用者看到", late, "知道該怎麼處理");
    }

    /// <summary>陷阱 9：用 Or 而不是 OrElse。</summary>
    private static void Pitfall09_EagerFallback()
    {
        ConsoleHelper.Section("陷阱 9", "用 Or 導致備援被白白執行");

        ConsoleHelper.Explain(
            "Or 的參數是「值」，會在呼叫前就先算好；\n" +
            "OrElse 的參數是「函式」，只有真的需要時才執行。");

        Console.WriteLine();

        var cached = Option<string>.Some("來自快取");

        // 錯誤寫法：不管快取有沒有命中，ExpensiveQuery() 都會先被執行一次。
        _queryCount = 0;
        var eager = cached.Or(ExpensiveQuery());
        ConsoleHelper.Bad("cached.Or(ExpensiveQuery())");
        ConsoleHelper.Demo("  結果", eager);
        ConsoleHelper.Demo("  昂貴查詢被執行幾次？", _queryCount, "1 次——即使快取已經命中！");

        Console.WriteLine();

        // 正確寫法：快取命中時完全不會執行。
        _queryCount = 0;
        var lazy = cached.OrElse(ExpensiveQuery);
        ConsoleHelper.Good("cached.OrElse(ExpensiveQuery)");
        ConsoleHelper.Demo("  結果", lazy);
        ConsoleHelper.Demo("  昂貴查詢被執行幾次？", _queryCount, "0 次");

        ConsoleHelper.Tip("記法：名稱有 Else 的都是惰性版本（OrElse、UnwrapOrElse、MapOrElse）");
    }

    /// <summary>陷阱 10：該用 Result 卻用 Option。</summary>
    private static void Pitfall10_OptionWhenYouNeedResult()
    {
        ConsoleHelper.Section("陷阱 10", "需要知道原因，卻選了 Option");

        ConsoleHelper.Explain(
            "Option 只能表達「有」或「沒有」。\n" +
            "如果失敗有好幾種原因，而且呼叫端會因為原因不同而做不同的事，那就必須用 Result。");

        Console.WriteLine();
        ConsoleHelper.Bad("Option<User> Login(...)   // 帳號不存在？密碼錯？被鎖定？分不出來");
        ConsoleHelper.Good("Result<User, LoginError> Login(...)   // 呼叫端能針對每種原因處理");

        Console.WriteLine();
        ConsoleHelper.Explain("判斷流程：");
        ConsoleHelper.Explain(
            "  呼叫端需要知道失敗原因嗎？\n" +
            "    需要 -> Result<T, E>\n" +
            "    不需要 -> Option<T>\n" +
            "\n" +
            "  不確定的話選 Result——之後想降級成 Option 隨時可以（ToOption()），\n" +
            "  但反過來要補上錯誤原因就得改動所有呼叫端了。");

        ConsoleHelper.Tip("底層資料存取常用 Option，業務層用 ToResult() 升級成 Result 附上原因");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>範例用的錯誤型別。NotFound 的底層值刻意是 0，用來示範陷阱 2。</summary>
    private enum MyError
    {
        NotFound,
        Disabled,
        Invalid
    }

    private static Option<int> ParseNumber(string text)
        => int.TryParse(text, out var value) ? Option<int>.Some(value) : Option.None;

    /// <summary>T 和 TE 同為 string，只能用包裹記錄。</summary>
    private static Result<string, string> ParseName(string input)
        => string.IsNullOrWhiteSpace(input)
            ? new Err<string>("名字不可為空白")
            : new Ok<string>(input);

    /// <summary>用 Result 表達驗證失敗（不拋例外）。</summary>
    private static Result<int, string> ValidateWithResult(string input)
        => int.TryParse(input, out var value) ? value : "不是數字";

    /// <summary>用例外表達驗證失敗（對照組）。</summary>
    private static int ValidateWithException(string input)
    {
        if (!int.TryParse(input, out var value))
        {
            throw new FormatException("不是數字");
        }

        return value;
    }

    private static int _queryCount;

    /// <summary>模擬昂貴的查詢，用計數器記錄被呼叫幾次。</summary>
    private static Option<string> ExpensiveQuery()
    {
        _queryCount++;
        return Option<string>.Some("來自資料庫");
    }
}
