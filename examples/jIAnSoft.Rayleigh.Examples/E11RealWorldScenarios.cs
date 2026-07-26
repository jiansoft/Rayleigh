using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E11：實戰場景 —— 把前面學的東西組合起來解決真實問題。
/// </summary>
/// <remarks>
/// <para>
/// 前面的模組是一個一個介紹 API，這個模組反過來：從<b>真實需求</b>出發，
/// 看看該挑哪些工具、怎麼組合。每個場景都會先寫出「傳統寫法」再對照「Rayleigh 寫法」，
/// 讓你看到差別在哪裡。
/// </para>
/// <para>五個場景：設定檔讀取、表單驗證、使用者註冊、批次匯入、多層快取。</para>
/// </remarks>
public static class E11RealWorldScenarios
{
    /// <summary>執行 E11 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E11", "實戰場景：五個真實需求的完整解法");

        Scenario1_Configuration();
        Scenario2_FormValidation();
        Scenario3_UserRegistration();
        Scenario4_BatchImport();
        Scenario5_LayeredCache();

        ConsoleHelper.Summary(
            "設定檔：GetValueOrNone + OrElse 串接多個來源，最後 UnwrapOr 給預設值",
            "表單驗證：Partition 一次蒐集所有錯誤，使用者不用來回修改",
            "註冊流程：Bind 串接每個驗證步驟，任一步失敗就自動短路",
            "批次匯入：Sequence 全有或全無，或 Partition 部分成功",
            "多層快取：OrElse 串接快取 -> 資料庫 -> 遠端，全部都是惰性求值");
    }

    /// <summary>
    /// 場景 1：讀取設定值。
    /// </summary>
    private static void Scenario1_Configuration()
    {
        ConsoleHelper.Section("1", "場景：讀取設定值（環境變數 -> 設定檔 -> 預設值）");

        ConsoleHelper.Explain(
            "需求：先看環境變數，沒有就看設定檔，再沒有就用預設值。\n" +
            "傳統寫法會是三層巢狀的 if 或一串 ?? 運算子，還要小心 int.TryParse 的 out 參數。");

        Console.WriteLine();

        foreach (var key in new[] { "PORT", "TIMEOUT", "UNKNOWN" })
        {
            // 三個來源依序嘗試，全部都是惰性的——前面找到就不會查後面。
            var value = ReadFromEnvironment(key)
                .OrElse(() => ReadFromConfigFile(key))
                .Filter(v => v > 0)                      // 順便驗證合理性
                .UnwrapOr(8080);                         // 都沒有就用預設值

            var source = ReadFromEnvironment(key).IsSome ? "環境變數"
                : ReadFromConfigFile(key).IsSome ? "設定檔"
                : "預設值";

            ConsoleHelper.Demo($"設定 {key}", $"{value}（來源：{source}）");
        }

        ConsoleHelper.Good("整段沒有 null 檢查、沒有 out 參數、沒有巢狀 if");
    }

    /// <summary>
    /// 場景 2：表單驗證。
    /// </summary>
    private static void Scenario2_FormValidation()
    {
        ConsoleHelper.Section("2", "場景：表單驗證（一次回報所有錯誤）");

        ConsoleHelper.Explain(
            "需求：使用者填了一張表單，可能有多個欄位不合格。\n" +
            "重點是要「一次告訴他全部的問題」，而不是改一個才說下一個。");

        Console.WriteLine();

        var form = new Dictionary<string, string>
        {
            ["姓名"] = "",
            ["Email"] = "not-an-email",
            ["年齡"] = "200",
            ["電話"] = "0912345678"
        };

        // 每個欄位各自驗證，得到一堆 Result。
        var validations = new[]
        {
            ValidateRequired("姓名", form["姓名"]),
            ValidateEmail(form["Email"]),
            ValidateAge(form["年齡"]),
            ValidateRequired("電話", form["電話"])
        };

        // Partition 一次拿到「通過的」和「所有錯誤」。
        var (passed, errors) = validations.Partition();

        ConsoleHelper.Demo("通過的欄位數", passed.Count);
        ConsoleHelper.Demo("錯誤數", errors.Count);

        Console.WriteLine();
        ConsoleHelper.Explain("回傳給前端的錯誤清單：");
        foreach (var error in errors)
        {
            ConsoleHelper.Demo("  ", error);
        }

        ConsoleHelper.Good("用 Partition 而不是 Sequence——因為使用者需要看到全部的問題");
    }

    /// <summary>
    /// 場景 3：使用者註冊流程。
    /// </summary>
    private static void Scenario3_UserRegistration()
    {
        ConsoleHelper.Section("3", "場景：使用者註冊（步驟有先後順序，前面過了才做後面）");

        ConsoleHelper.Explain(
            "和表單驗證不同：這裡的步驟有相依性。\n" +
            "帳號重複就不必再檢查密碼強度，所以要「遇到錯誤就停」——用 Bind 串接。");

        Console.WriteLine();

        var attempts = new[]
        {
            new SignUpRequest("alice@corp.com", "Str0ng!Pass"),
            new SignUpRequest("bob@corp.com", "Str0ng!Pass"),     // 這個 email 已存在
            new SignUpRequest("carol@corp.com", "123")             // 密碼太弱
        };

        foreach (var request in attempts)
        {
            var result = CheckEmailFormat(request)
                .Bind(CheckEmailNotTaken)
                .Bind(CheckPasswordStrength)
                .Map(CreateAccount);

            var message = result.Match(
                ok: account => $"註冊成功 -> {account}",
                err: error => $"註冊失敗 -> {error}");

            ConsoleHelper.Demo(request.Email, message);
        }

        ConsoleHelper.Good("四個步驟串成一條鏈，中間沒有任何 if 或 early return");
    }

    /// <summary>
    /// 場景 4：批次匯入。
    /// </summary>
    private static void Scenario4_BatchImport()
    {
        ConsoleHelper.Section("4", "場景：批次匯入 CSV（示範兩種策略的取捨）");

        var csvLines = new[]
        {
            "1,鍵盤,2500",
            "2,滑鼠,890",
            "3,螢幕,abc",        // 價格格式錯誤
            "4,,1200",            // 名稱空白
            "5,耳機,3200"
        };

        ConsoleHelper.Explain("原始資料（第 3、4 行有問題）：");
        foreach (var line in csvLines)
        {
            ConsoleHelper.Demo("  ", line);
        }

        var parsed = csvLines.Select(ParseCsvLine).ToArray();

        Console.WriteLine();
        ConsoleHelper.Explain("策略 A：嚴格模式——只要有一行壞掉，整批退回不匯入。");
        var strict = parsed.Sequence();
        ConsoleHelper.Demo("Sequence() 的結果", strict.Match(
            ok: items => $"匯入 {items.Count} 筆",
            err: error => $"整批退回：{error}"));

        Console.WriteLine();
        ConsoleHelper.Explain("策略 B：寬容模式——好的先匯入，壞的另外產出報表。");
        var (items, errors) = parsed.Partition();
        ConsoleHelper.Demo("成功匯入", $"{items.Count} 筆");
        foreach (var item in items)
        {
            ConsoleHelper.Demo("  匯入", item);
        }

        ConsoleHelper.Demo("需要人工處理", $"{errors.Count} 筆");
        foreach (var error in errors)
        {
            ConsoleHelper.Demo("  錯誤", error);
        }

        Console.WriteLine();
        ConsoleHelper.Tip("財務資料通常選 A（不容許部分匯入）；日誌、行銷名單通常選 B");
    }

    /// <summary>
    /// 場景 5：多層快取。
    /// </summary>
    private static void Scenario5_LayeredCache()
    {
        ConsoleHelper.Section("5", "場景：多層快取（記憶體 -> Redis -> 資料庫）");

        ConsoleHelper.Explain(
            "需求：依序查三層，先找到的就用。重點是「後面的層級不能被白白呼叫」——\n" +
            "OrElse 收的是函式，只有前一層真的沒找到時才會執行。");

        Console.WriteLine();

        foreach (var key in new[] { "user:1", "user:2", "user:3", "user:999" })
        {
            // 計數器歸零，用來證明惰性求值。
            _redisHits = 0;
            _dbHits = 0;

            var value = ReadMemory(key)
                .OrElse(() => ReadRedis(key))
                .OrElse(() => ReadDatabase(key))
                .UnwrapOr("(查無資料)");

            ConsoleHelper.Demo(
                key,
                value,
                $"Redis 被查 {_redisHits} 次、DB 被查 {_dbHits} 次");
        }

        Console.WriteLine();
        ConsoleHelper.Good("記憶體命中時，Redis 和 DB 完全沒有被呼叫——這就是惰性求值的價值");
        ConsoleHelper.Bad("如果寫成 .Or(ReadRedis(key)) 就慘了——不管有沒有命中都會先查一次 Redis");
    }

    // ================================================================
    // 場景 1 的輔助方法
    // ================================================================

    private static readonly Dictionary<string, string> Environment = new() { ["PORT"] = "9000" };
    private static readonly Dictionary<string, string> ConfigFile = new() { ["TIMEOUT"] = "30" };

    private static Option<int> ReadFromEnvironment(string key)
        => Environment.GetValueOrNone(key).Bind(ParseInt);

    private static Option<int> ReadFromConfigFile(string key)
        => ConfigFile.GetValueOrNone(key).Bind(ParseInt);

    private static Option<int> ParseInt(string text)
        => int.TryParse(text, out var value) ? Option<int>.Some(value) : Option.None;

    // ================================================================
    // 場景 2 的輔助方法
    // ================================================================

    // 注意：以下三個方法的 T 和 TE 都是 string，所以必須使用
    // Ok<T> / Err<TE> 包裹記錄，不能直接用隱式轉換（會得到編譯錯誤 CS0457）。
    // 詳見 E12 的陷阱 4。實務上更好的做法是為錯誤定義專用型別。

    private static Result<string, string> ValidateRequired(string field, string value)
        => string.IsNullOrWhiteSpace(value)
            ? new Err<string>($"{field} 不可為空白")
            : new Ok<string>(value);

    private static Result<string, string> ValidateEmail(string value)
        => value.Contains('@') && value.Contains('.')
            ? new Ok<string>(value)
            : new Err<string>($"Email 格式不正確：{value}");

    private static Result<string, string> ValidateAge(string value)
    {
        if (!int.TryParse(value, out var age))
        {
            return new Err<string>($"年齡必須是數字：{value}");
        }

        return age is >= 0 and <= 130
            ? new Ok<string>(value)
            : new Err<string>($"年齡超出合理範圍：{age}");
    }

    // ================================================================
    // 場景 3 的輔助方法
    // ================================================================

    private sealed record SignUpRequest(string Email, string Password);

    private static readonly HashSet<string> ExistingEmails = ["bob@corp.com"];

    private static Result<SignUpRequest, string> CheckEmailFormat(SignUpRequest request)
        => request.Email.Contains('@') ? request : "Email 格式不正確";

    private static Result<SignUpRequest, string> CheckEmailNotTaken(SignUpRequest request)
        => ExistingEmails.Contains(request.Email) ? "這個 Email 已經註冊過了" : request;

    private static Result<SignUpRequest, string> CheckPasswordStrength(SignUpRequest request)
        => request.Password.Length >= 8 ? request : "密碼至少需要 8 個字元";

    private static string CreateAccount(SignUpRequest request) => $"帳號 {request.Email} 已建立";

    // ================================================================
    // 場景 4 的輔助方法
    // ================================================================

    private sealed record Product(int Id, string Name, decimal Price)
    {
        public override string ToString() => $"#{Id} {Name} {Price:C}";
    }

    /// <summary>解析一行 CSV。任何一個欄位有問題就回傳說明清楚的錯誤。</summary>
    private static Result<Product, string> ParseCsvLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != 3)
        {
            return $"「{line}」欄位數不正確";
        }

        if (!int.TryParse(parts[0], out var id))
        {
            return $"「{line}」Id 不是數字";
        }

        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            return $"「{line}」名稱不可為空白";
        }

        if (!decimal.TryParse(parts[2], out var price))
        {
            return $"「{line}」價格不是數字";
        }

        return new Product(id, parts[1], price);
    }

    // ================================================================
    // 場景 5 的輔助方法
    // ================================================================

    private static int _redisHits;
    private static int _dbHits;

    private static readonly Dictionary<string, string> Memory = new() { ["user:1"] = "Alice（記憶體）" };
    private static readonly Dictionary<string, string> Redis = new() { ["user:2"] = "Bob（Redis）" };
    private static readonly Dictionary<string, string> Database = new() { ["user:3"] = "Carol（資料庫）" };

    private static Option<string> ReadMemory(string key) => Memory.GetValueOrNone(key);

    private static Option<string> ReadRedis(string key)
    {
        _redisHits++;
        return Redis.GetValueOrNone(key);
    }

    private static Option<string> ReadDatabase(string key)
    {
        _dbHits++;
        return Database.GetValueOrNone(key);
    }
}
