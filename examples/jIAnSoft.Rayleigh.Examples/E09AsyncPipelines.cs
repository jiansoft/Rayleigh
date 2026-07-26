using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E09：非同步管線 —— 在 async 世界裡使用 Option 和 Result。
/// </summary>
/// <remarks>
/// <para><b>問題出在哪裡</b></para>
/// <para>
/// 當你的方法變成 <c>async</c>，回傳型別就從 <c>Result&lt;T, E&gt;</c> 變成
/// <c>Task&lt;Result&lt;T, E&gt;&gt;</c>。這時候你不能直接 <c>.Bind(...)</c>，
/// 因為 <c>Task</c> 上面沒有這個方法——你得先 <c>await</c> 才行：
/// </para>
/// <code>
/// // 沒有非同步擴充方法時，只能這樣寫：
/// var user = await GetUserAsync(id);
/// if (user.IsErr) return user.MapErr(...);
/// var orders = await GetOrdersAsync(user.Unwrap().Id);
/// ...
/// </code>
/// <para>
/// Rayleigh 提供的 <c>BindAsync</c> / <c>MapAsync</c> 等擴充方法，
/// 讓你可以像同步版本一樣把整條鏈串起來，最後只 <c>await</c> 一次。
/// </para>
/// <para><b>三種「起點」</b></para>
/// <list type="bullet">
///   <item><description><c>Task&lt;Result&lt;T,E&gt;&gt;</c> —— 最常見</description></item>
///   <item><description><c>ValueTask&lt;Result&lt;T,E&gt;&gt;</c> —— 高頻呼叫時用</description></item>
///   <item><description><c>Result&lt;T,E&gt;</c> 本身 —— 起點是同步取得的值（第 5 節）</description></item>
/// </list>
/// </remarks>
public static class E09AsyncPipelines
{
    /// <summary>執行 E09 的完整教學流程。</summary>
    public static async Task RunAsync()
    {
        ConsoleHelper.Header("E09", "非同步管線：BindAsync / MapAsync 與零配置短路");

        await BindAsyncBasics();
        await MapAsyncAndMapErrAsync();
        await TapAndOrElseAsync();
        await FullPipeline();
        await SyncSourceOverloads();
        await CancellationSupport();
        await OptionAsyncBasics();

        ConsoleHelper.Summary(
            "BindAsync / MapAsync：讓 Task<Result<T,E>> 也能像同步版本一樣串接",
            "所有方法內部都用 ConfigureAwait(false)，不會捕捉同步內容",
            "起點是同步的 Result/Option 時，直接用它本身的多載，不要包 Task.FromResult",
            "短路（Err/None）時，sync-source 多載完全同步完成，零記憶體配置",
            "每個組合子都有 CancellationToken 版本，只在真的要執行委派時才檢查取消");
    }

    /// <summary>
    /// 第 1 節：BindAsync。
    /// </summary>
    private static async Task BindAsyncBasics()
    {
        ConsoleHelper.Section("1", "BindAsync：串接多個非同步且可能失敗的步驟");

        ConsoleHelper.Explain(
            "和同步版的 Bind 一樣，只是每一步都是非同步的。\n" +
            "注意整條鏈只在最前面寫一個 await，中間不需要。");

        // 成功路徑：查得到使用者，也查得到訂單。
        var success = await GetUserAsync(1)
            .BindAsync(user => GetOrdersAsync(user.Id));

        ConsoleHelper.Demo("GetUserAsync(1).BindAsync(GetOrdersAsync)", Describe(success));

        // 第一步就失敗：GetOrdersAsync 根本不會被呼叫。
        var firstStepFails = await GetUserAsync(999)
            .BindAsync(user => GetOrdersAsync(user.Id));

        ConsoleHelper.Demo("GetUserAsync(999).BindAsync(...)", Describe(firstStepFails), "第二步沒有執行");

        // 第二步失敗。
        var secondStepFails = await GetUserAsync(2)
            .BindAsync(user => GetOrdersAsync(user.Id));

        ConsoleHelper.Demo("使用者存在但沒有訂單", Describe(secondStepFails));
    }

    /// <summary>
    /// 第 2 節：MapAsync / MapErrAsync。
    /// </summary>
    private static async Task MapAsyncAndMapErrAsync()
    {
        ConsoleHelper.Section("2", "MapAsync / MapErrAsync：非同步轉換成功值或錯誤");

        ConsoleHelper.Explain("MapAsync：轉換成功值，轉換動作本身是非同步的（例如要再查一次 API）。");

        var mapped = await GetUserAsync(1)
            .MapAsync(async user =>
            {
                // 模擬非同步取得頭像網址。
                await Task.Delay(1);
                return $"{user.Name} <頭像已載入>";
            });

        ConsoleHelper.Demo("MapAsync 轉換後", Describe(mapped));

        Console.WriteLine();
        ConsoleHelper.Explain("MapErrAsync：非同步轉換錯誤，例如去翻譯訊息表或寫遠端 log。");

        var mappedErr = await GetUserAsync(999)
            .MapErrAsync(async error =>
            {
                await Task.Delay(1);
                return $"[已記錄] {error}";
            });

        ConsoleHelper.Demo("MapErrAsync 轉換後", Describe(mappedErr));
    }

    /// <summary>
    /// 第 3 節：TapAsync / OrElseAsync。
    /// </summary>
    private static async Task TapAndOrElseAsync()
    {
        ConsoleHelper.Section("3", "TapAsync / TapErrAsync / OrElseAsync");

        var log = new List<string>();

        ConsoleHelper.Explain("TapAsync：非同步副作用（寫遠端 log、發通知），不改變結果。");

        var tapped = await GetUserAsync(1)
            .TapAsync(async user =>
            {
                await Task.Delay(1);
                log.Add($"稽核記錄：查詢了 {user.Name}");
            })
            .TapErrAsync(async error =>
            {
                await Task.Delay(1);
                log.Add($"錯誤記錄：{error}");
            });

        ConsoleHelper.Demo("結果（未被 Tap 改變）", Describe(tapped));
        foreach (var entry in log)
        {
            ConsoleHelper.Demo("  log", entry);
        }

        Console.WriteLine();
        ConsoleHelper.Explain("OrElseAsync：失敗時執行非同步備援（例如主資料庫失敗改查備援庫）。");

        var recovered = await GetUserAsync(999)
            .OrElseAsync(async error =>
            {
                await Task.Delay(1);
                // 備援：回傳一個訪客帳號。
                return Result<User, string>.Ok(new User(0, "訪客"));
            });

        ConsoleHelper.Demo("OrElseAsync 備援後", Describe(recovered));
    }

    /// <summary>
    /// 第 4 節：完整管線。
    /// </summary>
    private static async Task FullPipeline()
    {
        ConsoleHelper.Section("4", "完整範例：把所有組合子串在一起");

        ConsoleHelper.Explain(
            "注意這整段只有最前面一個 await，而且沒有任何 if 或 try/catch。");

        foreach (var id in new[] { 1, 2, 999 })
        {
            var summary = await GetUserAsync(id)
                .TapAsync(u => LogAsync($"開始處理 {u.Name}"))
                .BindAsync(u => GetOrdersAsync(u.Id))
                .MapAsync(async orders =>
                {
                    await Task.Delay(1);
                    return orders.Sum(o => o.Amount);
                })
                .MapErrAsync(async e =>
                {
                    await Task.Delay(1);
                    return $"（已通報）{e}";
                });

            var text = summary.Match(
                ok: total => $"訂單總額 {total:C}",
                err: error => error);

            ConsoleHelper.Demo($"處理 id={id}", text);
        }
    }

    /// <summary>
    /// 第 5 節：起點是同步值的多載 —— 這是最容易被忽略的用法。
    /// </summary>
    private static async Task SyncSourceOverloads()
    {
        ConsoleHelper.Section("5", "重要：起點是「同步的 Result」時，不要包 Task.FromResult");

        ConsoleHelper.Explain(
            "常見情境：你先做了同步的驗證，通過之後才要非同步存檔。\n" +
            "驗證的結果是同步的 Result<T,E>，但 BindAsync 需要 Task<Result<T,E>>...\n" +
            "\n" +
            "很多人會這樣寫：");

        Console.WriteLine();
        ConsoleHelper.Bad("await Task.FromResult(Validate(x)).BindAsync(SaveAsync)   // 多配置一個 Task");

        Console.WriteLine();
        ConsoleHelper.Explain("其實可以直接對同步的 Result 呼叫 BindAsync：");
        ConsoleHelper.Good("await Validate(x).BindAsync(SaveAsync)                   // 不需要包裝");

        Console.WriteLine();

        foreach (var input in new[] { "valid-data", "" })
        {
            var saved = await ValidateInput(input)
                .BindAsync(SaveAsync);

            ConsoleHelper.Demo($"處理 \"{input}\"", Describe(saved));
        }

        Console.WriteLine();
        ConsoleHelper.Explain(
            "這些多載還有一個刻意的設計：它們沒有宣告成 async。\n" +
            "當來源是 Err（要短路）時，它直接回傳一個「已完成」的 ValueTask，\n" +
            "完全不會建立 async 狀態機，也不會配置任何記憶體。");

        // 實際證明短路路徑是同步完成的。
        var pending = ValidateInput("")     // 這會是 Err
            .BindAsync(SaveAsync);

        ConsoleHelper.Demo("短路時 ValueTask 已完成？", pending.IsCompletedSuccessfully, "true = 完全沒碰執行緒集區");
        _ = await pending;   // 還是要 await 掉，避免警告

        Console.WriteLine();
        ConsoleHelper.Explain("Option 也有同樣的多載，最典型的用途是「快取命中就用，沒有才去抓」：");

        var cache = new Dictionary<string, string> { ["hot"] = "來自快取" };
        var fetchCount = 0;

        foreach (var key in new[] { "hot", "cold" })
        {
            var value = await cache.GetValueOrNone(key)
                .OrElseAsync(async () =>
                {
                    fetchCount++;
                    await Task.Delay(1);
                    return Option<string>.Some("來自遠端");
                });

            ConsoleHelper.Demo($"取得 \"{key}\"", value.UnwrapOr("(失敗)"));
        }

        ConsoleHelper.Demo("遠端被呼叫幾次？", fetchCount, "1 次——快取命中時完全沒有非同步成本");
    }

    /// <summary>
    /// 第 6 節：CancellationToken。
    /// </summary>
    private static async Task CancellationSupport()
    {
        ConsoleHelper.Section("6", "CancellationToken：每個組合子都有可傳遞取消訊號的版本");

        ConsoleHelper.Explain(
            "只要你的委派多接一個 CancellationToken 參數，就會自動選到帶取消訊號的多載。");

        using var cts = new CancellationTokenSource();

        var result = await GetUserAsync(1)
            .BindAsync((user, ct) => GetOrdersAsync(user.Id, ct), cts.Token);

        ConsoleHelper.Demo("正常執行（未取消）", Describe(result));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "重要的設計細節：只有在「真的要執行你的委派」時才會檢查取消狀態。\n" +
            "如果前一步已經失敗、整條鏈要短路，那就是個 no-op，不會拋出 OperationCanceledException。");

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // 來源已經是 Err，委派不會執行，所以即使 token 已取消也不會拋出。
        var shortCircuited = await GetUserAsync(999)
            .BindAsync((user, ct) => GetOrdersAsync(user.Id, ct), cancelled.Token);

        ConsoleHelper.Demo("已取消的 token + 已失敗的來源", Describe(shortCircuited), "沒有拋例外，因為委派根本沒執行");
    }

    /// <summary>
    /// 第 7 節：Option 的非同步版本。
    /// </summary>
    private static async Task OptionAsyncBasics()
    {
        ConsoleHelper.Section("7", "Option 的非同步版本：BindAsync / MapAsync / OrElseAsync");

        ConsoleHelper.Explain("用法和 Result 版本完全一致，只是沒有錯誤那一側。");

        var found = await FindUserAsync(1)
            .MapAsync(async user =>
            {
                await Task.Delay(1);
                return user.Name.ToUpperInvariant();
            });

        ConsoleHelper.Demo("FindUserAsync(1).MapAsync(...)", found);

        var notFound = await FindUserAsync(999)
            .BindAsync(user => FindEmailAsync(user.Id));

        ConsoleHelper.Demo("FindUserAsync(999).BindAsync(...)", notFound, "None 一路傳遞到底");

        Console.WriteLine();
        ConsoleHelper.Tip("ValueTask 版本的 API 完全對應，高頻呼叫路徑上可以改用它避免 Task 配置");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法（都是模擬的非同步操作）
    // ================================================================

    private sealed record User(int Id, string Name);

    private sealed record Order(int Id, decimal Amount);

    private static readonly User[] Users = [new(1, "Alice"), new(2, "Bob")];

    /// <summary>模擬非同步查詢使用者，回傳 Result。</summary>
    private static async Task<Result<User, string>> GetUserAsync(int id)
    {
        await Task.Delay(1);   // 模擬 I/O

        foreach (var user in Users)
        {
            if (user.Id == id)
            {
                return user;
            }
        }

        return $"找不到 Id={id} 的使用者";
    }

    /// <summary>模擬非同步查詢訂單。Bob（id=2）沒有任何訂單。</summary>
    private static async Task<Result<List<Order>, string>> GetOrdersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        if (userId != 1)
        {
            return $"使用者 {userId} 沒有任何訂單";
        }

        return new List<Order> { new(101, 1200m), new(102, 850m) };
    }

    /// <summary>模擬非同步查詢使用者，回傳 Option。</summary>
    private static async Task<Option<User>> FindUserAsync(int id)
    {
        await Task.Delay(1);

        foreach (var user in Users)
        {
            if (user.Id == id)
            {
                return user;
            }
        }

        return Option.None;
    }

    /// <summary>模擬非同步查詢 Email。</summary>
    private static async Task<Option<string>> FindEmailAsync(int userId)
    {
        await Task.Delay(1);
        return userId == 1 ? Option<string>.Some("alice@corp.com") : Option.None;
    }

    /// <summary>
    /// 同步的輸入驗證——注意它<b>不是</b> async，回傳的是普通的 Result。
    /// </summary>
    /// <remarks>
    /// 這裡 T 和 TE 都是 <c>string</c>，所以必須用 <c>Ok&lt;T&gt;</c> / <c>Err&lt;TE&gt;</c> 包裹記錄，
    /// 直接用隱式轉換會得到編譯錯誤 CS0457。詳見 E12 的陷阱 4。
    /// </remarks>
    private static Result<string, string> ValidateInput(string input)
        => string.IsNullOrWhiteSpace(input)
            ? new Err<string>("輸入不可為空白")
            : new Ok<string>(input);

    /// <summary>模擬非同步存檔。回傳 ValueTask 以搭配 sync-source 多載。</summary>
    private static async ValueTask<Result<string, string>> SaveAsync(string data)
    {
        await Task.Delay(1);
        return new Ok<string>($"已存檔：{data}");
    }

    /// <summary>模擬非同步寫 log。</summary>
    private static async Task LogAsync(string message)
    {
        await Task.Delay(1);
    }

    /// <summary>把 Result 轉成好讀的字串，純粹為了範例輸出。</summary>
    private static string Describe<T, TE>(Result<T, TE> result)
        where T : notnull
        where TE : notnull
        => result.Match(
            ok: value => value is System.Collections.IEnumerable and not string
                ? $"Ok（{((System.Collections.IEnumerable)value).Cast<object>().Count()} 筆）"
                : $"Ok({value})",
            err: error => $"Err({error})");
}
