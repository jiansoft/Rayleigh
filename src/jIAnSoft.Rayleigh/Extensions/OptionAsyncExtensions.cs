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
        return option.IsSome
            ? await binder(option.Unwrap()).ConfigureAwait(false)
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
        if (!option.IsSome)
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(option.Unwrap(), cancellationToken).ConfigureAwait(false);
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
        if (!option.IsSome)
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Option<TU>.Some(await mapper(option.Unwrap(), cancellationToken).ConfigureAwait(false));
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
        return option.IsSome
            ? await binder(option.Unwrap()).ConfigureAwait(false)
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
        if (!option.IsSome)
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await binder(option.Unwrap(), cancellationToken).ConfigureAwait(false);
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
        if (!option.IsSome)
        {
            return Option<TU>.None;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Option<TU>.Some(await mapper(option.Unwrap(), cancellationToken).ConfigureAwait(false));
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
}
