namespace jIAnSoft.Rayleigh;

/// <summary>
/// 提供 <see cref="Option{T}"/> 的非同步擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 每個組合子都提供兩種多載：一種接受單參數委派（維持原有呼叫方式），
/// 另一種額外接受 <see cref="CancellationToken"/> 並將其一併傳入委派，方便在鏈式呼叫中傳遞取消訊號。
/// 兩者以委派的參數個數區分多載，彼此不會互相影響。
/// </para>
/// </remarks>
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
        return option.TryGetValue(out var value)
            ? await binder(value).ConfigureAwait(false)
            : Option<TU>.None;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.Bind{U}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="binder"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.None"/>，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Option<TU>> BindAsync<T, TU>(
        this Task<Option<T>> task,
        Func<T, CancellationToken, Task<Option<TU>>> binder,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (!option.TryGetValue(out var value))
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(value, cancellationToken).ConfigureAwait(false);
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
        return option.TryGetValue(out var value)
            ? Option<TU>.Some(await mapper(value).ConfigureAwait(false))
            : Option<TU>.None;
    }

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.Map{U}"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="mapper"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.None"/>，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Option<TU>> MapAsync<T, TU>(
        this Task<Option<T>> task,
        Func<T, CancellationToken, Task<TU>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (!option.TryGetValue(out var value))
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Option<TU>.Some(await mapper(value, cancellationToken).ConfigureAwait(false));
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

    /// <summary>
    /// 非同步版本的 <see cref="Option{T}.OrElse"/>，並將 <see cref="CancellationToken"/> 傳入 <paramref name="factory"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.Some"/>，本方法為 no-op，不會拋出。</remarks>
    public static async Task<Option<T>> OrElseAsync<T>(
        this Task<Option<T>> task,
        Func<CancellationToken, Task<Option<T>>> factory,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (option.IsSome)
        {
            return option;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(cancellationToken).ConfigureAwait(false);
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
        return option.TryGetValue(out var value)
            ? await binder(value).ConfigureAwait(false)
            : Option<TU>.None;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="BindAsync{T,TU}(Task{Option{T}}, Func{T, CancellationToken, Task{Option{TU}}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.None"/>，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Option<TU>> BindAsync<T, TU>(
        this ValueTask<Option<T>> task,
        Func<T, CancellationToken, ValueTask<Option<TU>>> binder,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (!option.TryGetValue(out var value))
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(value, cancellationToken).ConfigureAwait(false);
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
        return option.TryGetValue(out var value)
            ? Option<TU>.Some(await mapper(value).ConfigureAwait(false))
            : Option<TU>.None;
    }

    /// <summary>
    /// ValueTask 版本的 <see cref="MapAsync{T,TU}(Task{Option{T}}, Func{T, CancellationToken, Task{TU}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.None"/>，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Option<TU>> MapAsync<T, TU>(
        this ValueTask<Option<T>> task,
        Func<T, CancellationToken, ValueTask<TU>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (!option.TryGetValue(out var value))
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Option<TU>.Some(await mapper(value, cancellationToken).ConfigureAwait(false));
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

    /// <summary>
    /// ValueTask 版本的 <see cref="OrElseAsync{T}(Task{Option{T}}, Func{CancellationToken, Task{Option{T}}}, CancellationToken)"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若來源為 <see cref="Option{T}.Some"/>，本方法為 no-op，不會拋出。</remarks>
    public static async ValueTask<Option<T>> OrElseAsync<T>(
        this ValueTask<Option<T>> task,
        Func<CancellationToken, ValueTask<Option<T>>> factory,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        var option = await task.ConfigureAwait(false);
        if (option.IsSome)
        {
            return option;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await factory(cancellationToken).ConfigureAwait(false);
    }

    // ==========================================
    // Sync Source（以同步的 Option<T> 為起點）
    // ==========================================

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點，串接一個非同步的 binder。
    /// </summary>
    /// <typeparam name="T">原始值的類型。</typeparam>
    /// <typeparam name="TU">轉換後值的類型。</typeparam>
    /// <param name="option">來源 Option。</param>
    /// <param name="binder">非同步映射函數。</param>
    /// <returns>若有值則為 <paramref name="binder"/> 的結果；否則為 <see cref="Option{TU}.None"/>。</returns>
    /// <remarks>
    /// <para><b>為什麼需要這個多載</b></para>
    /// <para>
    /// 其餘的 <c>BindAsync</c> 多載都以 <see cref="Task{TResult}"/>／<see cref="ValueTask{TResult}"/> 為 <c>this</c>，
    /// 因此當管線的<b>起點</b>是一個同步取得的 <see cref="Option{T}"/> 時，呼叫端過去只能寫成
    /// <c>Task.FromResult(option).BindAsync(...)</c>——這會為了接上型別而多配置一個 <see cref="Task{TResult}"/>。
    /// </para>
    /// <para><b>零配置的短路路徑</b></para>
    /// <para>
    /// 本方法刻意<b>不</b>宣告為 <c>async</c>：None 時直接以同步完成的 <see cref="ValueTask{TResult}"/> 回傳，
    /// 完全不會建立 async 狀態機，也不會有任何 heap 配置。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Before：為了型別相容而多配置一個 Task
    /// var o1 = await Task.FromResult(cache.Lookup(key)).BindAsync(v =&gt; FetchAsync(v));
    ///
    /// // After：直接串接，None 短路時零配置
    /// var o2 = await cache.Lookup(key).BindAsync(v =&gt; FetchAsync(v));
    /// </code>
    /// </example>
    public static ValueTask<Option<TU>> BindAsync<T, TU>(
        this Option<T> option,
        Func<T, ValueTask<Option<TU>>> binder)
        where T : notnull
        where TU : notnull
        => option.TryGetValue(out var value)
            ? binder(value)
            : new ValueTask<Option<TU>>(Option<TU>.None);

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點，串接一個非同步的 binder，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="binder"/> 時才檢查取消狀態；若來源為 None，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Option<TU>> BindAsync<T, TU>(
        this Option<T> option,
        Func<T, CancellationToken, ValueTask<Option<TU>>> binder,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        if (!option.TryGetValue(out var value))
        {
            return new ValueTask<Option<TU>>(Option<TU>.None);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return binder(value, cancellationToken);
    }

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點，對其值套用一個非同步的 mapper。
    /// </summary>
    /// <typeparam name="T">原始值的類型。</typeparam>
    /// <typeparam name="TU">轉換後值的類型。</typeparam>
    /// <param name="option">來源 Option。</param>
    /// <param name="mapper">非同步映射函數。</param>
    /// <returns>若有值則為包含轉換後值的 Option；否則為 <see cref="Option{TU}.None"/>。</returns>
    /// <remarks>None 時走同步完成路徑，不建立 async 狀態機（詳見 <see cref="BindAsync{T,TU}(Option{T}, Func{T, ValueTask{Option{TU}}})"/> 的備註）。</remarks>
    public static ValueTask<Option<TU>> MapAsync<T, TU>(
        this Option<T> option,
        Func<T, ValueTask<TU>> mapper)
        where T : notnull
        where TU : notnull
        => option.TryGetValue(out var value)
            ? MapCore(mapper(value))
            : new ValueTask<Option<TU>>(Option<TU>.None);

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點，對其值套用一個非同步的 mapper，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="mapper"/> 時才檢查取消狀態；若來源為 None，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Option<TU>> MapAsync<T, TU>(
        this Option<T> option,
        Func<T, CancellationToken, ValueTask<TU>> mapper,
        CancellationToken cancellationToken = default)
        where T : notnull
        where TU : notnull
    {
        if (!option.TryGetValue(out var value))
        {
            return new ValueTask<Option<TU>>(Option<TU>.None);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return MapCore(mapper(value, cancellationToken));
    }

    /// <summary>
    /// 將 <c>ValueTask&lt;TU&gt;</c> 包裝回 <c>ValueTask&lt;Option&lt;TU&gt;&gt;</c>。
    /// </summary>
    /// <remarks>
    /// 抽出為獨立方法，是為了讓 <c>MapAsync</c> 的 None 短路路徑維持非 async——
    /// 只有真的需要 await mapper 結果時才進入 async 狀態機。
    /// </remarks>
    private static async ValueTask<Option<TU>> MapCore<TU>(ValueTask<TU> pending) where TU : notnull
        => Option<TU>.Some(await pending.ConfigureAwait(false));

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點，無值時執行非同步的備援工廠函數。
    /// </summary>
    /// <typeparam name="T">值的類型。</typeparam>
    /// <param name="option">來源 Option。</param>
    /// <param name="factory">非同步備援工廠函數。</param>
    /// <returns>若有值則回傳自身；若無值則回傳工廠函數的結果。</returns>
    /// <remarks>
    /// <para>
    /// 這是「快取命中則直接使用、未命中才非同步抓取」模式的自然寫法——
    /// 來源是同步取得的快取查詢結果，備援才是非同步的資料庫／API 呼叫。
    /// </para>
    /// <para>Some 時走同步完成路徑，不建立 async 狀態機，也不會執行 <paramref name="factory"/>。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var user = await cache.GetValueOrNone(id)          // 同步：Option&lt;User&gt;
    ///     .OrElseAsync(() =&gt; repository.FindAsync(id));  // 非同步備援
    /// </code>
    /// </example>
    public static ValueTask<Option<T>> OrElseAsync<T>(
        this Option<T> option,
        Func<ValueTask<Option<T>>> factory)
        where T : notnull
        => option.IsSome
            ? new ValueTask<Option<T>>(option)
            : factory();

    /// <summary>
    /// 以同步的 <see cref="Option{T}"/> 為起點的非同步備援，並傳入 <see cref="CancellationToken"/>。
    /// </summary>
    /// <remarks>僅在需要執行 <paramref name="factory"/> 時才檢查取消狀態；若來源為 Some，本方法為 no-op，不會拋出。</remarks>
    public static ValueTask<Option<T>> OrElseAsync<T>(
        this Option<T> option,
        Func<CancellationToken, ValueTask<Option<T>>> factory,
        CancellationToken cancellationToken = default)
        where T : notnull
    {
        if (option.IsSome)
        {
            return new ValueTask<Option<T>>(option);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return factory(cancellationToken);
    }
}
