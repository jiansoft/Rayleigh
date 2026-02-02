using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh;

// ================================
// Ok<T> / Err<E> - 包裹記錄
// ================================

/// <summary>
/// 代表成功的包裹記錄，可隱式轉換為 <see cref="Result{T,E}"/>。
/// </summary>
/// <typeparam name="T">成功值的類型。</typeparam>
/// <param name="Value">成功的值。</param>
/// <remarks>
/// <para>
/// 此 record 提供更直覺的語法來建立成功結果，支援隱式轉換至任意錯誤類型的 <see cref="Result{T,E}"/>。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 使用 Ok wrapper（適用於需要明確表達意圖的場景）
/// Result&lt;int, string&gt; Divide(int a, int b)
/// {
///     if (b == 0) return new Err&lt;string&gt;("Division by zero");
///     return new Ok&lt;int&gt;(a / b);
/// }
///
/// // 或直接使用隱式轉換（推薦寫法）
/// Result&lt;int, string&gt; Divide2(int a, int b)
/// {
///     if (b == 0) return "Division by zero";
///     return a / b;
/// }
/// </code>
/// </example>
public readonly record struct Ok<T>(T Value);

/// <summary>
/// 代表失敗的包裹記錄，可隱式轉換為 <see cref="Result{T,E}"/>。
/// </summary>
/// <typeparam name="TE">錯誤類型。</typeparam>
/// <param name="Error">錯誤資訊。</param>
/// <remarks>
/// <para>
/// 此 record 提供更直覺的語法來建立錯誤結果，支援隱式轉換至任意成功類型的 <see cref="Result{T,E}"/>。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 使用 Err wrapper
/// Result&lt;User, ValidationError&gt; ValidateUser(UserDto dto)
/// {
///     if (string.IsNullOrEmpty(dto.Name))
///         return new Err&lt;ValidationError&gt;(ValidationError.NameRequired);
///     return new Ok&lt;User&gt;(new User(dto.Name));
/// }
/// </code>
/// </example>
public readonly record struct Err<TE>(TE Error);


// ================================
// Result<T, E> - 高性能零分配版
// ================================

/// <summary>
/// 表示一個操作的結果，可以是成功的值或錯誤資訊。
/// 用於取代 Exception 驅動的流程控制。
/// </summary>
/// <typeparam name="T">成功時回傳的值類型。</typeparam>
/// <typeparam name="TE">失敗時回傳的錯誤類型，必須為非 null。</typeparam>
/// <remarks>
/// <para><b>設計目的</b></para>
/// <para>
/// <see cref="Result{T,E}"/> 是一種函數式程式設計的基礎型別，用於明確表達「操作可能成功或失敗」的語意。
/// 相較於使用例外處理來控制流程，<see cref="Result{T,E}"/> 提供了以下優勢：
/// </para>
/// <list type="bullet">
///   <item><description>編譯時期強制處理錯誤情況，避免未處理的例外</description></item>
///   <item><description>零成本錯誤處理：錯誤路徑不需要堆疊展開（stack unwinding）</description></item>
///   <item><description>明確的 API 語意：函數簽章清楚表明可能的錯誤類型</description></item>
///   <item><description>支援函數式組合操作（Map、Bind），便於鏈式處理</description></item>
///   <item><description>支援 LINQ 查詢語法</description></item>
/// </list>
///
/// <para><b>效能特性</b></para>
/// <para>
/// 此型別為 <c>readonly struct</c>，具備以下效能優勢：
/// </para>
/// <list type="bullet">
///   <item><description>零 Heap 分配：整個結構儲存於 Stack</description></item>
///   <item><description>AggressiveInlining：所有關鍵方法會被 JIT 內聯</description></item>
///   <item><description>無例外開銷：錯誤處理不需要建立例外物件或展開堆疊</description></item>
/// </list>
///
/// <para><b>何時使用 Result vs Exception</b></para>
/// <list type="bullet">
///   <item><description><b>使用 Result</b>：預期會發生的業務錯誤（驗證失敗、找不到資料、業務規則違反）</description></item>
///   <item><description><b>使用 Exception</b>：程式錯誤或不可預期的系統錯誤（NullReferenceException、OutOfMemoryException）</description></item>
/// </list>
///
/// <para><b>使用範例</b></para>
/// <code>
/// // 定義錯誤類型
/// public enum UserError { NotFound, Inactive, InsufficientBalance }
///
/// // 回傳 Result 的方法
/// Result&lt;User, UserError&gt; GetActiveUser(Guid id)
/// {
///     var user = _repository.Find(id);
///     if (user is null) return UserError.NotFound;
///     if (!user.IsActive) return UserError.Inactive;
///     return user;
/// }
///
/// // 使用 Match 處理結果
/// var message = GetActiveUser(userId).Match(
///     ok: user => "歡迎，" + user.Name + "！",
///     err: error => error switch
///     {
///         UserError.NotFound => "使用者不存在",
///         UserError.Inactive => "帳號已停用",
///         _ => "未知錯誤"
///     }
/// );
///
/// // 使用 LINQ 查詢語法
/// var balance = from user in GetActiveUser(userId)
///               from account in GetAccount(user.AccountId)
///               select account.Balance;
/// </code>
/// </remarks>
public readonly struct Result<T, TE> : IEquatable<Result<T, TE>> where TE : notnull where T : notnull
{
    private readonly T? _value;
    private readonly TE? _error;

    /// <summary>
    /// 取得一個值，指出操作是否成功。
    /// </summary>
    [MemberNotNullWhen(true, nameof(_value))]
    [MemberNotNullWhen(false, nameof(_error))]
    public bool IsOk { get; }

    /// <summary>
    /// 取得一個值，指出操作是否失敗。
    /// </summary>
    [MemberNotNullWhen(true, nameof(_error))]
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsErr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsOk;
    }

    #region Contains / Conditional Checks

    /// <summary>
    /// 檢查 Result 是否為成功且包含指定的值。
    /// </summary>
    /// <param name="value">要檢查的值。</param>
    /// <returns>若成功且值等於 <paramref name="value"/> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要快速驗證結果是否為特定成功值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var ok = Result&lt;int, string&gt;.Ok(42);
    /// var err = Result&lt;int, string&gt;.Err("error");
    ///
    /// Console.WriteLine(ok.Contains(42));   // true
    /// Console.WriteLine(ok.Contains(100));  // false
    /// Console.WriteLine(err.Contains(42));  // false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T value)
    {
        ThrowIfUninitialized();
        return IsOk && EqualityComparer<T>.Default.Equals(_value, value);
    }

    /// <summary>
    /// 檢查 Result 是否為失敗且包含指定的錯誤。
    /// </summary>
    /// <param name="error">要檢查的錯誤。</param>
    /// <returns>若失敗且錯誤等於 <paramref name="error"/> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要快速驗證結果是否為特定錯誤時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var err = Result&lt;int, string&gt;.Err("not found");
    ///
    /// if (err.ContainsErr("not found"))
    /// {
    ///     // Handle specific error
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsErr(TE error)
    {
        ThrowIfUninitialized();
        return IsErr && EqualityComparer<TE>.Default.Equals(_error, error);
    }

    /// <summary>
    /// 若成功且符合條件則回傳 true。
    /// </summary>
    /// <param name="predicate">要檢查的條件。</param>
    /// <returns>若成功且 <paramref name="predicate"/> 回傳 <c>true</c> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對成功值進行條件判斷，且希望失敗結果視為不符合條件時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = GetUser(id);
    ///
    /// // 檢查使用者是否為活躍狀態，若取得失敗則視為不活躍
    /// if (result.IsOkAnd(u => u.IsActive))
    /// {
    ///     // 操作成功且使用者為活躍狀態
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOkAnd(Func<T, bool> predicate)
    {
        ThrowIfUninitialized();
        return IsOk && predicate(_value);
    }

    /// <summary>
    /// 若失敗且符合條件則回傳 true。
    /// </summary>
    /// <param name="predicate">要檢查的條件。</param>
    /// <returns>若失敗且 <paramref name="predicate"/> 回傳 <c>true</c> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對錯誤進行分類或條件判斷時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = FetchData();
    ///
    /// // 僅針對超時錯誤進行重試
    /// if (result.IsErrAnd(e => e is TimeoutException))
    /// {
    ///     Retry();
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsErrAnd(Func<TE, bool> predicate)
    {
        ThrowIfUninitialized();
        return IsErr && predicate(_error);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result(T value)
    {
        _value = value;
        _error = default;
        IsOk = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Result(TE error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _value = default;
        _error = error;
        IsOk = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfUninitialized()
    {
        if (!IsOk && _error is null)
        {
            ThrowUninitializedException();
        }
    }

    [DoesNotReturn]
    private static void ThrowUninitializedException()
        => throw new InvalidOperationException("Result is in an uninitialized state (default struct).");

    #region Factory Methods

    /// <summary>
    /// 創建一個代表成功的結果。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, TE> Ok(T value) => new(value);

    /// <summary>
    /// 創建一個代表失敗的結果。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, TE> Err(TE error) => new(error);

    #endregion

    #region Implicit Operators

    /// <summary>
    /// 允許將成功的值隱式轉換為 <see cref="Result{T,E}"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T, TE>(T value) => Ok(value);

    /// <summary>
    /// 允許將錯誤值隱式轉換為 <see cref="Result{T,E}"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T, TE>(TE error) => Err(error);

    /// <summary>
    /// 允許將 <see cref="Ok{T}"/> 隱式轉換為 <see cref="Result{T,E}"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T, TE>(Ok<T> ok) => Ok(ok.Value);

    /// <summary>
    /// 允許將 <see cref="Err{TE}"/> 隱式轉換為 <see cref="Result{T,E}"/>。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T, TE>(Err<TE> err) => Err(err.Error);

    #endregion

    #region Match

    /// <summary>
    /// 根據操作是否成功，執行對應的委派並回傳結果。
    /// </summary>
    /// <typeparam name="TResult">回傳值的類型。</typeparam>
    /// <param name="ok">成功時執行的函數。</param>
    /// <param name="err">失敗時執行的函數。</param>
    /// <returns>執行對應委派後產生的結果。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要根據結果將流程分岔，並將兩種結果統一轉換為同一種型別（例如回傳 HTTP Response）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 將結果轉換為 HTTP 狀態碼與訊息
    /// var response = result.Match(
    ///     ok: user => Ok(new { user.Id, user.Name }),
    ///     err: error => BadRequest(new { Error = error })
    /// );
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>(Func<T, TResult> ok, Func<TE, TResult> err)
    {
        ThrowIfUninitialized();
        return IsOk ? ok(_value) : err(_error);
    }

    /// <summary>
    /// 根據操作是否成功，執行對應的委派（無回傳值版本）。
    /// </summary>
    /// <param name="ok">成功時執行的動作。</param>
    /// <param name="err">失敗時執行的動作。</param>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你只需要執行副作用（如 Console.WriteLine），不需要回傳值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// result.Match(
    ///     ok: value => Console.WriteLine($"Success: {value}"),
    ///     err: error => Console.WriteLine($"Error: {error}")
    /// );
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Match(Action<T> ok, Action<TE> err)
    {
        ThrowIfUninitialized();
        if (IsOk)
        {
            ok(_value);
        }
        else
        {
            err(_error);
        }
    }

    #endregion

    #region Map / MapErr

    /// <summary>
    /// 如果成功，則映射成功的值。
    /// </summary>
    /// <typeparam name="TU">轉換後的類型。</typeparam>
    /// <param name="mapper">映射函數。</param>
    /// <returns>若原先為成功，回傳轉換後的新 Result；若失敗則保留原錯誤。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對成功的值進行轉換（例如資料格式轉換、數學運算），且轉換過程**不會失敗**時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 將 User 物件轉換為 UserDto
    /// Result&lt;UserDto, Error&gt; dtoResult = userResult.Map(user => user.ToDto());
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TU, TE> Map<TU>(Func<T, TU> mapper) where TU : notnull
    {
        ThrowIfUninitialized();
        return IsOk ? Result<TU, TE>.Ok(mapper(_value)) : Result<TU, TE>.Err(_error);
    }

    /// <summary>
    /// 如果失敗，則映射錯誤的值。
    /// </summary>
    /// <typeparam name="TF">轉換後的錯誤類型。</typeparam>
    /// <param name="mapper">錯誤映射函數。</param>
    /// <returns>若原先為失敗，回傳轉換後錯誤的新 Result；若成功則保留原值。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要轉換錯誤類型以符合介面要求（例如將 DomainError 轉換為 ApiError）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 將底層資料庫錯誤轉換為高層應用錯誤
    /// var apiResult = dbResult.MapErr(dbErr => new ApiError(dbErr.Code));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TF> MapErr<TF>(Func<TE, TF> mapper) where TF : notnull
    {
        ThrowIfUninitialized();
        return IsOk ? Result<T, TF>.Ok(_value) : Result<T, TF>.Err(mapper(_error));
    }

    #endregion

    #region Bind

    /// <summary>
    /// 如果成功，則將其映射到另一個 <see cref="Result{U,E}"/>。
    /// </summary>
    /// <typeparam name="TU">轉換後的類型。</typeparam>
    /// <param name="binder">回傳 Result 的映射函數。</param>
    /// <returns>若原先為成功，回傳 binder 的執行結果；若失敗則保留原錯誤。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>
    /// 當你需要串接多個**可能失敗**的操作時使用。這是「鐵路導向程式設計（ROP）」的核心方法。
    /// 例如：驗證輸入 -> 查詢資料庫 -> 執行業務邏輯。任一步驟失敗都會中斷流程並回傳錯誤。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 驗證輸入，成功後查詢使用者，再計算權限
    /// var result = ValidateInput(input)
    ///     .Bind(validInput => GetUser(validInput.UserId))
    ///     .Bind(user => CalculatePermission(user));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TU, TE> Bind<TU>(Func<T, Result<TU, TE>> binder) where TU : notnull
    {
        ThrowIfUninitialized();
        return IsOk ? binder(_value) : Result<TU, TE>.Err(_error);
    }

    #endregion

    #region Or / OrElse

    /// <summary>
    /// 如果失敗，則回傳替代的 Result。
    /// </summary>
    /// <param name="other">替代的 Result。</param>
    /// <returns>若成功則回傳自身；若失敗則回傳 <paramref name="other"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你有備用的資料來源（如快取失敗則查資料庫），且備用來源的代價較低（因為 <paramref name="other"/> 會被立即求值）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 嘗試從主庫讀取，失敗則從備庫讀取
    /// var result = ReadPrimary().Or(ReadSecondary());
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> Or(Result<T, TE> other)
    {
        ThrowIfUninitialized();
        return IsOk ? this : other;
    }

    /// <summary>
    /// 如果失敗，則執行工廠函數取得替代的 Result（惰性求值）。
    /// </summary>
    /// <param name="factory">產生替代 Result 的工廠函數，接收當前錯誤作為參數。</param>
    /// <returns>若成功則回傳自身；若失敗則回傳工廠函數的結果。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>
    /// 類似 <see cref="Or"/>，但備用操作代價較高（如 API 呼叫）或需要根據錯誤訊息決定策略時使用。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 嘗試從快取讀取，失敗則從資料庫查詢（資料庫查詢僅在快取失敗時執行）
    /// var result = GetFromCache(key)
    ///     .OrElse(err => GetFromDatabase(key));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> OrElse(Func<TE, Result<T, TE>> factory)
    {
        ThrowIfUninitialized();
        return IsOk ? this : factory(_error);
    }

    #endregion

    #region Tap / TapErr

    /// <summary>
    /// 如果成功，則執行指定的動作（不改變 Result）。
    /// </summary>
    /// <param name="action">要執行的動作。</param>
    /// <returns>原始 Result，用於鏈式呼叫。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>用於執行副作用，如記錄日誌（Logging）、發送通知，而不干擾主要的業務流程。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 成功時記錄日誌
    /// var result = ProcessOrder(order)
    ///     .Tap(order => _logger.LogInformation("Order processed: {Id}", order.Id));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> Tap(Action<T> action)
    {
        ThrowIfUninitialized();
        if (IsOk)
        {
            action(_value);
        }

        return this;
    }

    /// <summary>
    /// 如果失敗，則執行指定的動作（不改變 Result）。
    /// </summary>
    /// <param name="action">要執行的動作。</param>
    /// <returns>原始 Result，用於鏈式呼叫。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>用於記錄錯誤日誌或執行失敗後的清理工作，而不改變錯誤結果。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 失敗時記錄錯誤
    /// var result = ProcessOrder(order)
    ///     .TapErr(err => _logger.LogError("Order processing failed: {Error}", err));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> TapErr(Action<TE> action)
    {
        ThrowIfUninitialized();

        if (!IsOk)
        {
            action(_error);
        }

        return this;
    }

    #endregion

    #region Unwrap

    /// <summary>
    /// 嘗試取出成功的值，類似 C# 的 TryParse 模式。
    /// </summary>
    /// <param name="value">若成功則輸出該值；若失敗則輸出 default。</param>
    /// <returns>若成功則為 <c>true</c>；若失敗則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你只關心成功值，且希望使用類似 <c>if (TryParse(...))</c> 的語法結構時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (result.TryGetOk(out var user))
    /// {
    ///     Console.WriteLine($"Found user: {user.Name}");
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOk([MaybeNullWhen(false)] out T value)
    {
        ThrowIfUninitialized();

        if (IsOk)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 嘗試取出成功的值與錯誤資訊，類似 Rust 的 <c>let Ok(x) = result else { ... }</c> 模式。
    /// </summary>
    /// <param name="value">若成功則輸出該值；若失敗則輸出 default。</param>
    /// <param name="error">若失敗則輸出錯誤；若成功則輸出 default。</param>
    /// <returns>若成功則為 <c>true</c>；若失敗則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>
    /// 推薦用於 guard clause（守衛子句）。若失敗則立即處理錯誤並返回，若成功則繼續執行，
    /// 這樣可以減少巢狀結構（Arrow code）。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Guard clause 模式
    /// if (!result.TryGetOk(out var user, out var error))
    /// {
    ///     return BadRequest(error); // 失敗分支
    /// }
    ///
    /// // 成功分支，此處 user 保證有值
    /// return Ok(user);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOk(
        [MaybeNullWhen(false)] out T value,
        [MaybeNullWhen(true)] out TE error)
    {
        ThrowIfUninitialized();

        if (IsOk)
        {
            value = _value;
            error = default;
            return true;
        }

        value = default;
        error = _error;
        return false;
    }

    /// <summary>
    /// 嘗試取出錯誤資訊，類似 C# 的 TryParse 模式。
    /// </summary>
    /// <param name="error">若失敗則輸出錯誤；若成功則輸出 default。</param>
    /// <returns>若失敗則為 <c>true</c>；若成功則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你只關心錯誤處理，或者想使用 Guard clause 儘早處理錯誤時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Guard clause: 若有錯誤則處理並返回
    /// if (result.TryGetErr(out var err))
    /// {
    ///     _logger.LogError("Failed: {Error}", err);
    ///     return;
    /// }
    /// // 此處保證成功
    /// var value = result.Unwrap();
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetErr([MaybeNullWhen(false)] out TE error)
    {
        ThrowIfUninitialized();

        if (!IsOk)
        {
            error = _error;
            return true;
        }

        error = default;
        return false;
    }

    /// <summary>
    /// 取出成功的值。若為失敗則拋出例外。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>僅在**非常確定**結果不可能失敗，或者失敗時應該視為程式 bug（拋出例外）的情況下使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap()
    {
        ThrowIfUninitialized();
        return IsOk ? _value : throw new InvalidOperationException($"Result is Err: {_error}");
    }

    /// <summary>
    /// 取出成功的值。若為失敗則回傳指定的預設值。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當失敗時可以安全地使用預設值替代（例如設定預設參數、降級處理）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 讀取設定失敗則使用預設逾時時間
    /// var timeout = GetConfig("Timeout").UnwrapOr(30);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnwrapOr(T defaultValue)
    {
        ThrowIfUninitialized();
        return IsOk ? _value : defaultValue;
    }

    /// <summary>
    /// 取出成功的值。若為失敗則執行工廠函數取得預設值（惰性求值）。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>類似 <see cref="UnwrapOr"/>，但預設值的產生代價較高，或需要根據錯誤內容決定預設值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var value = result.UnwrapOrElse(err =>
    /// {
    ///     _logger.LogWarning("Fallback due to: {Error}", err);
    ///     return ComputeExpensiveDefault();
    /// });
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnwrapOrElse(Func<TE, T> factory)
    {
        ThrowIfUninitialized();
        return IsOk ? _value : factory(_error);
    }

    /// <summary>
    /// 取出成功的值。若為失敗則拋出包含自定義訊息的例外。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>類似 <see cref="Unwrap"/>，但希望能提供更清楚的錯誤訊息以便除錯。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = LoadConfig().Expect("設定檔載入失敗，無法啟動系統");
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Expect(string message)
    {
        ThrowIfUninitialized();
        return IsOk ? _value : throw new InvalidOperationException(message);
    }

    /// <summary>
    /// 取出錯誤資訊。若為成功則拋出例外。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>通常用於測試程式碼，驗證某個操作是否如預期般失敗。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TE UnwrapErr()
    {
        ThrowIfUninitialized();
        return IsOk ? throw new InvalidOperationException("Result is Ok") : _error;
    }

    /// <summary>
    /// 取出錯誤資訊。若為成功則拋出包含自定義訊息的例外。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>通常用於測試程式碼，驗證某個操作是否如預期般失敗，並提供清楚的測試失敗訊息。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var error = result.ExpectErr("此操作應該要失敗但卻成功了");
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TE ExpectErr(string message)
    {
        ThrowIfUninitialized();
        return IsErr ? _error : throw new InvalidOperationException(message);
    }

    #endregion

    #region ToOption / Err

    /// <summary>
    /// 將 Result 轉換為 Option，只保留成功的值（丟棄錯誤資訊）。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你只關心操作是否成功，而不關心失敗的具體原因時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var maybeUser = GetUser(id).ToOption();
    /// // maybeUser 為 Option&lt;User&gt;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> ToOption()
    {
        ThrowIfUninitialized();
        return IsOk ? Option<T>.Some(_value) : Option<T>.None;
    }

    /// <summary>
    /// 將 Result 轉換為 Option，只保留錯誤資訊（丟棄成功值）。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要收集錯誤列表，或者只關心操作失敗的狀況時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 收集所有驗證錯誤
    /// var errors = validations
    ///     .Select(v => v.Err())
    ///     .Where(opt => opt.IsSome)
    ///     .Select(opt => opt.Unwrap())
    ///     .ToList();
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TE> Err()
    {
        ThrowIfUninitialized();
        return IsOk ? Option<TE>.None : Option<TE>.Some(_error);
    }

    #endregion

    #region LINQ Support

    /// <summary>
    /// LINQ Select 支援，等同於 <see cref="Map{TU}"/>。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 LINQ 的查詢語法 (Query Syntax)，讓代碼更具可讀性。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = from user in GetUser(id)
    ///              select user.Name;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TU, TE> Select<TU>(Func<T, TU> selector) where TU : notnull => Map(selector);

    /// <summary>
    /// LINQ SelectMany 支援，等同於 <see cref="Bind{TU}"/>。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 LINQ 的多重 <c>from</c> 查詢，這在處理多個相依的 Result 時非常強大，能避免多層巢狀縮排。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 依序取得使用者、訂單，最後計算總額
    /// // 任一步驟失敗都會直接回傳錯誤
    /// var total = from user in GetUser(id)
    ///             from order in GetLatestOrder(user.Id)
    ///             select order.Total;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TResult, TE> SelectMany<TU, TResult>(
        Func<T, Result<TU, TE>> selector,
        Func<T, TU, TResult> resultSelector)
        where TU : notnull
        where TResult : notnull
    {
        ThrowIfUninitialized();
        if (!IsOk)
        {
            return Result<TResult, TE>.Err(_error);
        }

        var intermediate = selector(_value);
        return intermediate.IsOk
            ? Result<TResult, TE>.Ok(resultSelector(_value, intermediate.Unwrap()))
            : Result<TResult, TE>.Err(intermediate.UnwrapErr());
    }

    #endregion

    #region Functional Extensions (Rust-like)

    /// <summary>
    /// 如果成功，應用映射函數；否則回傳預設值。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要將 Result 統一轉換為某個值，且失敗時有固定的預設值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 若成功則轉為字串，失敗則回傳 "Empty"
    /// var msg = result.MapOr("Empty", v => v.ToString());
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TU MapOr<TU>(TU defaultValue, Func<T, TU> mapper) where TU : notnull
    {
        ThrowIfUninitialized();
        return IsOk ? mapper(_value) : defaultValue;
    }

    /// <summary>
    /// 如果成功，應用映射函數；否則應用錯誤處理函數。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要將 Result 統一轉換為某個值，且成功與失敗的轉換邏輯不同時使用（類似 <see cref="Match{TResult}"/>）。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 若成功則轉為字串，失敗則回傳錯誤訊息
    /// var msg = result.MapOrElse(
    ///     err => $"Error: {err}",
    ///     val => $"Value: {val}"
    /// );
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TU MapOrElse<TU>(Func<TE, TU> fallback, Func<T, TU> mapper) where TU : notnull
    {
        ThrowIfUninitialized();
        return IsOk ? mapper(_value) : fallback(_error);
    }

    #endregion

    #region Deconstruct

    /// <summary>
    /// 將 <see cref="Result{T, TE}"/> 解構為元組 <c>(bool isOk, T? value, TE? error)</c>。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 C# 的解構語法與 Pattern Matching，特別適合在 switch 表達式中使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var message = result switch
    /// {
    ///     (true, var v, _) => $"Success: {v}",
    ///     (false, _, var e) => $"Error: {e}"
    /// };
    /// </code>
    /// </example>
    public void Deconstruct(out bool isOk, out T? value, out TE? error)
    {
        if (IsOk)
        {
            isOk = true;
            value = _value;
            error = default;
        }
        else
        {
            isOk = false;
            value = default;
            error = _error;
        }
    }

    #endregion

    #region Equality

    /// <inheritdoc/>
    public bool Equals(Result<T, TE> other)
    {
        if (IsOk != other.IsOk)
        {
            return false;
        }

        return IsOk
            ? EqualityComparer<T>.Default.Equals(_value, other._value)
            : EqualityComparer<TE>.Default.Equals(_error, other._error);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Result<T, TE> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => IsOk
        ? HashCode.Combine(true, _value)
        : HashCode.Combine(false, _error);

    /// <inheritdoc/>
    public override string ToString() => IsOk ? $"Ok({_value})" : $"Err({_error})";

    /// <summary>比較兩個 Result 是否相等。</summary>
    public static bool operator ==(Result<T, TE> left, Result<T, TE> right) => left.Equals(right);

    /// <summary>比較兩個 Result 是否不相等。</summary>
    public static bool operator !=(Result<T, TE> left, Result<T, TE> right) => !left.Equals(right);

    #endregion
}
