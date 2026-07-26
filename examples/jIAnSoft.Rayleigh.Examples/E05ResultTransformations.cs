using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E05：轉換 Result —— 鐵路導向程式設計（Railway-Oriented Programming）。
/// </summary>
/// <remarks>
/// <para><b>「鐵路」是什麼意思？</b></para>
/// <para>
/// 想像兩條平行的鐵軌：上面那條是「成功軌」，下面那條是「失敗軌」。
/// </para>
/// <code>
///  Ok  ──►──┬──►── Map ──►── Bind ──►── Map ──►──  最終結果
///           │        │         │         │
///           ▼        ▼         ▼         ▼        （任一步失敗就切換軌道）
///  Err ──►──┴──►─────┴─────────┴─────────┴──►──   錯誤直接送到終點
/// </code>
/// <para>
/// 你的資料從左邊進來，一路經過各種處理。只要有任何一步失敗，
/// 它就會「切換到失敗軌」，後面所有的處理步驟全部自動跳過，錯誤原封不動送到終點。
/// </para>
/// <para>
/// 這代表你<b>不需要在每一步之間寫 if 檢查</b>——這就是 Result 最大的價值。
/// </para>
/// </remarks>
public static class E05ResultTransformations
{
    /// <summary>執行 E05 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E05", "轉換 Result：Map / MapErr / Bind 與鐵路導向");

        MapTransformsSuccess();
        MapErrTransformsError();
        BindChainsFallibleSteps();
        TheRailwayInAction();
        FlattenNestedResults();
        MapOrFamily();

        ConsoleHelper.Summary(
            "Map：只動成功值，失敗時原封不動傳遞錯誤",
            "MapErr：只動錯誤，用來把底層錯誤轉成上層錯誤（例如 DbError -> ApiError）",
            "Bind：串接「下一步也可能失敗」的操作，這是鐵路導向的核心",
            "整條鏈中任何一步失敗，後面全部自動跳過——不需要寫 if",
            "選擇原則和 Option 一樣：函式回傳普通值用 Map，回傳 Result 用 Bind");
    }

    /// <summary>
    /// 第 1 節：Map —— 只轉換成功的那一側。
    /// </summary>
    private static void MapTransformsSuccess()
    {
        ConsoleHelper.Section("1", "Map：轉換成功值，錯誤原封不動");

        ConsoleHelper.Explain(
            "Map 只在 Ok 的時候執行你的函式。\n" +
            "如果是 Err，你的函式不會被呼叫，錯誤會直接被帶到下一步。");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("原始錯誤");

        ConsoleHelper.Demo("Ok(42).Map(x => x * 2)", ok.Map(x => x * 2));
        ConsoleHelper.Demo("Ok(42).Map(x => $\"值：{x}\")", ok.Map(x => $"值：{x}"), "型別從 int 變成 string");
        ConsoleHelper.Demo("Err(\"原始錯誤\").Map(x => x * 2)", err.Map(x => x * 2), "函式沒被執行，錯誤照舊");

        // 用計數器證明「Err 時函式不會執行」。
        var callCount = 0;
        _ = err.Map(x =>
        {
            callCount++;
            return x;
        });
        ConsoleHelper.Demo("Err.Map(...) 後，函式執行幾次？", callCount, "0 次");
    }

    /// <summary>
    /// 第 2 節：MapErr —— 只轉換錯誤的那一側。
    /// </summary>
    private static void MapErrTransformsError()
    {
        ConsoleHelper.Section("2", "MapErr：轉換錯誤（例如把底層錯誤翻譯成使用者看得懂的訊息）");

        ConsoleHelper.Explain(
            "MapErr 和 Map 完全相反：它只在 Err 的時候執行。\n" +
            "最常見的用途是「換一種錯誤型別」——把資料層的錯誤轉成 API 層的錯誤。");

        var ok = Result<int, DbError>.Ok(42);
        var err = Result<int, DbError>.Err(DbError.ConnectionLost);

        // 把 DbError（技術細節）轉成給使用者看的字串。
        ConsoleHelper.Demo("Ok(42).MapErr(轉成訊息)", ok.MapErr(ToUserMessage), "成功時不動作");
        ConsoleHelper.Demo("Err(ConnectionLost).MapErr(轉成訊息)", err.MapErr(ToUserMessage));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "為什麼要這樣做？因為不同層級關心的錯誤不一樣：\n" +
            "  資料層：ConnectionLost、Timeout、DuplicateKey（技術細節）\n" +
            "  API 層：「系統忙碌中，請稍後再試」（使用者看得懂的話）\n" +
            "MapErr 就是兩層之間的翻譯器。");

        ConsoleHelper.Good("MapErr 也能避免把資料庫連線字串之類的敏感資訊洩漏給使用者");
    }

    /// <summary>
    /// 第 3 節：Bind —— 串接可能失敗的步驟。
    /// </summary>
    private static void BindChainsFallibleSteps()
    {
        ConsoleHelper.Section("3", "Bind：串接「這一步也可能失敗」的操作");

        ConsoleHelper.Explain(
            "判斷原則和 Option 完全一樣：\n" +
            "  你的函式回傳普通值        -> 用 Map\n" +
            "  你的函式回傳 Result<...>  -> 用 Bind");

        // ParsePositive 回傳 Result，所以要用 Bind。
        ConsoleHelper.Demo("Ok(\"42\").Bind(ParsePositive)", Result<string, string>.Ok("42").Bind(ParsePositive));
        ConsoleHelper.Demo("Ok(\"-5\").Bind(ParsePositive)", Result<string, string>.Ok("-5").Bind(ParsePositive));
        ConsoleHelper.Demo("Ok(\"abc\").Bind(ParsePositive)", Result<string, string>.Ok("abc").Bind(ParsePositive));

        Console.WriteLine();
        ConsoleHelper.Explain("如果誤用 Map，型別會變成雙層的 Result，後續非常難處理：");
        var nested = Result<string, string>.Ok("42").Map(ParsePositive);
        ConsoleHelper.Demo("Ok(\"42\").Map(ParsePositive)", nested);
        ConsoleHelper.Bad("型別是 Result<Result<int, string>, string>，包了兩層");
    }

    /// <summary>
    /// 第 4 節：完整的鐵路 —— 這是本模組的重點。
    /// </summary>
    private static void TheRailwayInAction()
    {
        ConsoleHelper.Section("4", "鐵路導向實戰：一條完整的驗證流程");

        ConsoleHelper.Explain(
            "情境：處理使用者註冊。要依序做四件事，每一件都可能失敗——\n" +
            "  1. 檢查名字不可空白\n" +
            "  2. 檢查 Email 格式\n" +
            "  3. 檢查年齡範圍\n" +
            "  4. 建立帳號\n" +
            "傳統寫法要寫四層巢狀 if；用 Bind 串起來就是四行。");

        Console.WriteLine();

        // 測試四組資料，分別在不同的步驟失敗。
        var inputs = new[]
        {
            new RegistrationInput("Alice", "alice@corp.com", 30),
            new RegistrationInput("", "bob@corp.com", 25),          // 第 1 步失敗
            new RegistrationInput("Carol", "not-an-email", 28),     // 第 2 步失敗
            new RegistrationInput("Dave", "dave@corp.com", 200)     // 第 3 步失敗
        };

        foreach (var input in inputs)
        {
            // 這就是整個註冊流程——四個步驟串成一條鏈，沒有任何 if。
            var result = ValidateName(input)
                .Bind(ValidateEmail)
                .Bind(ValidateAge)
                .Map(CreateAccount);

            var message = result.Match(
                ok: account => $"註冊成功：{account}",
                err: error => $"註冊失敗：{error}");

            ConsoleHelper.Demo($"輸入 \"{input.Name}\"", message);
        }

        Console.WriteLine();
        ConsoleHelper.Good("四個步驟串成一條鏈，任何一步失敗都會直接跳到終點，中間不需要任何 if");
        ConsoleHelper.Tip("最後用 Match 把 Result 轉成你要的東西（訊息、HTTP 回應、DTO...）");
    }

    /// <summary>
    /// 第 5 節：Flatten。
    /// </summary>
    private static void FlattenNestedResults()
    {
        ConsoleHelper.Section("5", "Flatten：壓平雙層的 Result");

        ConsoleHelper.Explain(
            "如果不小心產生了雙層 Result（通常是誤用 Map 造成的），Flatten 可以壓平它。\n" +
            "規則：外層是 Err 就用外層的錯誤，外層 Ok 但內層 Err 就用內層的錯誤。");

        var bothOk = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(42));
        var innerErr = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("內層錯誤"));
        var outerErr = Result<Result<int, string>, string>.Err("外層錯誤");

        ConsoleHelper.Demo("Ok(Ok(42)).Flatten()", bothOk.Flatten());
        ConsoleHelper.Demo("Ok(Err(\"內層錯誤\")).Flatten()", innerErr.Flatten());
        ConsoleHelper.Demo("Err(\"外層錯誤\").Flatten()", outerErr.Flatten());

        ConsoleHelper.Tip("x.Map(f).Flatten() 等同於 x.Bind(f)——直接用 Bind 就好，不要繞路");
    }

    /// <summary>
    /// 第 6 節：MapOr / MapOrElse。
    /// </summary>
    private static void MapOrFamily()
    {
        ConsoleHelper.Section("6", "MapOr / MapOrElse：轉換並直接得到最終值");

        ConsoleHelper.Explain(
            "和 Option 的同名方法一樣：轉換 + 提供失敗時的替代方案，一次完成。\n" +
            "回傳的是普通型別，不再是 Result。");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("boom");

        // MapOr(預設值, 轉換函式)
        ConsoleHelper.Demo("Ok(42).MapOr(\"無\", x => $\"值 {x}\")", ok.MapOr("無", x => $"值 {x}"));
        ConsoleHelper.Demo("Err.MapOr(\"無\", x => $\"值 {x}\")", err.MapOr("無", x => $"值 {x}"));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "MapOrElse 的第一個參數改成函式，而且——注意這點——\n" +
            "它會把「錯誤內容」傳給你，所以可以根據錯誤產生不同的替代結果。");

        ConsoleHelper.Demo(
            "Err(\"boom\").MapOrElse(e => $\"錯誤：{e}\", x => ...)",
            err.MapOrElse(e => $"錯誤：{e}", x => $"值 {x}"));

        ConsoleHelper.Tip("MapOrElse 和 Match 幾乎是同一件事，選你覺得比較好讀的那個");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>模擬資料層的錯誤型別。</summary>
    private enum DbError
    {
        ConnectionLost,
        Timeout,
        DuplicateKey
    }

    /// <summary>把技術性的資料層錯誤，翻譯成使用者看得懂的訊息。</summary>
    private static string ToUserMessage(DbError error) => error switch
    {
        DbError.ConnectionLost => "系統忙碌中，請稍後再試",
        DbError.Timeout => "操作逾時，請重新整理",
        DbError.DuplicateKey => "這筆資料已經存在",
        _ => "發生未知錯誤"
    };

    /// <summary>
    /// 把字串解析成正整數。回傳 Result，所以呼叫端要用 Bind。
    /// </summary>
    private static Result<int, string> ParsePositive(string input)
    {
        if (!int.TryParse(input, out var number))
        {
            return $"\"{input}\" 不是有效的數字";
        }

        if (number <= 0)
        {
            return $"{number} 不是正整數";
        }

        return number;
    }

    /// <summary>註冊表單的輸入資料。</summary>
    private sealed record RegistrationInput(string Name, string Email, int Age);

    /// <summary>步驟 1：驗證名字。</summary>
    private static Result<RegistrationInput, string> ValidateName(RegistrationInput input)
        => string.IsNullOrWhiteSpace(input.Name)
            ? "名字不可為空白"
            : input;

    /// <summary>步驟 2：驗證 Email 格式。</summary>
    private static Result<RegistrationInput, string> ValidateEmail(RegistrationInput input)
        => input.Email.Contains('@') && input.Email.Contains('.')
            ? input
            : $"\"{input.Email}\" 不是有效的 Email";

    /// <summary>步驟 3：驗證年齡。</summary>
    private static Result<RegistrationInput, string> ValidateAge(RegistrationInput input)
        => input.Age is >= 0 and <= 130
            ? input
            : $"年齡 {input.Age} 不在合理範圍內";

    /// <summary>
    /// 步驟 4：建立帳號。
    /// </summary>
    /// <remarks>
    /// 注意這個方法回傳的是<b>普通字串</b>而不是 Result——
    /// 因為前面三步都通過了，建立帳號這一步假設不會失敗。
    /// 所以呼叫端用的是 <c>Map</c> 而不是 <c>Bind</c>。
    /// </remarks>
    private static string CreateAccount(RegistrationInput input)
        => $"{input.Name} <{input.Email}>";
}
