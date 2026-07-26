using jIAnSoft.Rayleigh.Examples.Helpers;

namespace jIAnSoft.Rayleigh.Examples;

/// <summary>
/// E07：Option 與 Result 互相轉換 —— 什麼時候該用哪一個。
/// </summary>
/// <remarks>
/// <para><b>先搞清楚：我到底該用 Option 還是 Result？</b></para>
/// <list type="table">
///   <item>
///     <term>用 Option</term>
///     <description>「沒有」是很正常的情況，而且呼叫端不需要知道原因。<br/>
///     例：字典查不到某個 key、清單是空的、使用者沒填選填欄位</description>
///   </item>
///   <item>
///     <term>用 Result</term>
///     <description>操作可能失敗，而且呼叫端<b>需要知道為什麼</b>失敗。<br/>
///     例：表單驗證、API 呼叫、檔案讀寫、資料庫操作</description>
///   </item>
/// </list>
/// <para>
/// 實務上這兩者會混用：底層可能回傳 <c>Option</c>（單純查不到），
/// 到了業務層需要告訴使用者原因，就轉成 <c>Result</c>。本模組教你怎麼轉。
/// </para>
/// </remarks>
public static class E07OptionResultInterop
{
    /// <summary>執行 E07 的完整教學流程。</summary>
    public static void Run()
    {
        ConsoleHelper.Header("E07", "Option 與 Result 互轉：什麼時候用哪一個");

        WhichOneShouldIUse();
        OptionToResult();
        ResultToOption();
        RealWorldPipeline();
        DoNotDiscardErrorsTooEarly();

        ConsoleHelper.Summary(
            "Option -> Result：用 ToResult(錯誤)，把「沒有值」升級成「失敗並附原因」",
            "Result -> Option：用 ToOption()，丟棄錯誤原因；或用 Err() 只保留錯誤",
            "轉換方向的原則：往上層走通常補充資訊，往下層走才丟棄",
            "常見錯誤：太早呼叫 ToOption()，把還需要的錯誤原因弄丟了",
            "ToResult 也有惰性版本：ToResult(() => 建立錯誤)，錯誤物件昂貴時使用");
    }

    /// <summary>
    /// 第 1 節：選擇指南。
    /// </summary>
    private static void WhichOneShouldIUse()
    {
        ConsoleHelper.Section("1", "選擇指南：同一件事，兩種表達方式");

        ConsoleHelper.Explain(
            "同樣是「查詢使用者」，看你的呼叫端需要什麼：");

        Console.WriteLine();
        ConsoleHelper.Explain("情況 A：只是想顯示個人資料，查不到就顯示「訪客」——用 Option 就夠了");
        ConsoleHelper.Demo("FindUser(1)", FindUser(1));
        ConsoleHelper.Demo("FindUser(999)", FindUser(999), "只知道沒有，不知道為什麼");

        Console.WriteLine();
        ConsoleHelper.Explain("情況 B：登入流程，必須告訴使用者「帳號不存在」還是「帳號已停用」——要用 Result");
        ConsoleHelper.Demo("Login(1)", Login(1));
        ConsoleHelper.Demo("Login(3)", Login(3), "帳號存在，但被停用了");
        ConsoleHelper.Demo("Login(999)", Login(999), "帳號根本不存在");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "注意情況 B 的兩種失敗，在 Option 的世界裡都只會是 None，\n" +
            "使用者只會看到「登入失敗」，完全不知道該怎麼處理。");

        ConsoleHelper.Good("判斷標準：呼叫端會不會因為「失敗原因不同」而做不同的事？會的話就用 Result");
    }

    /// <summary>
    /// 第 2 節：Option 轉 Result。
    /// </summary>
    private static void OptionToResult()
    {
        ConsoleHelper.Section("2", "Option -> Result：ToResult(錯誤)");

        ConsoleHelper.Explain(
            "ToResult 的意思是「把『沒有值』這件事，升級成一個具名的失敗」。\n" +
            "Some(x) 會變成 Ok(x)，None 會變成 Err(你指定的錯誤)。");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        ConsoleHelper.Demo("Some(42).ToResult(\"查無資料\")", some.ToResult("查無資料"));
        ConsoleHelper.Demo("None.ToResult(\"查無資料\")", none.ToResult("查無資料"));

        Console.WriteLine();
        ConsoleHelper.Explain(
            "如果錯誤物件的建立成本比較高（例如要組字串、要查訊息表），\n" +
            "可以改用惰性版本 ToResult(() => 建立錯誤)——只在真的是 None 時才會執行。");

        var buildCount = 0;
        LoginError BuildError()
        {
            buildCount++;
            return LoginError.UserNotFound;
        }

        ConsoleHelper.Demo("Some(42).ToResult(() => 建立錯誤)", some.ToResult(BuildError));
        ConsoleHelper.Demo("建立錯誤被呼叫幾次？", buildCount, "0 次——有值時不需要錯誤物件");
        ConsoleHelper.Demo("None.ToResult(() => 建立錯誤)", none.ToResult(BuildError));
        ConsoleHelper.Demo("現在呢？", buildCount, "1 次");

        ConsoleHelper.Tip("這是最常用的轉換方向：底層用 Option 查資料，業務層用 ToResult 補上失敗原因");
    }

    /// <summary>
    /// 第 3 節：Result 轉 Option。
    /// </summary>
    private static void ResultToOption()
    {
        ConsoleHelper.Section("3", "Result -> Option：ToOption() 與 Err()");

        var ok = Result<int, LoginError>.Ok(42);
        var err = Result<int, LoginError>.Err(LoginError.AccountDisabled);

        ConsoleHelper.Explain("ToOption()：保留成功值，把錯誤原因丟掉。");
        ConsoleHelper.Demo("Ok(42).ToOption()", ok.ToOption());
        ConsoleHelper.Demo("Err(AccountDisabled).ToOption()", err.ToOption(), "錯誤原因不見了");

        Console.WriteLine();
        ConsoleHelper.Explain("Err()：方向相反，只保留錯誤，把成功值丟掉。");
        ConsoleHelper.Demo("Ok(42).Err()", ok.Err(), "成功，所以沒有錯誤");
        ConsoleHelper.Demo("Err(AccountDisabled).Err()", err.Err(), "取出錯誤，包成 Option");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "Err() 的典型用途是「蒐集一批操作中所有失敗的原因」——\n" +
            "配合 Values() 可以一次把所有錯誤挑出來：");

        var results = new[]
        {
            Result<int, LoginError>.Ok(1),
            Result<int, LoginError>.Err(LoginError.UserNotFound),
            Result<int, LoginError>.Ok(3),
            Result<int, LoginError>.Err(LoginError.AccountDisabled)
        };

        // .Select(r => r.Err()) 得到一堆 Option<LoginError>，
        // 再用 .Values() 把其中有值的挑出來。
        var allErrors = results.Select(r => r.Err()).Values().ToList();
        ConsoleHelper.Demo("挑出所有錯誤", string.Join(", ", allErrors));

        ConsoleHelper.Tip("其實有更直接的做法：results.Partition() 一次拿到成功清單和錯誤清單——見 E08");
    }

    /// <summary>
    /// 第 4 節：實際的混用場景。
    /// </summary>
    private static void RealWorldPipeline()
    {
        ConsoleHelper.Section("4", "實戰：null -> Option -> Result 的完整流程");

        ConsoleHelper.Explain(
            "真實專案的資料通常來自外部（資料庫、HTTP 請求、設定檔），\n" +
            "這些來源給你的是可為 null 的值。標準流程是：\n" +
            "\n" +
            "  1. 用 ToOption() 把 null 擋在邊界\n" +
            "  2. 用 Filter 做基本篩選\n" +
            "  3. 用 ToResult() 補上失敗原因\n" +
            "  4. 用 Bind 串接後續的驗證");

        Console.WriteLine();

        foreach (var input in new[] { "alice@corp.com", "not-an-email", null })
        {
            var result = input
                .ToOption()                                          // string? -> Option<string>
                .Filter(s => !string.IsNullOrWhiteSpace(s))           // 空白視為沒有
                .ToResult(LoginError.UserNotFound)                    // Option -> Result，補上原因
                .Bind(ValidateEmailFormat);                           // 繼續驗證格式

            var display = result.Match(
                ok: email => $"通過：{email}",
                err: e => $"拒絕：{Describe(e)}");

            ConsoleHelper.Demo($"輸入 {input ?? "null"}", display);
        }

        ConsoleHelper.Good("把 null 在最外層就轉成 Option，之後整條流程都不必再檢查 null");
    }

    /// <summary>
    /// 第 5 節：常見錯誤。
    /// </summary>
    private static void DoNotDiscardErrorsTooEarly()
    {
        ConsoleHelper.Section("5", "常見錯誤：太早把錯誤資訊丟掉");

        ConsoleHelper.Explain(
            "ToOption() 是一個「不可逆」的操作——錯誤原因一旦丟掉就找不回來了。\n" +
            "下面對照兩種寫法的差別：");

        Console.WriteLine();

        // 錯誤示範：一拿到 Result 就轉成 Option。
        var badResult = Login(3)          // Err(AccountDisabled)
            .ToOption()                    // 錯誤原因在這裡就消失了
            .Map(u => u.Name)
            .UnwrapOr("登入失敗");         // 只能給一個籠統的訊息

        ConsoleHelper.Demo("錯誤寫法的輸出", badResult);
        ConsoleHelper.Bad("使用者只看到「登入失敗」，不知道是帳號不存在還是被停用");

        Console.WriteLine();

        // 正確示範：保留 Result 直到最後。
        var goodResult = Login(3)
            .Map(u => u.Name)
            .Match(
                ok: name => $"歡迎，{name}",
                err: e => Describe(e));    // 這裡還拿得到具體原因

        ConsoleHelper.Demo("正確寫法的輸出", goodResult);
        ConsoleHelper.Good("使用者看到明確的原因，知道該聯絡客服而不是重試密碼");

        Console.WriteLine();
        ConsoleHelper.Explain(
            "什麼時候用 ToOption() 才是對的？\n" +
            "當你「真的」不在乎原因的時候，例如：\n" +
            "  - 讀取一個選填的設定值，讀不到就用預設值\n" +
            "  - 嘗試多個來源，只要有一個成功就好");
    }

    // ================================================================
    // 以下是本範例用到的輔助型別與方法
    // ================================================================

    /// <summary>登入可能發生的錯誤。</summary>
    private enum LoginError
    {
        UserNotFound,
        AccountDisabled
    }

    /// <summary>把錯誤代碼翻譯成給使用者看的訊息。</summary>
    private static string Describe(LoginError error) => error switch
    {
        LoginError.UserNotFound => "查無此帳號，請確認輸入是否正確",
        LoginError.AccountDisabled => "此帳號已停用，請聯絡客服",
        _ => "登入失敗"
    };

    /// <summary>範例用的使用者。</summary>
    private sealed record User(int Id, string Name, bool IsActive);

    private static readonly User[] Users =
    [
        new(1, "Alice", true),
        new(2, "Bob", true),
        new(3, "Carol", false)   // 已停用
    ];

    /// <summary>
    /// 查詢使用者。<b>回傳 Option</b>——只告訴你有沒有，不說原因。
    /// </summary>
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
    /// 登入。<b>回傳 Result</b>——失敗時說明原因。
    /// </summary>
    /// <remarks>
    /// 注意這個方法怎麼把 <see cref="FindUser"/> 的 Option 升級成 Result：
    /// 先 ToResult 補上「查無此人」，再用 Bind 檢查是否停用。
    /// </remarks>
    private static Result<User, LoginError> Login(int id)
        => FindUser(id)
            .ToResult(LoginError.UserNotFound)
            .Bind(user => user.IsActive
                ? Result<User, LoginError>.Ok(user)
                : LoginError.AccountDisabled);

    /// <summary>驗證 Email 格式。</summary>
    private static Result<string, LoginError> ValidateEmailFormat(string email)
        => email.Contains('@') && email.Contains('.')
            ? email
            : LoginError.UserNotFound;
}
