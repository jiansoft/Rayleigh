namespace jIAnSoft.Rayleigh;

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
/// <para>
/// 每個組合子都提供兩種多載：一種接受單參數委派（維持原有呼叫方式），
/// 另一種額外接受 <see cref="CancellationToken"/> 並將其一併傳入委派，方便在鏈式呼叫中傳遞取消訊號。
/// 兩者以委派的參數個數區分多載，彼此不會互相影響；只有在實際要執行委派時才會檢查取消狀態，
/// 短路（已經是 Err，或 Tap 系列在非對應分支）的情況下不會拋出 <see cref="OperationCanceledException"/>。
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
///
/// // 傳遞 CancellationToken，讓後續步驟也能觀察取消訊號
/// var result2 = await GetUserAsync(userId)
///     .BindAsync((user, ct) => GetOrdersAsync(user.Id, ct), cancellationToken);
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
        return result.TryGetOk(out var value, out var error)
            ? await binder(value).ConfigureAwait(false)
            : Result<TU, TE>.Err(error);
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Bind{U}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="binder"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<TU, TE>> BindAsync<T, TU, TE>(
        this Task<Result<T, TE>> task,
        Func<T, CancellationToken, Task<Result<TU, TE>>> binder,
        CancellationToken cancellationToken = default) where TE : notnull where T : notnull where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetOk(out var value, out var error))
        {
            return Result<TU, TE>.Err(error);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(value, cancellationToken).ConfigureAwait(false);
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
        return result.TryGetOk(out var value, out var error)
            ? Result<TU, TE>.Ok(await mapper(value).ConfigureAwait(false))
            : Result<TU, TE>.Err(error);
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Map{U}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="mapper"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<TU, TE>> MapAsync<T, TU, TE>(
        this Task<Result<T, TE>> task,
        Func<T, CancellationToken, Task<TU>> mapper,
        CancellationToken cancellationToken = default) where TE : notnull where T : notnull where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetOk(out var value, out var error))
        {
            return Result<TU, TE>.Err(error);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TU, TE>.Ok(await mapper(value, cancellationToken).ConfigureAwait(false));
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
        return result.TryGetOk(out var value, out var error)
            ? Result<T, TF>.Ok(value)
            : Result<T, TF>.Err(await mapper(error).ConfigureAwait(false));
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.MapErr{F}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="mapper"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this Task<Result<T, TE>> task,
        Func<TE, CancellationToken, Task<TF>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
        where TF : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetOk(out var value, out var error))
        {
            return Result<T, TF>.Ok(value);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<T, TF>.Err(await mapper(error, cancellationToken).ConfigureAwait(false));
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
        return result.TryGetErr(out var error)
            ? await factory(error).ConfigureAwait(false)
            : result;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.OrElse"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="factory"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<T, TE>> OrElseAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<TE, CancellationToken, Task<Result<T, TE>>> factory,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetErr(out var error))
        {
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(error, cancellationToken).ConfigureAwait(false);
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
        if (result.TryGetOk(out var value))
        {
            await action(value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.Tap"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="action"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<T, TE>> TapAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<T, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetOk(out var value))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(value, cancellationToken).ConfigureAwait(false);
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
        if (result.TryGetErr(out var error))
        {
            await action(error).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Result{T,E}.TapErr"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="action"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<T, TE>> TapErrAsync<T, TE>(
        this Task<Result<T, TE>> task,
        Func<TE, CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetErr(out var error))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(error, cancellationToken).ConfigureAwait(false);
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
        return result.TryGetOk(out var value, out var error)
            ? await binder(value).ConfigureAwait(false)
            : Result<TU, TE>.Err(error);
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="BindAsync{T,TU,TE}(Task{Result{T,TE}}, Func{T, CancellationToken, Task{Result{TU,TE}}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<TU, TE>> BindAsync<T, TU, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, CancellationToken, ValueTask<Result<TU, TE>>> binder,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where T : notnull
        where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetOk(out var value, out var error))
        {
            return Result<TU, TE>.Err(error);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(value, cancellationToken).ConfigureAwait(false);
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
        return result.TryGetOk(out var value, out var error)
            ? Result<TU, TE>.Ok(await mapper(value).ConfigureAwait(false))
            : Result<TU, TE>.Err(error);
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapAsync{T,TU,TE}(Task{Result{T,TE}}, Func{T, CancellationToken, Task{TU}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<TU, TE>> MapAsync<T, TU, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, CancellationToken, ValueTask<TU>> mapper,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where T : notnull
        where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetOk(out var value, out var error))
        {
            return Result<TU, TE>.Err(error);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TU, TE>.Ok(await mapper(value, cancellationToken).ConfigureAwait(false));
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
        return result.TryGetOk(out var value, out var error)
            ? Result<T, TF>.Ok(value)
            : Result<T, TF>.Err(await mapper(error).ConfigureAwait(false));
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapErrAsync{T,TE,TF}(Task{Result{T,TE}}, Func{TE, CancellationToken, Task{TF}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, CancellationToken, ValueTask<TF>> mapper,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where TF : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetOk(out var value, out var error))
        {
            return Result<T, TF>.Ok(value);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<T, TF>.Err(await mapper(error, cancellationToken).ConfigureAwait(false));
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
        return result.TryGetErr(out var error)
            ? await factory(error).ConfigureAwait(false)
            : result;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="OrElseAsync{T,TE}(Task{Result{T,TE}}, Func{TE, CancellationToken, Task{Result{T,TE}}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<T, TE>> OrElseAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, CancellationToken, ValueTask<Result<T, TE>>> factory,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.TryGetErr(out var error))
        {
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(error, cancellationToken).ConfigureAwait(false);
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
        if (result.TryGetOk(out var value))
        {
            await action(value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="TapAsync{T,TE}(Task{Result{T,TE}}, Func{T, CancellationToken, Task}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<T, TE>> TapAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<T, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetOk(out var value))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(value, cancellationToken).ConfigureAwait(false);
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
        if (result.TryGetErr(out var error))
        {
            await action(error).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="TapErrAsync{T,TE}(Task{Result{T,TE}}, Func{TE, CancellationToken, Task}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若原先為成功，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Result<T, TE>> TapErrAsync<T, TE>(
        this ValueTask<Result<T, TE>> task,
        Func<TE, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default)
        where TE : notnull
        where T : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (result.TryGetErr(out var error))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(error, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    // ==========================================
    // Sync Source（以同步的 Result<T, TE> 為起點）
    // ==========================================

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，串接一個非同步的 binder。
    /// </summary>
    /// <typeparam name="T">原始成功值的類型。</typeparam>
    /// <typeparam name="TU">轉換後成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤的類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="binder">非同步映射函數。</param>
    /// <returns>若成功則為 <paramref name="binder"/> 的結果；若失敗則為帶有相同錯誤的 <see cref="Result{TU,TE}"/>。</returns>
    /// <remarks>
    /// <para><b>為什麼需要這個多載</b></para>
    /// <para>
    /// 其餘的 <c>BindAsync</c> 多載都以 <see cref="Task{TResult}"/>／<see cref="ValueTask{TResult}"/> 為 <c>this</c>，
    /// 因此當管線的<b>起點</b>是一個同步取得的 <see cref="Result{T,TE}"/> 時，呼叫端過去只能寫成
    /// <c>Task.FromResult(result).BindAsync(...)</c>——這會為了接上型別而多配置一個 <see cref="Task{TResult}"/>。
    /// </para>
    /// <para><b>零配置的短路路徑</b></para>
    /// <para>
    /// 本方法刻意<b>不</b>宣告為 <c>async</c>：失敗時直接以同步完成的 <see cref="ValueTask{TResult}"/> 回傳，
    /// 完全不會建立 async 狀態機，也不會有任何 heap 配置；成功時則直接回傳 <paramref name="binder"/> 的
    /// <see cref="ValueTask{TResult}"/>，不額外包裝一層。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Before：為了型別相容而多配置一個 Task
    /// var r1 = await Task.FromResult(Validate(input)).BindAsync(v => SaveAsync(v));
    ///
    /// // After：直接串接，Err 短路時零配置
    /// var r2 = await Validate(input).BindAsync(v => SaveAsync(v));
    /// </code>
    /// </example>
    public static ValueTask<Result<TU, TE>> BindAsync<T, TU, TE>(
        this Result<T, TE> result,
        Func<T, ValueTask<Result<TU, TE>>> binder)
        where T : notnull
        where TU : notnull
        where TE : notnull
        => result.TryGetOk(out var value, out var error)
            ? binder(value)
            : new ValueTask<Result<TU, TE>>(Result<TU, TE>.Err(error));

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，串接一個非同步的 binder，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若來源為 Err，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<TU, TE>> BindAsync<T, TU, TE>(
        this Result<T, TE> result,
        Func<T, CancellationToken, ValueTask<Result<TU, TE>>> binder,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
        where TE : notnull
    {
        if (!result.TryGetOk(out var value, out var error))
        {
            return new ValueTask<Result<TU, TE>>(Result<TU, TE>.Err(error));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return binder(value, cancellationToken);
    }

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，對成功值套用一個非同步的 mapper。
    /// </summary>
    /// <typeparam name="T">原始成功值的類型。</typeparam>
    /// <typeparam name="TU">轉換後成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤的類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="mapper">非同步映射函數。</param>
    /// <returns>若成功則為包含轉換後值的結果；若失敗則保留原錯誤。</returns>
    /// <remarks>失敗時走同步完成路徑，不建立 async 狀態機（詳見 <see cref="BindAsync{T,TU,TE}(Result{T,TE}, Func{T, ValueTask{Result{TU,TE}}})"/> 的備註）。</remarks>
    public static ValueTask<Result<TU, TE>> MapAsync<T, TU, TE>(
        this Result<T, TE> result,
        Func<T, ValueTask<TU>> mapper)
        where T : notnull
        where TU : notnull
        where TE : notnull
        => result.TryGetOk(out var value, out var error)
            ? MapCore<TU, TE>(mapper(value))
            : new ValueTask<Result<TU, TE>>(Result<TU, TE>.Err(error));

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，對成功值套用一個非同步的 mapper，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若來源為 Err，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<TU, TE>> MapAsync<T, TU, TE>(
        this Result<T, TE> result,
        Func<T, CancellationToken, ValueTask<TU>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
        where TE : notnull
    {
        if (!result.TryGetOk(out var value, out var error))
        {
            return new ValueTask<Result<TU, TE>>(Result<TU, TE>.Err(error));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return MapCore<TU, TE>(mapper(value, cancellationToken));
    }

    /// <summary>
    /// 將 <c>ValueTask&lt;TU&gt;</c> 包裝回 <c>ValueTask&lt;Result&lt;TU, TE&gt;&gt;</c>。
    /// </summary>
    /// <remarks>
    /// 抽出為獨立的區域方法，是為了讓 <c>MapAsync</c> 的 Err 短路路徑維持非 async——
    /// 只有真的需要 await mapper 結果時才進入 async 狀態機。
    /// </remarks>
    private static async ValueTask<Result<TU, TE>> MapCore<TU, TE>(ValueTask<TU> pending)
        where TU : notnull
        where TE : notnull
        => Result<TU, TE>.Ok(await pending.ConfigureAwait(false));

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，對錯誤套用一個非同步的 mapper。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">原始錯誤類型。</typeparam>
    /// <typeparam name="TF">轉換後錯誤類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="mapper">非同步錯誤映射函數。</param>
    /// <returns>若失敗則為包含轉換後錯誤的結果；若成功則保留原值。</returns>
    /// <remarks>Ok 時走同步完成路徑，不建立 async 狀態機。</remarks>
    public static ValueTask<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this Result<T, TE> result,
        Func<TE, ValueTask<TF>> mapper)
        where T : notnull
        where TE : notnull
        where TF : notnull
        => result.TryGetOk(out var value, out var error)
            ? new ValueTask<Result<T, TF>>(Result<T, TF>.Ok(value))
            : MapErrCore<T, TF>(mapper(error));

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，對錯誤套用非同步 mapper，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若來源為 Ok，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<T, TF>> MapErrAsync<T, TE, TF>(
        this Result<T, TE> result,
        Func<TE, CancellationToken, ValueTask<TF>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
        where TF : notnull
    {
        if (result.TryGetOk(out var value, out var error))
        {
            return new ValueTask<Result<T, TF>>(Result<T, TF>.Ok(value));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return MapErrCore<T, TF>(mapper(error, cancellationToken));
    }

    /// <summary>
    /// 將 <c>ValueTask&lt;TF&gt;</c> 包裝回 <c>ValueTask&lt;Result&lt;T, TF&gt;&gt;</c>，理由同 <see cref="MapCore{TU,TE}"/>。
    /// </summary>
    private static async ValueTask<Result<T, TF>> MapErrCore<T, TF>(ValueTask<TF> pending)
        where T : notnull
        where TF : notnull
        => Result<T, TF>.Err(await pending.ConfigureAwait(false));

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，失敗時執行非同步的備援工廠函數。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="factory">非同步備援工廠函數，接收目前的錯誤。</param>
    /// <returns>若成功則回傳自身；若失敗則回傳工廠函數的結果。</returns>
    /// <remarks>
    /// <para>
    /// 這是「快取命中則直接使用、未命中才非同步抓取」模式的自然寫法——
    /// 來源是同步取得的快取查詢結果，備援才是非同步的資料庫／API 呼叫。
    /// </para>
    /// <para>Ok 時走同步完成路徑，不建立 async 狀態機，也不會執行 <paramref name="factory"/>。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var user = await cache.Lookup(id)                  // 同步：Result&lt;User, Error&gt;
    ///     .OrElseAsync(err =&gt; repository.FindAsync(id));  // 非同步備援
    /// </code>
    /// </example>
    public static ValueTask<Result<T, TE>> OrElseAsync<T, TE>(
        this Result<T, TE> result,
        Func<TE, ValueTask<Result<T, TE>>> factory)
        where T : notnull
        where TE : notnull
        => result.TryGetErr(out var error)
            ? factory(error)
            : new ValueTask<Result<T, TE>>(result);

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點的非同步備援，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若來源為 Ok，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<T, TE>> OrElseAsync<T, TE>(
        this Result<T, TE> result,
        Func<TE, CancellationToken, ValueTask<Result<T, TE>>> factory,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        if (!result.TryGetErr(out var error))
        {
            return new ValueTask<Result<T, TE>>(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return factory(error, cancellationToken);
    }

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，成功時執行非同步的副作用（不改變結果）。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="action">非同步動作。</param>
    /// <returns>原始結果，用於鏈式呼叫。</returns>
    /// <remarks>Err 時走同步完成路徑，不建立 async 狀態機，也不會執行 <paramref name="action"/>。</remarks>
    public static ValueTask<Result<T, TE>> TapAsync<T, TE>(
        this Result<T, TE> result,
        Func<T, ValueTask> action)
        where T : notnull
        where TE : notnull
        => result.TryGetOk(out var value)
            ? TapCore(result, action(value))
            : new ValueTask<Result<T, TE>>(result);

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點的非同步副作用，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若來源為 Err，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<T, TE>> TapAsync<T, TE>(
        this Result<T, TE> result,
        Func<T, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        if (!result.TryGetOk(out var value))
        {
            return new ValueTask<Result<T, TE>>(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return TapCore(result, action(value, cancellationToken));
    }

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點，失敗時執行非同步的副作用（不改變結果）。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="result">來源結果。</param>
    /// <param name="action">非同步動作，接收錯誤。</param>
    /// <returns>原始結果，用於鏈式呼叫。</returns>
    /// <remarks>Ok 時走同步完成路徑，不建立 async 狀態機，也不會執行 <paramref name="action"/>。</remarks>
    public static ValueTask<Result<T, TE>> TapErrAsync<T, TE>(
        this Result<T, TE> result,
        Func<TE, ValueTask> action)
        where T : notnull
        where TE : notnull
        => result.TryGetErr(out var error)
            ? TapCore(result, action(error))
            : new ValueTask<Result<T, TE>>(result);

    /// <summary>
    /// 以同步的 <see cref="Result{T,TE}"/> 為起點的非同步錯誤副作用，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="action"/> 時才檢查取消狀態；若來源為 Ok，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Result<T, TE>> TapErrAsync<T, TE>(
        this Result<T, TE> result,
        Func<TE, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TE : notnull
    {
        if (!result.TryGetErr(out var error))
        {
            return new ValueTask<Result<T, TE>>(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return TapCore(result, action(error, cancellationToken));
    }

    /// <summary>
    /// 等待副作用完成後回傳原始結果，供 Tap 系列的 sync-source 多載共用。
    /// </summary>
    /// <remarks>
    /// 抽出為獨立方法，是為了讓 Tap 系列的短路路徑維持非 async——
    /// 只有真的需要 await 副作用時才進入 async 狀態機。
    /// </remarks>
    private static async ValueTask<Result<T, TE>> TapCore<T, TE>(Result<T, TE> result, ValueTask pending)
        where T : notnull
        where TE : notnull
    {
        await pending.ConfigureAwait(false);
        return result;
    }
}
