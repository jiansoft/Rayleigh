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
        return result.IsOk
            ? await binder(result.Unwrap()).ConfigureAwait(false)
            : Result<TU, TE>.Err(result.UnwrapErr());
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
        if (!result.IsOk)
        {
            return Result<TU, TE>.Err(result.UnwrapErr());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(result.Unwrap(), cancellationToken).ConfigureAwait(false);
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
    /// 非同步版本的 <see cref="Result{T,E}.Map{U}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="mapper"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若原先為失敗，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Result<TU, TE>> MapAsync<T, TU, TE>(
        this Task<Result<T, TE>> task,
        Func<T, CancellationToken, Task<TU>> mapper,
        CancellationToken cancellationToken = default) where TE : notnull where T : notnull where TU : notnull
    {
        var result = await task.ConfigureAwait(false);
        if (!result.IsOk)
        {
            return Result<TU, TE>.Err(result.UnwrapErr());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TU, TE>.Ok(await mapper(result.Unwrap(), cancellationToken).ConfigureAwait(false));
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
        if (result.IsOk)
        {
            return Result<T, TF>.Ok(result.Unwrap());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<T, TF>.Err(await mapper(result.UnwrapErr(), cancellationToken).ConfigureAwait(false));
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
        if (result.IsOk)
        {
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(result.UnwrapErr(), cancellationToken).ConfigureAwait(false);
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
        if (result.IsOk)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(result.Unwrap(), cancellationToken).ConfigureAwait(false);
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
        if (result.IsErr)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(result.UnwrapErr(), cancellationToken).ConfigureAwait(false);
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
        if (!result.IsOk)
        {
            return Result<TU, TE>.Err(result.UnwrapErr());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(result.Unwrap(), cancellationToken).ConfigureAwait(false);
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
        if (!result.IsOk)
        {
            return Result<TU, TE>.Err(result.UnwrapErr());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<TU, TE>.Ok(await mapper(result.Unwrap(), cancellationToken).ConfigureAwait(false));
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
        if (result.IsOk)
        {
            return Result<T, TF>.Ok(result.Unwrap());
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Result<T, TF>.Err(await mapper(result.UnwrapErr(), cancellationToken).ConfigureAwait(false));
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
        if (result.IsOk)
        {
            return result;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(result.UnwrapErr(), cancellationToken).ConfigureAwait(false);
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
        if (result.IsOk)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(result.Unwrap(), cancellationToken).ConfigureAwait(false);
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
        if (result.IsErr)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(result.UnwrapErr(), cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}
