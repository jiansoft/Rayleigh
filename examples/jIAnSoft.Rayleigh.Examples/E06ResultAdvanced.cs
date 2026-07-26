using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E06：Result 的取值、備援與副作用 —— 以及 Unit 型別的用途。
/// </summary>
/// <remarks>
/// <para>
/// E05 教你怎麼「把資料推過鐵路」，本模組教你<b>鐵路的終點站</b>：
/// 怎麼把結果拿出來、失敗時怎麼備援、以及怎麼在中途記錄日誌。
/// </para>
/// <para>
/// 另外會介紹一個新手常問的問題：「如果我的操作成功時沒有東西要回傳呢？」
/// 答案是 <c>Result&lt;Unit, TE&gt;</c>，第 6 節會說明。
/// </para>
/// </remarks>
public static class E06ResultAdvanced
{
    /// <summary>執行 E06 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E06", "Result 取值、備援、副作用與 Unit 型別");

        MatchTheSafeWay();
        TryGetPatterns();
        UnwrapFamily();
        OrElseFallback();
        TapForLogging();
        UnitForVoidOperations();
        ConvertingToOption();

        ConsoleHelper.Summary(
            "Match：同時處理成功與失敗，最安全的取值方式",
            "TryGetOk(out value, out error)：雙輸出版本最適合寫 guard clause",
            "OrElse：失敗時的備援，而且可以看到錯誤內容再決定怎麼做",
            "Tap / TapErr：在鏈中間記錄日誌，不影響資料流",
            "Result<Unit, TE>：操作成功但沒有值要回傳時使用（相當於 void）");
    }

    /// <summary>
    /// 第 1 節：Match。
    /// </summary>
    private static void MatchTheSafeWay()
    {
        ConsoleHelper.Section("1", "Match：把 Result 轉換成你要的最終型別");

        ConsoleHelper.Explain(
            "Match 要你同時提供「成功怎麼辦」和「失敗怎麼辦」，兩邊都不能漏。\n" +
            "最典型的用途是在 Web API 的最外層，把 Result 轉成 HTTP 回應。");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("查無資料");

        // 模擬轉成 HTTP 回應的樣子。
        string ToHttpResponse(Result<int, string> result) => result.Match(
            ok: value => $"200 OK  {{ \"data\": {value} }}",
            err: error => $"400 Bad Request  {{ \"error\": \"{error}\" }}");

        ConsoleHelper.Demo("ToHttpResponse(Ok(42))", ToHttpResponse(ok));
        ConsoleHelper.Demo("ToHttpResponse(Err(...))", ToHttpResponse(err));

        ConsoleHelper.Good("在 Controller 的最後一行用 Match，整個方法就不需要任何 try/catch");
    }

    /// <summary>
    /// 第 2 節：TryGetOk / TryGetErr。
    /// </summary>
    private static void TryGetPatterns()
    {
        ConsoleHelper.Section("2", "TryGetOk / TryGetErr：guard clause 寫法");

        ConsoleHelper.Explain(
            "TryGetOk 有兩種版本：\n" +
            "  單輸出：TryGetOk(out var value)              -> 只關心成功值\n" +
            "  雙輸出：TryGetOk(out var value, out var err)  -> 兩邊都要（推薦）");

        // ProcessOrder 定義在下方，示範雙輸出版本的 guard clause。
        ConsoleHelper.Demo("ProcessOrder(100)", ProcessOrder(100));
        ConsoleHelper.Demo("ProcessOrder(-1)", ProcessOrder(-1));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "為什麼推薦雙輸出版本？因為失敗時你通常需要知道「為什麼失敗」，\n" +
            "才能回傳有意義的訊息。單輸出版本會把錯誤資訊丟掉。");

        ConsoleHelper.Good("if (!result.TryGetOk(out var value, out var error)) { return 處理(error); }");

        Console.WriteLine();
        ConsoleHelper.Explain("TryGetErr 則是反過來，只在失敗時給你錯誤。");
        var err = Result<int, string>.Err("磁碟已滿");
        if (err.TryGetErr(out var reason))
        {
            ConsoleHelper.Demo("TryGetErr 取出的錯誤", reason);
        }
    }

    /// <summary>
    /// 第 3 節：Unwrap 家族。
    /// </summary>
    private static void UnwrapFamily()
    {
        ConsoleHelper.Section("3", "Unwrap 家族：從安全到危險");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("設定檔損毀");

        ConsoleHelper.Explain("安全的版本：失敗時給預設值，不會拋例外。");
        ConsoleHelper.Demo("Ok(42).UnwrapOr(-1)", ok.UnwrapOr(-1));
        ConsoleHelper.Demo("Err(...).UnwrapOr(-1)", err.UnwrapOr(-1));

        // UnwrapOrElse 會把錯誤傳給你，所以可以根據錯誤決定預設值。
        ConsoleHelper.Demo(
            "Err(...).UnwrapOrElse(e => e.Length)",
            err.UnwrapOrElse(e => e.Length),
            "可以根據錯誤內容決定要回傳什麼");

        Console.WriteLine();
        ConsoleHelper.Explain("危險的版本：失敗時直接拋例外。");
        ConsoleHelper.Demo("Ok(42).Unwrap()", ok.Unwrap());
        ConsoleHelper.Boom("Err(\"設定檔損毀\").Unwrap()", () => err.Unwrap());
        ConsoleHelper.Boom("Err(...).Expect(\"啟動時必須讀到設定\")", () => err.Expect("啟動時必須讀到設定"));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "還有反過來的版本，用在測試裡驗證「這個操作應該要失敗」：");
        ConsoleHelper.Demo("Err(\"設定檔損毀\").UnwrapErr()", err.UnwrapErr());
        ConsoleHelper.Boom("Ok(42).UnwrapErr()", () => ok.UnwrapErr());

        Console.WriteLine();
        ConsoleHelper.Bad("Unwrap() 的預設例外訊息會把錯誤內容整個印出來");
        ConsoleHelper.Explain(
            "如果你的錯誤型別可能含有敏感資訊（連線字串、使用者輸入、Token），\n" +
            "這些內容會經由未攔截的例外流進 log 或錯誤回應。\n" +
            "這種情況請改用 Expect(\"固定的安全訊息\")。");
    }

    /// <summary>
    /// 第 4 節：Or / OrElse。
    /// </summary>
    private static void OrElseFallback()
    {
        ConsoleHelper.Section("4", "Or / OrElse：失敗時的備援方案");

        ConsoleHelper.Explain(
            "OrElse 比 Option 版本更強大——它會把「錯誤內容」傳給你，\n" +
            "所以你可以根據不同的錯誤採取不同的備援策略。");

        // 模擬三層資料來源：快取 -> 資料庫 -> 預設值。
        ConsoleHelper.Explain("\n情境：三層備援（先查快取，失敗查資料庫，再失敗用預設值）");

        foreach (var key in new[] { "in-cache", "in-db", "nowhere" })
        {
            var value = ReadFromCache(key)
                .OrElse(cacheErr =>
                {
                    // 這裡可以看到快取為什麼失敗，決定要不要繼續往下查。
                    return ReadFromDatabase(key);
                })
                .UnwrapOr("(使用預設值)");

            ConsoleHelper.Demo($"查詢 \"{key}\"", value);
        }

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Or 和 OrElse 的差別和 Option 一樣：\n" +
            "  Or(備援結果)   -> 備援會立刻求值，就算用不到\n" +
            "  OrElse(函式)   -> 只在失敗時才執行，昂貴的備援用這個");

        ConsoleHelper.Tip("重試邏輯也可以這樣寫：result.OrElse(e => e is Timeout ? Retry() : result)");
    }

    /// <summary>
    /// 第 5 節：Tap / TapErr。
    /// </summary>
    private static void TapForLogging()
    {
        ConsoleHelper.Section("5", "Tap / TapErr：在鏈中記錄日誌而不干擾資料流");

        ConsoleHelper.Explain(
            "Tap 在成功時執行、TapErr 在失敗時執行，\n" +
            "兩者都會把 Result 原封不動傳給下一步。這是加日誌最乾淨的方式。");

        var log = new List<string>();

        // 成功的情況。
        var okResult = Result<int, string>.Ok(10)
            .Tap(v => log.Add($"[INFO] 收到值 {v}"))
            .Map(v => v * 2)
            .Tap(v => log.Add($"[INFO] 處理後 {v}"))
            .TapErr(e => log.Add($"[ERROR] {e}"));   // 不會執行

        // 失敗的情況。
        var errResult = Result<int, string>.Err("連線逾時")
            .Tap(v => log.Add($"[INFO] 收到值 {v}"))  // 不會執行
            .Map(v => v * 2)
            .TapErr(e => log.Add($"[ERROR] {e}"));

        ConsoleHelper.Demo("成功路徑的最終結果", okResult);
        ConsoleHelper.Demo("失敗路徑的最終結果", errResult);

        Console.WriteLine();
        ConsoleHelper.Explain("記錄下來的日誌：");
        foreach (var entry in log)
        {
            ConsoleHelper.Demo("  ", entry);
        }

        ConsoleHelper.Good("Tap 不會改變 Result 的內容，插入或移除它都不影響業務邏輯");
    }

    /// <summary>
    /// 第 6 節：Unit 型別。
    /// </summary>
    private static void UnitForVoidOperations()
    {
        ConsoleHelper.Section("6", "Unit：當操作成功但「沒有東西要回傳」時");

        ConsoleHelper.Explain(
            "問題：刪除一筆資料成功時，要回傳什麼？沒有值可以回傳，但也不能用 void，\n" +
            "因為 Result<void, TE> 在 C# 裡是不合法的寫法。\n" +
            "\n" +
            "答案是 Unit —— 一個「只有一種值」的型別，專門用來表達「成功，但沒有內容」。");

        ConsoleHelper.Demo("Unit.Value", Unit.Value, "它的 ToString() 是空括號，代表「什麼都沒有」");

        Console.WriteLine();

        // DeleteUser 定義在下方，回傳 Result<Unit, string>。
        ConsoleHelper.Demo("DeleteUser(1)", DeleteUser(1));
        ConsoleHelper.Demo("DeleteUser(999)", DeleteUser(999));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Result<Unit, TE> 讀起來就是：「這個操作可能失敗，成功的話沒有東西要給你」。\n" +
            "它一樣可以串進鐵路，和其他 Result 完全相容。");

        var chained = DeleteUser(1)
            .Tap(_ => ConsoleHelper.Demo("  刪除成功，執行後續動作", "已發送通知"))
            .Match(
                ok: _ => "完成",
                err: e => $"失敗：{e}");

        ConsoleHelper.Demo("串接後的結果", chained);
    }

    /// <summary>
    /// 第 7 節：Result 轉 Option。
    /// </summary>
    private static void ConvertingToOption()
    {
        ConsoleHelper.Section("7", "轉換成 Option：ToOption() 與 Err()");

        ConsoleHelper.Explain(
            "有時候你不在乎失敗的原因，只想知道「有沒有拿到東西」。\n" +
            "這時可以把 Result 降級成 Option。");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("查無資料");

        // ToOption()：保留成功值，丟棄錯誤。
        ConsoleHelper.Demo("Ok(42).ToOption()", ok.ToOption());
        ConsoleHelper.Demo("Err(...).ToOption()", err.ToOption(), "錯誤資訊被丟棄了");

        // Err()：反過來，保留錯誤，丟棄成功值。
        ConsoleHelper.Demo("Ok(42).Err()", ok.Err());
        ConsoleHelper.Demo("Err(\"查無資料\").Err()", err.Err(), "取出錯誤，包成 Option");

        Console.WriteLine();
        ConsoleHelper.Bad("太早呼叫 ToOption() 會永久丟失錯誤資訊，之後想查都查不到");
        ConsoleHelper.Good("請在流程的最末端、確定不需要錯誤原因時才轉換");

        ConsoleHelper.Tip("反方向轉換是 option.ToResult(錯誤)，把「沒有值」變成「失敗」——見 E07");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>
    /// 示範雙輸出 TryGetOk 的 guard clause 寫法。
    /// </summary>
    private static string ProcessOrder(int amount)
    {
        var validated = ValidateAmount(amount);

        // 一行搞定：失敗就帶著錯誤提早返回，成功就繼續往下走。
        if (!validated.TryGetOk(out var value, out var error))
        {
            return $"訂單被拒絕：{error}";
        }

        // 走到這裡，value 保證可用。
        return $"訂單成立，金額 {value}";
    }

    /// <summary>驗證訂單金額。</summary>
    private static Result<int, string> ValidateAmount(int amount)
        => amount > 0 ? amount : $"金額必須大於 0（收到 {amount}）";

    private static readonly Dictionary<string, string> Cache = new() { ["in-cache"] = "來自快取" };
    private static readonly Dictionary<string, string> Database = new() { ["in-db"] = "來自資料庫" };

    /// <summary>
    /// 模擬讀取快取。
    /// </summary>
    /// <remarks>
    /// 注意這裡用的是 <c>Ok&lt;T&gt;</c> / <c>Err&lt;TE&gt;</c> 包裹記錄，而不是直接 return 字串。
    /// 因為此處 T 和 TE 都是 <c>string</c>，直接用隱式轉換會得到編譯錯誤 CS0457（模稜兩可）。
    /// 詳見 E12 的陷阱 4。
    /// </remarks>
    private static Result<string, string> ReadFromCache(string key)
        => Cache.TryGetValue(key, out var value)
            ? new Ok<string>(value)
            : new Err<string>($"快取沒有 \"{key}\"");

    /// <summary>模擬讀取資料庫。同樣使用包裹記錄，理由見 <see cref="ReadFromCache"/>。</summary>
    private static Result<string, string> ReadFromDatabase(string key)
        => Database.TryGetValue(key, out var value)
            ? new Ok<string>(value)
            : new Err<string>($"資料庫也沒有 \"{key}\"");

    /// <summary>
    /// 刪除使用者。示範 <c>Result&lt;Unit, TE&gt;</c> 的用法。
    /// </summary>
    /// <remarks>
    /// 成功時沒有任何值要回傳，所以用 <see cref="Unit.Value"/>。
    /// 這比回傳 <c>bool</c> 好，因為失敗時還能說明原因。
    /// </remarks>
    private static Result<Unit, string> DeleteUser(int id)
    {
        if (id is not (1 or 2))
        {
            return $"找不到 Id={id} 的使用者";
        }

        // 這裡實際上會執行刪除動作，成功後回傳 Unit.Value 代表「做完了」。
        return Unit.Value;
    }
}
