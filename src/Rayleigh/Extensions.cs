using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh;

/// <summary>
/// 提供將可空類型轉換為 <see cref="Option{T}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法讓您可以輕鬆地將現有的可空參考類型或 <see cref="Nullable{T}"/>
/// 轉換為 <see cref="Option{T}"/>，以便使用函數式風格處理可能不存在的值。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 從可空參考類型轉換
/// string? name = GetName();
/// var option = name.ToOption();  // Some("value") 或 None
///
/// // 從 Nullable&lt;T&gt; 轉換
/// int? age = GetAge();
/// var ageOption = age.ToOption();  // Some(25) 或 None
///
/// // 結合 LINQ 風格操作
/// var greeting = GetUser()
///     .ToOption()
///     .Map(user => user.Name)
///     .Map(name => $"Hello, {name}!")
///     .UnwrapOr("Hello, Guest!");
/// </code>
/// </example>
public static class NullableExtensions
{
    /// <summary>
    /// 將可空參考類型轉換為 <see cref="Option{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為參考類型。</typeparam>
    /// <param name="value">可能為 <c>null</c> 的值。</param>
    /// <returns>
    /// 若 <paramref name="value"/> 不為 <c>null</c>，則回傳 <see cref="Option{T}.Some"/>；
    /// 否則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <example>
    /// <code>
    /// string? name = "Alice";
    /// var some = name.ToOption();  // Some("Alice")
    ///
    /// string? nullName = null;
    /// var none = nullName.ToOption();  // None
    ///
    /// // 實際應用：從字典取值
    /// var dict = new Dictionary&lt;string, User&gt;();
    /// var user = dict.GetValueOrDefault("key").ToOption();
    /// </code>
    /// </example>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is not null ? Option<T>.Some(value) : Option<T>.None;

    /// <summary>
    /// 將 <see cref="Nullable{T}"/> 轉換為 <see cref="Option{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為值類型。</typeparam>
    /// <param name="value">可能為 <c>null</c> 的 <see cref="Nullable{T}"/>。</param>
    /// <returns>
    /// 若 <paramref name="value"/> 有值，則回傳 <see cref="Option{T}.Some"/>；
    /// 否則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <example>
    /// <code>
    /// int? age = 25;
    /// var some = age.ToOption();  // Some(25)
    ///
    /// int? nullAge = null;
    /// var none = nullAge.ToOption();  // None
    ///
    /// // 實際應用：解析可能失敗的操作
    /// int? parsed = int.TryParse(input, out var result) ? result : null;
    /// var option = parsed.ToOption();
    /// </code>
    /// </example>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}

/// <summary>
/// 提供 <see cref="Option{T}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法提供額外的功能，特別是那些需要特定泛型約束的操作，
/// 例如 <see cref="Flatten{T}(Option{Option{T}})"/> 需要巢狀的 <see cref="Option{T}"/>。
/// </para>
/// </remarks>
public static class OptionExtensions
{
    /// <summary>
    /// 展平巢狀的 <see cref="Option{T}"/>，將 <c>Option&lt;Option&lt;T&gt;&gt;</c> 轉換為 <c>Option&lt;T&gt;</c>。
    /// </summary>
    /// <typeparam name="T">內部 Option 的值類型。</typeparam>
    /// <param name="nested">巢狀的 Option。</param>
    /// <returns>
    /// 若外層和內層都是 Some，則回傳內層的值；
    /// 若任一層為 None，則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法對應 Rust 的 <c>flatten()</c> 方法，用於移除一層 Option 包裝。
    /// 這在處理回傳 <c>Option&lt;Option&lt;T&gt;&gt;</c> 的操作時特別有用。
    /// </para>
    /// <para>
    /// <c>Flatten</c> 等同於 <c>Bind(x => x)</c>，但語意更明確。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 巢狀 Option 的情況
    /// Option&lt;Option&lt;int&gt;&gt; nested = Option&lt;Option&lt;int&gt;&gt;.Some(Option&lt;int&gt;.Some(42));
    /// var flattened = nested.Flatten();
    /// // flattened: Some(42)
    ///
    /// // 外層為 Some，內層為 None
    /// Option&lt;Option&lt;int&gt;&gt; nested2 = Option&lt;Option&lt;int&gt;&gt;.Some(Option&lt;int&gt;.None);
    /// var flattened2 = nested2.Flatten();
    /// // flattened2: None
    ///
    /// // 外層為 None
    /// Option&lt;Option&lt;int&gt;&gt; nested3 = Option&lt;Option&lt;int&gt;&gt;.None;
    /// var flattened3 = nested3.Flatten();
    /// // flattened3: None
    ///
    /// // 實際應用：Map 後可能產生巢狀 Option
    /// Option&lt;string&gt; TryParse(string input) => ...;
    /// var input = Option&lt;string&gt;.Some("42");
    /// var parsed = input.Map(TryParse).Flatten();  // 等同於 input.Bind(TryParse)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Flatten<T>(this Option<Option<T>> nested) where T : notnull => nested.Bind(inner => inner);
}

/// <summary>
/// 提供 <see cref="Result{T,E}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法提供額外的功能，特別是那些需要特定泛型約束的操作，
/// 例如 <see cref="Flatten{T, TE}(Result{Result{T, TE}, TE})"/> 需要巢狀的 <see cref="Result{T,E}"/>。
/// </para>
/// </remarks>
public static class ResultExtensions
{
    /// <summary>
    /// 展平巢狀的 <see cref="Result{T,E}"/>，將 <c>Result&lt;Result&lt;T, E&gt;, E&gt;</c> 轉換為 <c>Result&lt;T, E&gt;</c>。
    /// </summary>
    /// <typeparam name="T">內部 Result 的成功值類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="nested">巢狀的 Result。</param>
    /// <returns>
    /// 若外層和內層都是 Ok，則回傳內層的成功值；
    /// 若任一層為 Err，則回傳該層的錯誤。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法對應 Rust 的 <c>flatten()</c> 方法，用於移除一層 Result 包裝。
    /// 這在處理回傳 <c>Result&lt;Result&lt;T, E&gt;, E&gt;</c> 的操作時特別有用。
    /// </para>
    /// <para>
    /// <c>Flatten</c> 等同於 <c>Bind(x => x)</c>，但語意更明確。
    /// </para>
    /// <para><b>錯誤處理</b></para>
    /// <para>
    /// 當外層為 Err 時，回傳外層的錯誤；
    /// 當外層為 Ok 但內層為 Err 時，回傳內層的錯誤。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 巢狀 Result 的情況
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Ok(Result&lt;int, string&gt;.Ok(42));
    /// var flattened = nested.Flatten();
    /// // flattened: Ok(42)
    ///
    /// // 外層為 Ok，內層為 Err
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested2 =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Ok(Result&lt;int, string&gt;.Err("inner error"));
    /// var flattened2 = nested2.Flatten();
    /// // flattened2: Err("inner error")
    ///
    /// // 外層為 Err
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested3 =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Err("outer error");
    /// var flattened3 = nested3.Flatten();
    /// // flattened3: Err("outer error")
    ///
    /// // 實際應用：Map 後可能產生巢狀 Result
    /// Result&lt;User, Error&gt; ValidateUser(UserDto dto) => ...;
    /// var dto = Result&lt;UserDto, Error&gt;.Ok(new UserDto(...));
    /// var user = dto.Map(ValidateUser).Flatten();  // 等同於 dto.Bind(ValidateUser)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, TE> Flatten<T, TE>(this Result<Result<T, TE>, TE> nested)
        where T : notnull
        where TE : notnull
        => nested.Bind(inner => inner);
}

/// <summary>
/// 提供 <see cref="IEnumerable{T}"/> 的擴充方法。
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// 從 <see cref="Option{T}"/> 集合中過濾出所有的 Some 值。
    /// </summary>
    /// <typeparam name="T">值的類型。</typeparam>
    /// <param name="source">Option 集合。</param>
    /// <returns>包含所有 Some 值的集合。</returns>
    /// <example>
    /// <code>
    /// var options = new[] { Option&lt;int&gt;.Some(1), Option&lt;int&gt;.None, Option&lt;int&gt;.Some(3) };
    /// var values = options.Values();  // [1, 3]
    /// </code>
    /// </example>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source) where T : notnull
    {
        foreach (var option in source)
        {
            if (option.TryGetValue(out var value))
            {
                yield return value;
            }
        }
    }
}

/// <summary>
/// 提供 <see cref="Option{T}"/> 的非同步擴充方法。
/// </summary>
public static class OptionAsyncExtensions
{
    // ==========================================
    // Task Support
    // ==========================================

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.Bind{U}"/>。
    /// </summary>
    public static async Task<Option<TU>> BindAsync<T, TU>(
        this Task<Option<T>> task,
        Func<T, Task<Option<TU>>> binder)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome
            ? await binder(option.Unwrap()).ConfigureAwait(false)
            : Option<TU>.None;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.Map{U}"/>。
    /// </summary>
    public static async Task<Option<TU>> MapAsync<T, TU>(
        this Task<Option<T>> task,
        Func<T, Task<TU>> mapper)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome
            ? Option<TU>.Some(await mapper(option.Unwrap()).ConfigureAwait(false))
            : Option<TU>.None;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.OrElse"/>.
    /// </summary>
    public static async Task<Option<T>> OrElseAsync<T>(
        this Task<Option<T>> task,
        Func<Task<Option<T>>> factory)
        where T : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome ? option : await factory().ConfigureAwait(false);
    }

    // ==========================================
    // ValueTask Support
    // ==========================================

    /// <summary>
    /// ValueTask 版本的 <see cref="BindAsync{T,TU}(Task{Option{T}}, Func{T, Task{Option{TU}}})"/>。
    /// </summary>
    public static async ValueTask<Option<TU>> BindAsync<T, TU>(
        this ValueTask<Option<T>> task,
        Func<T, ValueTask<Option<TU>>> binder)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome
            ? await binder(option.Unwrap()).ConfigureAwait(false)
            : Option<TU>.None;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapAsync{T,TU}(Task{Option{T}}, Func{T, Task{TU}})"/>。
    /// </summary>
    public static async ValueTask<Option<TU>> MapAsync<T, TU>(
        this ValueTask<Option<T>> task,
        Func<T, ValueTask<TU>> mapper)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome
            ? Option<TU>.Some(await mapper(option.Unwrap()).ConfigureAwait(false))
            : Option<TU>.None;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="OrElseAsync{T}(Task{Option{T}}, Func{Task{Option{T}}})"/>。
    /// </summary>
    public static async ValueTask<Option<T>> OrElseAsync<T>(
        this ValueTask<Option<T>> task,
        Func<ValueTask<Option<T>>> factory)
        where T : notnull
    {
        var option = await task.ConfigureAwait(false);
        return option.IsSome ? option : await factory().ConfigureAwait(false);
    }
}

/// <summary>
/// 提供 <see cref="Result{T,E}"/> 的非同步擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法讓您可以在非同步環境中使用 <see cref="Result{T,E}"/> 的組合操作，
/// 例如 BindAsync 和 MapAsync。
/// </para>
/// <para>
/// 所有方法都使用 <c>ConfigureAwait(false)</c> 以避免不必要的同步上下文捕獲，
/// 提升效能並避免潛在的死鎖問題。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 非同步鏈式操作
/// var result = await GetUserAsync(userId)
///     .BindAsync(user => GetOrdersAsync(user.Id))
///     .MapAsync(orders => orders.Sum(o => o.Total));
///
/// // 結合同步與非同步操作
/// var greeting = await GetUserAsync(userId)
///     .MapAsync(user => $"Hello, {user.Name}!");
/// </code>
/// </example>
public static class ResultAsyncExtensions
{
    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Bind{U}"/>。
    /// 若成功，則將結果映射到另一個非同步 <see cref="Result{U,E}"/>。
    /// </summary>
    /// <typeparam name="T">原始成功值的類型。</typeparam>
    /// <typeparam name="TU">轉換後成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤的類型。</typeparam>
    /// <param name="task">包含 <see cref="Result{T,E}"/> 的非同步任務。</param>
    /// <param name="binder">非同步映射函數，將成功值轉換為 <see cref="Result{U,E}"/>。</param>
    /// <returns>
    /// 若原先為成功，則回傳 <paramref name="binder"/> 的非同步結果；
    /// 若原先為失敗，則回傳包含相同錯誤的 <see cref="Result{U,E}"/>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法用於串接非同步的可能失敗操作，實現非同步版本的「鐵路導向程式設計」。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 定義非同步操作
    /// Task&lt;Result&lt;User, Error&gt;&gt; GetUserAsync(Guid id) => ...;
    /// Task&lt;Result&lt;Order, Error&gt;&gt; GetLatestOrderAsync(Guid userId) => ...;
    ///
    /// // 使用 BindAsync 串接
    /// var order = await GetUserAsync(userId)
    ///     .BindAsync(user => GetLatestOrderAsync(user.Id));
    ///
    /// // 多層串接
    /// var total = await GetUserAsync(userId)
    ///     .BindAsync(user => GetLatestOrderAsync(user.Id))
    ///     .BindAsync(order => CalculateTotalAsync(order));
    /// </code>
    /// </example>
    public static async Task<Result<TU, TE>> BindAsync<T, TU, TE>(
        this Task<Result<T, TE>> task,
        Func<T, Task<Result<TU, TE>>> binder) where TE : notnull where T : notnull where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? await binder(result.Unwrap()).ConfigureAwait(false)
            : Result<TU, TE>.Err(result.UnwrapErr());
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Map{U}"/>。
    /// 若成功，則將結果值非同步映射到新的類型。
    /// </summary>
    /// <typeparam name="T">原始成功值的類型。</typeparam>
    /// <typeparam name="TU">轉換後成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤的類型。</typeparam>
    /// <param name="task">包含 <see cref="Result{T,E}"/> 的非同步任務。</param>
    /// <param name="mapper">非同步映射函數，將成功值轉換為新類型。</param>
    /// <returns>
    /// 若原先為成功，則回傳包含轉換後值的 <see cref="Result{U,E}"/>；
    /// 若原先為失敗，則回傳包含相同錯誤的 <see cref="Result{U,E}"/>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法用於對 <see cref="Result{T,E}"/> 內的成功值進行非同步轉換，
    /// 而無需手動處理非同步邏輯。錯誤會自動傳遞。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 定義非同步轉換
    /// async Task&lt;UserDto&gt; ToUserDtoAsync(User user)
    /// {
    ///     var avatar = await _avatarService.GetAvatarAsync(user.Id);
    ///     return new UserDto(user.Id, user.Name, avatar);
    /// }
    ///
    /// // 使用 MapAsync
    /// var dto = await GetUserAsync(userId)
    ///     .MapAsync(user => ToUserDtoAsync(user));
    ///
    /// // 鏈式操作
    /// var greeting = await GetUserAsync(userId)
    ///     .MapAsync(async user =>
    ///     {
    ///         var settings = await GetSettingsAsync(user.Id);
    ///         return $"Hello, {user.Name}! Language: {settings.Language}";
    ///     });
    /// </code>
    /// </example>
    public static async Task<Result<TU, TE>> MapAsync<T, TU, TE>(
        this Task<Result<T, TE>> task,
        Func<T, Task<TU>> mapper) where TE : notnull where T : notnull where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? Result<TU, TE>.Ok(await mapper(result.Unwrap()).ConfigureAwait(false))
            : Result<TU, TE>.Err(result.UnwrapErr());
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.MapErr{F}"/>。
    /// 若失敗，則將錯誤非同步映射到新的類型。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">原始錯誤類型。</typeparam>
    /// <typeparam name="TF">轉換後錯誤類型。</typeparam>
    /// <param name="task">包含 Result 的非同步任務。</param>
    /// <param name="mapper">非同步錯誤映射函數。</param>
    /// <example>
    /// <code>
    /// var result = await GetUserAsync(id)
    ///     .MapErrAsync(async err => await TranslateErrorAsync(err));
    /// </code>
    /// </example>
    public static async Task<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this Task<Result<T, TE>> task,
        Func<TE, Task<TF>> mapper)
        where T : notnull
        where TE : notnull
        where TF : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? Result<T, TF>.Ok(result.Unwrap())
            : Result<T, TF>.Err(await mapper(result.UnwrapErr()).ConfigureAwait(false));
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.OrElse"/>。
    /// 若失敗，則執行非同步工廠函數取得替代結果。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="task">包含 Result 的非同步任務。</param>
    /// <param name="factory">非同步工廠函數。</param>
    /// <example>
    /// <code>
    /// var result = await GetFromCacheAsync(key)
    ///     .OrElseAsync(err => GetFromDatabaseAsync(key));
    /// </code>
    /// </example>
    public static async Task<Result<T, TE>> OrElseAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<TE, Task<Result<T, TE>>> factory)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? result
            : await factory(result.UnwrapErr()).ConfigureAwait(false);
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Tap"/>。
    /// 若成功，則執行非同步動作（不改變 Result）。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="task">包含 Result 的非同步任務。</param>
    /// <param name="action">非同步動作。</param>
    /// <example>
    /// <code>
    /// var result = await ProcessOrderAsync(order)
    ///     .TapAsync(async receipt => await SendEmailAsync(receipt));
    /// </code>
    /// </example>
    public static async Task<Result<T, TE>> TapAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<T, Task> action)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsOk)
        {
            await action(result.Unwrap()).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.TapErr"/>。
    /// 若失敗，則執行非同步動作（不改變 Result）。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="task">包含 Result 的非同步任務。</param>
    /// <param name="action">非同步動作。</param>
    /// <example>
    /// <code>
    /// var result = await ProcessOrderAsync(order)
    ///     .TapErrAsync(async err => await LogErrorAsync(err));
    /// </code>
    /// </example>
    public static async Task<Result<T, TE>> TapErrAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<TE, Task> action)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsErr)
        {
            await action(result.UnwrapErr()).ConfigureAwait(false);
        }

        return result;
    }

    // ==========================================
    // ValueTask Support
    // ==========================================

    /// <summary>
    /// ValueTask 版本的 <see cref="BindAsync{T,TU,TE}(Task{Result{T,TE}}, Func{T, Task{Result{TU,TE}}})"/>。
    /// </summary>
    public static async ValueTask<Result<TU, TE>> BindAsync<T, TU, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, ValueTask<Result<TU, TE>>> binder)
        where TE : notnull
        where T : notnull
        where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? await binder(result.Unwrap()).ConfigureAwait(false)
            : Result<TU, TE>.Err(result.UnwrapErr());
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapAsync{T,TU,TE}(Task{Result{T,TE}}, Func{T, Task{TU}})"/>。
    /// </summary>
    public static async ValueTask<Result<TU, TE>> MapAsync<T, TU, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, ValueTask<TU>> mapper)
        where TE : notnull
        where T : notnull
        where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? Result<TU, TE>.Ok(await mapper(result.Unwrap()).ConfigureAwait(false))
            : Result<TU, TE>.Err(result.UnwrapErr());
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapErrAsync{T,TE,TF}(Task{Result{T,TE}}, Func{TE, Task{TF}})"/>。
    /// </summary>
    public static async ValueTask<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, ValueTask<TF>> mapper)
        where TE : notnull
        where TF : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? Result<T, TF>.Ok(result.Unwrap())
            : Result<T, TF>.Err(await mapper(result.UnwrapErr()).ConfigureAwait(false));
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="OrElseAsync{T,TE}(Task{Result{T,TE}}, Func{TE, Task{Result{T,TE}}})"/>。
    /// </summary>
    public static async ValueTask<Result<T, TE>> OrElseAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, ValueTask<Result<T, TE>>> factory)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        return result.IsOk
            ? result
            : await factory(result.UnwrapErr()).ConfigureAwait(false);
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="TapAsync{T,TE}(Task{Result{T,TE}}, Func{T, Task})"/>。
    /// </summary>
    public static async ValueTask<Result<T, TE>> TapAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, ValueTask> action)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsOk)
        {
            await action(result.Unwrap()).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="TapErrAsync{T,TE}(Task{Result{T,TE}}, Func{TE, Task})"/>。
    /// </summary>
    public static async ValueTask<Result<T, TE>> TapErrAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, ValueTask> action)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.IsErr)
        {
            await action(result.UnwrapErr()).ConfigureAwait(false);
        }

        return result;
    }
}
