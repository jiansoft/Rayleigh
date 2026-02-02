using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh;

/// <summary>
/// 表示一個可能有值或沒有值的選項類型，用於取代 null 以明確表達「無值」的語意。
/// </summary>
/// <typeparam name="T">包含值的類型，必須為非 null 的類型。</typeparam>
/// <remarks>
/// <para><b>設計目的</b></para>
/// <para>
/// <see cref="Option{T}"/> 是一種函數式程式設計的基礎型別，用於明確表達「值可能不存在」的語意。
/// 相較於使用 <c>null</c> 來表示無值，<see cref="Option{T}"/> 提供了以下優勢：
/// </para>
/// <list type="bullet">
///   <item><description>編譯時期強制處理無值情況，避免 <see cref="NullReferenceException"/></description></item>
///   <item><description>明確的 API 語意：函數簽章清楚表明回傳值可能不存在</description></item>
///   <item><description>支援函數式組合操作（Map、Bind），便於鏈式處理</description></item>
///   <item><description>與 LINQ 風格一致的操作方式</description></item>
/// </list>
///
/// <para><b>效能特性</b></para>
/// <para>
/// 此型別為 <c>readonly struct</c>，具備以下效能優勢：
/// </para>
/// <list type="bullet">
///   <item><description>零 Heap 分配：整個結構儲存於 Stack</description></item>
///   <item><description>AggressiveInlining：關鍵方法會被 JIT 內聯</description></item>
///   <item><description>大小等同於 T 加上一個 bool 欄位</description></item>
/// </list>
///
/// <para><b>使用範例</b></para>
/// <code>
/// // 建立 Option
/// var some = Option&lt;int&gt;.Some(42);
/// var none = Option&lt;int&gt;.None;
///
/// // 使用 Match 處理兩種情況
/// var message = some.Match(
///     some: value => "Value is " + value,
///     none: () => "No value"
/// );
///
/// // 使用 Map 轉換值
/// var doubled = some.Map(x => x * 2);  // Some(84)
///
/// // 使用 Bind 串接可能無值的操作
/// Option&lt;User&gt; FindUser(int id) => ...;
/// Option&lt;Address&gt; GetAddress(User user) => ...;
///
/// var address = FindUser(123).Bind(GetAddress);
/// </code>
/// </remarks>
/// <example>
/// <para><b>實際應用場景：查詢使用者</b></para>
/// <code>
/// public Option&lt;User&gt; FindUserById(Guid id)
/// {
///     var user = _repository.Find(id);
///     return user is not null
///         ? Option&lt;User&gt;.Some(user)
///         : Option&lt;User&gt;.None;
/// }
///
/// // 呼叫端
/// var greeting = FindUserById(userId).Match(
///     some: user => $"Hello, {user.Name}!",
///     none: () => "User not found"
/// );
/// </code>
/// </example>
public readonly struct Option<T> : IEquatable<Option<T>> where T : notnull
{
    private readonly T? _value;

    /// <summary>
    /// 取得一個值，指出此選項是否包含值。
    /// </summary>
    /// <value>如果包含值則為 <c>true</c>；否則為 <c>false</c>。</value>
    /// <example>
    /// <code>
    /// var some = Option&lt;int&gt;.Some(42);
    /// var none = Option&lt;int&gt;.None;
    ///
    /// Console.WriteLine(some.IsSome);  // true
    /// Console.WriteLine(none.IsSome);  // false
    /// </code>
    /// </example>
    [MemberNotNullWhen(true, nameof(_value))]
    public bool IsSome { get; }

    /// <summary>
    /// 取得一個值，指出此選項是否為空（無值）。
    /// </summary>
    /// <value>如果為空則為 <c>true</c>；否則為 <c>false</c>。</value>
    /// <example>
    /// <code>
    /// var some = Option&lt;int&gt;.Some(42);
    /// var none = Option&lt;int&gt;.None;
    ///
    /// Console.WriteLine(some.IsNone);  // false
    /// Console.WriteLine(none.IsNone);  // true
    /// </code>
    /// </example>
    public bool IsNone
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsSome;
    }

    #region Contains / Conditional Checks

    /// <summary>
    /// 檢查 Option 是否包含指定的值。
    /// </summary>
    /// <param name="value">要檢查的值。</param>
    /// <returns>若有值且等於 <paramref name="value"/> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要快速驗證 Option 內的數值是否為特定值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var some = Option&lt;int&gt;.Some(42);
    /// var none = Option&lt;int&gt;.None;
    ///
    /// Console.WriteLine(some.Contains(42));  // true
    /// Console.WriteLine(some.Contains(100)); // false
    /// Console.WriteLine(none.Contains(42));  // false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T value) => IsSome && EqualityComparer<T>.Default.Equals(_value, value);

    /// <summary>
    /// 若有值且符合條件則回傳 true。
    /// </summary>
    /// <param name="predicate">要檢查的條件。</param>
    /// <returns>若有值且 <paramref name="predicate"/> 回傳 <c>true</c> 則為 <c>true</c>；否則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對 Option 內的值進行條件判斷，且希望無值狀態視為不符合條件時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var user = GetUser(id);
    ///
    /// // 檢查使用者是否存在且為活躍狀態
    /// if (user.IsSomeAnd(u => u.IsActive))
    /// {
    ///     // 使用者存在且活躍
    /// }
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSomeAnd(Func<T, bool> predicate) => IsSome && predicate(_value);

    #endregion

    /// <summary>
    /// 建立一個包含指定值的 <see cref="Option{T}"/>。
    /// </summary>
    /// <param name="value">要包含的值，不可為 <c>null</c>。</param>
    /// <exception cref="ArgumentNullException">當 <paramref name="value"/> 為 <c>null</c> 時擲出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Use Option.None for null values");
        }

        _value = value;
        IsSome = true;
    }

    /// <summary>
    /// 建立一個空的 <see cref="Option{T}"/>（無值）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option()
    {
        _value = default;
        IsSome = false;
    }

    #region Factory Methods

    /// <summary>
    /// 建立一個包含指定值的 <see cref="Option{T}"/>。
    /// </summary>
    /// <param name="value">要包含的值，不可為 <c>null</c>。</param>
    /// <returns>包含值的 <see cref="Option{T}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="value"/> 為 <c>null</c> 時擲出。</exception>
    /// <example>
    /// <code>
    /// var some = Option&lt;string&gt;.Some("Hello");
    /// Console.WriteLine(some.IsSome);  // true
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some(T value) => new(value);

    /// <summary>
    /// 取得一個空的 <see cref="Option{T}"/>（無值）。
    /// </summary>
    /// <value>空的 <see cref="Option{T}"/>。</value>
    /// <example>
    /// <code>
    /// var none = Option&lt;int&gt;.None;
    /// Console.WriteLine(none.IsNone);  // true
    /// </code>
    /// </example>
    public static Option<T> None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new();
    }

    #endregion

    #region Match

    /// <summary>
    /// 根據選項是否有值，執行對應的委派並回傳結果。
    /// </summary>
    /// <typeparam name="TResult">回傳值的類型。</typeparam>
    /// <param name="some">有值時執行的函數，接收值作為參數。</param>
    /// <param name="none">無值時執行的函數。</param>
    /// <returns>執行對應委派後產生的結果。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要根據 Option 的狀態將流程分岔，並將兩種結果統一轉換為同一種型別（例如給定預設值或格式化輸出）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var option = GetUserAge(userId);
    ///
    /// // 將 Option 轉換為字串
    /// var message = option.Match(
    ///     some: age => $"User is {age} years old",
    ///     none: () => "Age unknown"
    /// );
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => IsSome ? some(_value) : none();

    /// <summary>
    /// 根據選項是否有值，執行對應的委派（無回傳值版本）。
    /// </summary>
    /// <param name="some">有值時執行的動作。</param>
    /// <param name="none">無值時執行的動作。</param>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你只需要執行副作用（如 Console.WriteLine），不需要回傳值時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// option.Match(
    ///     some: value => Console.WriteLine($"Value: {value}"),
    ///     none: () => Console.WriteLine("No value")
    /// );
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Match(Action<T> some, Action none)
    {
        if (IsSome)
        {
            some(_value);
        }
        else
        {
            none();
        }
    }

    #endregion

    #region Map / Filter

    /// <summary>
    /// 如果有值，則映射該值到新的類型。
    /// </summary>
    /// <typeparam name="TU">轉換後的類型。</typeparam>
    /// <param name="mapper">映射函數，將值轉換為新類型。</param>
    /// <returns>
    /// 若原先有值，則回傳包含轉換後值的 <see cref="Option{U}"/>；
    /// 若原先無值，則回傳 <see cref="Option{U}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對 Option 內的值進行轉換（例如數學運算、屬性存取），且轉換過程**不會失敗**時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var some = Option&lt;int&gt;.Some(42);
    /// var doubled = some.Map(x => x * 2);  // Some(84)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TU> Map<TU>(Func<T, TU> mapper) where TU : notnull => IsSome ? Option<TU>.Some(mapper(_value)) : Option<TU>.None;

    /// <summary>
    /// 根據條件過濾值，若不符合條件則變成 <see cref="None"/>。
    /// </summary>
    /// <param name="predicate">過濾條件。</param>
    /// <returns>若有值且符合條件則回傳自身；否則回傳 <see cref="None"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對 Option 內的值進行驗證，若驗證失敗則視為無值時使用（例如過濾無效數據）。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var some = Option&lt;int&gt;.Some(42);
    /// var filtered = some.Filter(x => x > 50);  // None
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> Filter(Func<T, bool> predicate) => IsSome && predicate(_value) ? this : None;

    #endregion

    #region Bind

    /// <summary>
    /// 如果有值，則將其映射到另一個 <see cref="Option{U}"/>。
    /// </summary>
    /// <typeparam name="TU">轉換後的類型。</typeparam>
    /// <param name="binder">回傳 <see cref="Option{U}"/> 的映射函數。</param>
    /// <returns>
    /// 若原先有值，則回傳 <paramref name="binder"/> 的結果；
    /// 若原先無值，則回傳 <see cref="Option{U}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>
    /// 當你需要串接多個**可能無值**的操作時使用（例如：查詢使用者 -> 查詢地址 -> 查詢城市）。
    /// 任一步驟回傳 None，整個鏈條就會回傳 None。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Option&lt;User&gt; FindUser(int id) => ...;
    /// Option&lt;Address&gt; GetAddress(User user) => ...;
    ///
    /// // 串接操作
    /// var address = FindUser(123).Bind(GetAddress);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TU> Bind<TU>(Func<T, Option<TU>> binder) where TU : notnull => IsSome ? binder(_value) : Option<TU>.None;

    #endregion

    #region Zip

    /// <summary>
    /// 將兩個 <see cref="Option{T}"/> 組合成一個包含元組的 <see cref="Option{T}"/>。
    /// </summary>
    /// <typeparam name="TU">另一個 Option 的值類型。</typeparam>
    /// <param name="other">要組合的另一個 <see cref="Option{TU}"/>。</param>
    /// <returns>
    /// 若兩者都有值，則回傳 <c>Some((T, U))</c>；
    /// 若任一者為 <see cref="None"/>，則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要同時擁有兩個可能不存在的值才能進行後續操作時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var firstName = Option&lt;string&gt;.Some("John");
    /// var lastName = Option&lt;string&gt;.Some("Doe");
    ///
    /// var fullName = firstName.Zip(lastName); // Some(("John", "Doe"))
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<(T, TU)> Zip<TU>(Option<TU> other) where TU : notnull
        => IsSome && other.IsSome ? Option<(T, TU)>.Some((_value, other.Unwrap())) : Option<(T, TU)>.None;

    /// <summary>
    /// 將兩個 <see cref="Option{T}"/> 組合，並使用指定的函數進行轉換。
    /// </summary>
    /// <typeparam name="TU">另一個 Option 的值類型。</typeparam>
    /// <typeparam name="TResult">轉換後的結果類型。</typeparam>
    /// <param name="other">要組合的另一個 <see cref="Option{TU}"/>。</param>
    /// <param name="zipper">組合兩個值的函數。</param>
    /// <returns>
    /// 若兩者都有值，則回傳 <c>Some(zipper(T, U))</c>；
    /// 若任一者為 <see cref="None"/>，則回傳 <see cref="Option{TResult}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要對兩個可能不存在的值進行計算（如：寬 * 高 = 面積），且任一值不存在則結果無效時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var width = Option&lt;int&gt;.Some(10);
    /// var height = Option&lt;int&gt;.Some(20);
    ///
    /// var area = width.ZipWith(height, (w, h) => w * h); // Some(200)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TResult> ZipWith<TU, TResult>(Option<TU> other, Func<T, TU, TResult> zipper)
        where TU : notnull
        where TResult : notnull
        => IsSome && other.IsSome ? Option<TResult>.Some(zipper(_value, other.Unwrap())) : Option<TResult>.None;

    #endregion

    #region Or / OrElse

    /// <summary>
    /// 如果無值，則回傳替代的 Option。
    /// </summary>
    /// <param name="other">替代的 Option。</param>
    /// <returns>若有值則回傳自身；若無值則回傳 <paramref name="other"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你有備用的 Option 來源，且備用來源的代價較低（因為 <paramref name="other"/> 會被立即求值）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = Option&lt;int&gt;.None.Or(Option&lt;int&gt;.Some(42)); // Some(42)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> Or(Option<T> other) => IsSome ? this : other;

    /// <summary>
    /// 如果無值，則執行工廠函數取得替代的 Option（惰性求值）。
    /// </summary>
    /// <param name="factory">產生替代 Option 的工廠函數。</param>
    /// <returns>若有值則回傳自身；若無值則回傳工廠函數的結果。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當備用 Option 的產生代價較高（如查詢資料庫），或需要延遲執行時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = GetFromCache().OrElse(() => GetFromDatabase());
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> OrElse(Func<Option<T>> factory) => IsSome ? this : factory();

    #endregion

    #region Tap

    /// <summary>
    /// 如果有值，則執行指定的動作（不改變 Option）。
    /// </summary>
    /// <param name="action">要執行的動作。</param>
    /// <returns>原始 Option，用於鏈式呼叫。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>用於執行副作用，如記錄日誌（Logging），而不干擾主要的業務流程。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = option.Tap(v => Console.WriteLine($"Found: {v}"));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> Tap(Action<T> action)
    {
        if (IsSome)
        {
            action(_value);
        }

        return this;
    }

    #endregion

    #region Unwrap

    /// <summary>
    /// 嘗試取出值，類似 C# 的 TryParse 模式。
    /// </summary>
    /// <param name="value">若有值則輸出該值；若無值則輸出 default。</param>
    /// <returns>若有值則為 <c>true</c>；若無值則為 <c>false</c>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>適合用於 guard clause（防禦性檢查）場景，若無值則提前返回。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!option.TryGetValue(out var value))
    /// {
    ///     return;
    /// }
    /// // 此處 value 保證有值
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsSome)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 取出值。若為 <see cref="IsNone"/> 則拋出例外。
    /// </summary>
    /// <returns>包含的值。</returns>
    /// <exception cref="InvalidOperationException">當 <see cref="Option{T}"/> 為 <see cref="IsNone"/> 時擲出。</exception>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>僅在**非常確定** Option 必定有值，或者無值時應該視為程式 bug（拋出例外）的情況下使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap() => IsSome ? _value : throw new InvalidOperationException("Option is None");

    /// <summary>
    /// 取出值。若為 <see cref="IsNone"/> 則回傳指定的預設值。
    /// </summary>
    /// <param name="defaultValue">當為無值時回傳的預設值。</param>
    /// <returns>若有值則為該值；若無值則為 <paramref name="defaultValue"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當無值時可以安全地使用預設值替代（例如顯示 "Unknown"）時使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnwrapOr(T defaultValue) => IsSome ? _value : defaultValue;

    /// <summary>
    /// 取出值。若為 <see cref="IsNone"/> 則執行工廠函數取得預設值（惰性求值）。
    /// </summary>
    /// <param name="factory">產生預設值的工廠函數。</param>
    /// <returns>若有值則為該值；若無值則為工廠函數的結果。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>類似 <see cref="UnwrapOr"/>，但預設值的產生代價較高（如計算或查詢）時使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnwrapOrElse(Func<T> factory) => IsSome ? _value : factory();

    /// <summary>
    /// 取出值。若為 <see cref="IsNone"/> 則拋出包含自定義訊息的例外。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <returns>包含的值。</returns>
    /// <exception cref="InvalidOperationException">當 <see cref="Option{T}"/> 為 <see cref="IsNone"/> 時擲出。</exception>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>類似 <see cref="Unwrap"/>，但希望能提供更清楚的錯誤訊息以便除錯。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Expect(string message) => IsSome ? _value : throw new InvalidOperationException(message);

    #endregion

    #region ToResult

    /// <summary>
    /// 將 Option 轉換為 Result，無值時使用提供的錯誤。
    /// </summary>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="error">當為 <see cref="None"/> 時使用的錯誤值。</param>
    /// <returns>若有值則回傳 <see cref="Result{T,TE}.Ok(T)"/>；若無值則回傳 <see cref="Result{T,TE}.Err(TE)"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要將「可能無值」的概念轉換為「可能失敗」的概念（例如找不到資料視為錯誤）時使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = option.ToResult("User not found");
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> ToResult<TE>(TE error) where TE : notnull =>
        IsSome ? Result<T, TE>.Ok(_value) : Result<T, TE>.Err(error);

    /// <summary>
    /// 將 Option 轉換為 Result，無值時使用工廠函數產生錯誤（惰性求值）。
    /// </summary>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="errorFactory">產生錯誤值的工廠函數。</param>
    /// <returns>若有值則回傳 <see cref="Result{T,TE}.Ok(T)"/>；若無值則回傳 <see cref="Result{T,TE}.Err(TE)"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>類似 <see cref="ToResult{TE}(TE)"/>，但錯誤物件產生代價較高或需要動態產生時使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T, TE> ToResult<TE>(Func<TE> errorFactory) where TE : notnull =>
        IsSome ? Result<T, TE>.Ok(_value) : Result<T, TE>.Err(errorFactory());

    #endregion

    #region LINQ Support

    /// <summary>
    /// LINQ Select 支援，等同於 <see cref="Map{TU}"/>。
    /// </summary>
    /// <typeparam name="TU">轉換後的類型。</typeparam>
    /// <param name="selector">選擇器函數。</param>
    /// <returns>轉換後的 Option。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 LINQ 的查詢語法 (Query Syntax)，讓代碼更具可讀性。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = from user in FindUser(id)
    ///              select user.Name;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TU> Select<TU>(Func<T, TU> selector) where TU : notnull => Map(selector);

    /// <summary>
    /// LINQ SelectMany 支援，等同於 <see cref="Bind{TU}"/>。
    /// </summary>
    /// <typeparam name="TU">中間類型。</typeparam>
    /// <typeparam name="TResult">最終類型。</typeparam>
    /// <param name="selector">選擇器函數。</param>
    /// <param name="resultSelector">結果選擇器。</param>
    /// <returns>轉換後的 Option。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 LINQ 的多重 <c>from</c> 查詢，這在處理多個相依的 Option 時非常強大，能避免多層巢狀縮排。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = from user in FindUser(id)
    ///              from order in GetLatestOrder(user.Id)
    ///              select order.Total;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TResult> SelectMany<TU, TResult>(
        Func<T, Option<TU>> selector,
        Func<T, TU, TResult> resultSelector)
        where TU : notnull
        where TResult : notnull
    {
        if (!IsSome)
        {
            return Option<TResult>.None;
        }

        var intermediate = selector(_value);
        return intermediate.TryGetValue(out var value)
            ? Option<TResult>.Some(resultSelector(_value, value))
            : Option<TResult>.None;
    }

    /// <summary>
    /// LINQ Where 支援，等同於 <see cref="Filter"/>。
    /// </summary>
    /// <param name="predicate">過濾條件。</param>
    /// <returns>若有值且符合條件則回傳自身；否則回傳 <see cref="None"/>。</returns>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 LINQ 的 <c>where</c> 子句，用於在查詢表達式中進行過濾。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = from value in option
    ///              where value > 0
    ///              select value;
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<T> Where(Func<T, bool> predicate) => Filter(predicate);

    #endregion

    #region Deconstruct

    /// <summary>
    /// 將 <see cref="Option{T}"/> 解構為元組 <c>(bool isSome, T? value)</c>，支援 C# Pattern Matching 語法。
    /// </summary>
    /// <param name="isSome">輸出參數，指出此 Option 是否包含值。</param>
    /// <param name="value">輸出參數，包含的值或 <c>default</c>。</param>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>支援 C# 的解構語法與 Pattern Matching，特別適合在 switch 表達式或解構賦值中使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (hasValue, val) = option;
    ///
    /// var message = option switch
    /// {
    ///     (true, var v) => $"Value: {v}",
    ///     (false, _) => "No value"
    /// };
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out bool isSome, out T? value)
    {
        // 使用 T? 讓編譯器知道 value 可能為 null
        // 配合 IsSome 的 [MemberNotNullWhen] 進行流程分析
        if (IsSome)
        {
            isSome = true;
            value = _value;
        }
        else
        {
            isSome = false;
            value = default;  // T? 允許 null，無需 null-forgiving operator (!)
        }
    }

    #endregion

    #region Functional Extensions (Rust-like)

    /// <summary>
    /// 如果有值，應用映射函數；否則回傳預設值。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要將 Option 統一轉換為某個值，且無值時有固定的預設值時使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TU MapOr<TU>(TU defaultValue, Func<T, TU> mapper) where TU : notnull
        => IsSome ? mapper(_value) : defaultValue;

    /// <summary>
    /// 如果有值，應用映射函數；否則執行預設值工廠函數。
    /// </summary>
    /// <remarks>
    /// <para><b>適用情境</b></para>
    /// <para>當你需要將 Option 統一轉換為某個值，且無值時的預設值需要計算（惰性求值）時使用。</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TU MapOrElse<TU>(Func<TU> defaultFactory, Func<T, TU> mapper) where TU : notnull
        => IsSome ? mapper(_value) : defaultFactory();

    #endregion

    #region Equality

    /// <summary>
    /// 判斷目前的 <see cref="Option{T}"/> 是否與另一個 <see cref="Option{T}"/> 相等。
    /// </summary>
    /// <param name="other">要比較的另一個 <see cref="Option{T}"/>。</param>
    /// <returns>
    /// 若兩者都有值且值相等，或都無值，則為 <c>true</c>；否則為 <c>false</c>。
    /// </returns>
    /// <example>
    /// <code>
    /// var a = Option&lt;int&gt;.Some(42);
    /// var b = Option&lt;int&gt;.Some(42);
    /// var c = Option&lt;int&gt;.None;
    ///
    /// Console.WriteLine(a.Equals(b));  // true
    /// Console.WriteLine(a.Equals(c));  // false
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Option<T> other)
    {
        if (IsSome != other.IsSome)
        {
            return false;
        }

        return !IsSome || EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => IsSome ? HashCode.Combine(true, _value) : HashCode.Combine(false);

    /// <inheritdoc/>
    public override string ToString() => IsSome ? $"Some({_value})" : "None";

    /// <summary>
    /// 比較兩個 <see cref="Option{T}"/> 是否相等。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>
    /// 比較兩個 <see cref="Option{T}"/> 是否不相等。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    #endregion
}
