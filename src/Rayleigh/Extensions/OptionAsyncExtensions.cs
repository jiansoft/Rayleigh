namespace jIAnSoft.Rayleigh.Extensions;

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
