using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// 驗證以同步的 <see cref="Result{T,TE}"/>／<see cref="Option{T}"/> 為起點的非同步擴充多載。
/// </summary>
/// <remarks>
/// 這些多載的存在理由是讓「同步取得結果 → 接非同步步驟」的管線不需要先包一層
/// <c>Task.FromResult(...)</c>；其設計重點是短路路徑（Err／None）刻意不進入 async 狀態機，
/// 因此測試除了行為正確性外，也驗證短路時回傳的 ValueTask 為「同步完成」。
/// </remarks>
public class SyncSourceAsyncExtensionsTests
{
    // ================================================================
    // Result.BindAsync
    // ================================================================

    /// <summary>
    /// 驗證 Ok 來源會執行 binder 並回傳其結果。
    /// </summary>
    [Fact]
    public async Task Result_BindAsync_OkSource_InvokesBinder()
    {
        var result = await Result<int, string>.Ok(21)
            .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x * 2)));

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Err 來源會短路，不執行 binder，並保留原錯誤。
    /// </summary>
    [Fact]
    public async Task Result_BindAsync_ErrSource_ShortCircuitsWithoutInvokingBinder()
    {
        var invoked = false;

        var result = await Result<int, string>.Err("boom")
            .BindAsync(x =>
            {
                invoked = true;
                return new ValueTask<Result<int, string>>(Result<int, string>.Ok(x));
            });

        Assert.False(invoked);
        Assert.True(result.IsErr);
        Assert.Equal("boom", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Err 短路時回傳的 ValueTask 為「同步完成」——代表未建立 async 狀態機，因此沒有 heap 配置。
    /// </summary>
    /// <remarks>
    /// <see cref="ValueTask{TResult}.IsCompletedSuccessfully"/> 在方法回傳的當下即為 true，
    /// 是「該路徑完全同步、未經過 await」的可觀測證據。若日後有人為了圖方便把這些多載改成
    /// <c>async</c>，本測試會立即失敗。
    /// </remarks>
    [Fact]
    public void Result_BindAsync_ErrSource_ReturnsSynchronouslyCompletedValueTask()
    {
        var pending = Result<int, string>.Err("boom")
            .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)));

        Assert.True(pending.IsCompletedSuccessfully);
    }

    /// <summary>
    /// 驗證帶 CancellationToken 的多載：Ok 來源會傳遞 token 給 binder。
    /// </summary>
    [Fact]
    public async Task Result_BindAsync_WithToken_OkSource_PassesTokenToBinder()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;

        var result = await Result<int, string>.Ok(1)
            .BindAsync((x, ct) =>
            {
                received = ct;
                return new ValueTask<Result<int, string>>(Result<int, string>.Ok(x));
            }, cts.Token);

        Assert.True(result.IsOk);
        Assert.Equal(cts.Token, received);
    }

    /// <summary>
    /// 驗證已取消的 token 在 Ok 來源時會拋出，但在 Err 短路時不會拋出（維持與其他多載一致的 no-op 語意）。
    /// </summary>
    [Fact]
    public async Task Result_BindAsync_WithCancelledToken_ThrowsOnlyWhenNotShortCircuited()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result<int, string>.Ok(1)
                .BindAsync((x, _) => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)), cts.Token));

        // Err 短路：不執行委派，因此不觀察取消訊號
        var result = await Result<int, string>.Err("boom")
            .BindAsync((x, _) => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)), cts.Token);

        Assert.Equal("boom", result.UnwrapErr());
    }

    // ================================================================
    // Result.MapAsync
    // ================================================================

    /// <summary>
    /// 驗證 Ok 來源會套用 mapper。
    /// </summary>
    [Fact]
    public async Task Result_MapAsync_OkSource_AppliesMapper()
    {
        var result = await Result<int, string>.Ok(21)
            .MapAsync<int, string, string>(x => new ValueTask<string>($"v{x}"));

        Assert.True(result.IsOk);
        Assert.Equal("v21", result.Unwrap());
    }

    /// <summary>
    /// 驗證 Err 來源短路且不執行 mapper。
    /// </summary>
    [Fact]
    public async Task Result_MapAsync_ErrSource_ShortCircuits()
    {
        var invoked = false;

        var result = await Result<int, string>.Err("boom")
            .MapAsync<int, string, string>(x =>
            {
                invoked = true;
                return new ValueTask<string>($"v{x}");
            });

        Assert.False(invoked);
        Assert.Equal("boom", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 MapAsync 的 Err 短路同樣是同步完成路徑。
    /// </summary>
    [Fact]
    public void Result_MapAsync_ErrSource_ReturnsSynchronouslyCompletedValueTask()
    {
        var pending = Result<int, string>.Err("boom")
            .MapAsync<int, string, string>(x => new ValueTask<string>($"v{x}"));

        Assert.True(pending.IsCompletedSuccessfully);
    }

    /// <summary>
    /// 驗證未初始化的 Result 在這些多載上同樣被中毒化（不會靜默視為 Err）。
    /// </summary>
    [Fact]
    public void Result_SyncSourceOverloads_OnUninitialized_Throw()
    {
        var uninitialized = default(Result<int, ResultTests.TestError>);

        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.BindAsync(x =>
                new ValueTask<Result<int, ResultTests.TestError>>(Result<int, ResultTests.TestError>.Ok(x))));

        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.MapAsync<int, string, ResultTests.TestError>(x => new ValueTask<string>($"v{x}")));
    }

    // ================================================================
    // Option.BindAsync / MapAsync
    // ================================================================

    /// <summary>
    /// 驗證 Some 來源會執行 binder。
    /// </summary>
    [Fact]
    public async Task Option_BindAsync_SomeSource_InvokesBinder()
    {
        var option = await Option<int>.Some(21)
            .BindAsync(x => new ValueTask<Option<int>>(Option<int>.Some(x * 2)));

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Unwrap());
    }

    /// <summary>
    /// 驗證 None 來源會短路且不執行 binder。
    /// </summary>
    [Fact]
    public async Task Option_BindAsync_NoneSource_ShortCircuits()
    {
        var invoked = false;

        var option = await Option<int>.None
            .BindAsync(x =>
            {
                invoked = true;
                return new ValueTask<Option<int>>(Option<int>.Some(x));
            });

        Assert.False(invoked);
        Assert.True(option.IsNone);
    }

    /// <summary>
    /// 驗證 None 短路時回傳同步完成的 ValueTask（未建立 async 狀態機）。
    /// </summary>
    [Fact]
    public void Option_BindAsync_NoneSource_ReturnsSynchronouslyCompletedValueTask()
    {
        var pending = Option<int>.None
            .BindAsync(x => new ValueTask<Option<int>>(Option<int>.Some(x)));

        Assert.True(pending.IsCompletedSuccessfully);
    }

    /// <summary>
    /// 驗證 Option.MapAsync 的兩種分支。
    /// </summary>
    [Fact]
    public async Task Option_MapAsync_AppliesMapperOnlyWhenSome()
    {
        var mapped = await Option<int>.Some(21).MapAsync(x => new ValueTask<string>($"v{x}"));
        Assert.Equal("v21", mapped.Unwrap());

        var skipped = await Option<int>.None.MapAsync(x => new ValueTask<string>($"v{x}"));
        Assert.True(skipped.IsNone);
    }

    /// <summary>
    /// 驗證帶 CancellationToken 的 Option 多載：Some 時傳遞 token，None 時短路不拋出。
    /// </summary>
    [Fact]
    public async Task Option_MapAsync_WithCancelledToken_ThrowsOnlyWhenNotShortCircuited()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Option<int>.Some(1).MapAsync((x, _) => new ValueTask<string>($"v{x}"), cts.Token));

        var option = await Option<int>.None.MapAsync((x, _) => new ValueTask<string>($"v{x}"), cts.Token);
        Assert.True(option.IsNone);
    }

    /// <summary>
    /// 驗證這些多載能與既有的 Task/ValueTask 多載組成完整管線，
    /// 亦即「同步起點 → 非同步步驟 → 非同步步驟」不需要任何 Task.FromResult 包裝。
    /// </summary>
    [Fact]
    public async Task SyncSource_ComposesWithExistingAsyncOverloads()
    {
        var result = await Result<int, string>.Ok(5)
            .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x + 1)))
            .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x * 10)));

        Assert.Equal(60, result.Unwrap());
    }

    // ================================================================
    // Result.MapErrAsync / OrElseAsync / TapAsync / TapErrAsync
    // ================================================================

    /// <summary>
    /// 驗證 MapErrAsync 只在 Err 時套用 mapper，Ok 時保留原值且不執行 mapper。
    /// </summary>
    [Fact]
    public async Task Result_MapErrAsync_AppliesMapperOnlyWhenErr()
    {
        var mapped = await Result<int, string>.Err("boom")
            .MapErrAsync(e => new ValueTask<int>(e.Length));
        Assert.Equal(4, mapped.UnwrapErr());

        var invoked = false;
        var untouched = await Result<int, string>.Ok(7)
            .MapErrAsync(e =>
            {
                invoked = true;
                return new ValueTask<int>(e.Length);
            });

        Assert.False(invoked);
        Assert.Equal(7, untouched.Unwrap());
    }

    /// <summary>
    /// 驗證 OrElseAsync 只在 Err 時執行備援，Ok 時直接回傳自身。
    /// </summary>
    [Fact]
    public async Task Result_OrElseAsync_InvokesFactoryOnlyWhenErr()
    {
        var recovered = await Result<int, string>.Err("miss")
            .OrElseAsync(_ => new ValueTask<Result<int, string>>(Result<int, string>.Ok(99)));
        Assert.Equal(99, recovered.Unwrap());

        var invoked = false;
        var kept = await Result<int, string>.Ok(1)
            .OrElseAsync(_ =>
            {
                invoked = true;
                return new ValueTask<Result<int, string>>(Result<int, string>.Ok(99));
            });

        Assert.False(invoked);
        Assert.Equal(1, kept.Unwrap());
    }

    /// <summary>
    /// 驗證 TapAsync 只在 Ok 時執行副作用，且回傳的結果未被改變。
    /// </summary>
    [Fact]
    public async Task Result_TapAsync_RunsSideEffectOnlyWhenOk()
    {
        var seen = new List<int>();

        var ok = await Result<int, string>.Ok(5).TapAsync(v =>
        {
            seen.Add(v);
            return ValueTask.CompletedTask;
        });

        var err = await Result<int, string>.Err("boom").TapAsync(v =>
        {
            seen.Add(v);
            return ValueTask.CompletedTask;
        });

        Assert.Equal([5], seen);
        Assert.Equal(5, ok.Unwrap());
        Assert.Equal("boom", err.UnwrapErr());
    }

    /// <summary>
    /// 驗證 TapErrAsync 只在 Err 時執行副作用，且回傳的結果未被改變。
    /// </summary>
    [Fact]
    public async Task Result_TapErrAsync_RunsSideEffectOnlyWhenErr()
    {
        var seen = new List<string>();

        var err = await Result<int, string>.Err("boom").TapErrAsync(e =>
        {
            seen.Add(e);
            return ValueTask.CompletedTask;
        });

        var ok = await Result<int, string>.Ok(5).TapErrAsync(e =>
        {
            seen.Add(e);
            return ValueTask.CompletedTask;
        });

        Assert.Equal(["boom"], seen);
        Assert.Equal("boom", err.UnwrapErr());
        Assert.Equal(5, ok.Unwrap());
    }

    /// <summary>
    /// 驗證所有 sync-source 多載在短路分支上都回傳同步完成的 ValueTask（零配置）。
    /// </summary>
    /// <remarks>
    /// 這是這批多載存在的核心理由。若日後有人為了圖方便把它們改成 <c>async</c>，本測試會立即失敗。
    /// </remarks>
    [Fact]
    public void Result_AllSyncSourceOverloads_ShortCircuitSynchronously()
    {
        var ok = Result<int, string>.Ok(1);
        var err = Result<int, string>.Err("boom");

        Assert.True(err.BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x))).IsCompletedSuccessfully);
        Assert.True(err.MapAsync<int, string, string>(x => new ValueTask<string>($"{x}")).IsCompletedSuccessfully);
        Assert.True(ok.MapErrAsync(e => new ValueTask<int>(e.Length)).IsCompletedSuccessfully);
        Assert.True(ok.OrElseAsync(_ => new ValueTask<Result<int, string>>(err)).IsCompletedSuccessfully);
        Assert.True(err.TapAsync(_ => ValueTask.CompletedTask).IsCompletedSuccessfully);
        Assert.True(ok.TapErrAsync(_ => ValueTask.CompletedTask).IsCompletedSuccessfully);
    }

    /// <summary>
    /// 驗證新增的 sync-source 多載在未初始化的 Result 上同樣被中毒化。
    /// </summary>
    [Fact]
    public void Result_NewSyncSourceOverloads_OnUninitialized_Throw()
    {
        var uninitialized = default(Result<int, ResultTests.TestError>);

        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.MapErrAsync(e => new ValueTask<string>($"{e}")));
        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.OrElseAsync(_ => new ValueTask<Result<int, ResultTests.TestError>>(uninitialized)));
        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.TapAsync(_ => ValueTask.CompletedTask));
        Assert.Throws<InvalidOperationException>(() =>
            uninitialized.TapErrAsync(_ => ValueTask.CompletedTask));
    }

    /// <summary>
    /// 驗證帶 CancellationToken 的新多載：需要執行委派時才觀察取消訊號。
    /// </summary>
    [Fact]
    public async Task Result_NewSyncSourceOverloads_WithCancelledToken_ThrowOnlyWhenNotShortCircuited()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Err 來源 → OrElseAsync 會執行委派 → 應拋出
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result<int, string>.Err("boom")
                .OrElseAsync((_, _) => new ValueTask<Result<int, string>>(Result<int, string>.Ok(1)), cts.Token));

        // Ok 來源 → OrElseAsync 短路 → 不應拋出
        var kept = await Result<int, string>.Ok(1)
            .OrElseAsync((_, _) => new ValueTask<Result<int, string>>(Result<int, string>.Ok(2)), cts.Token);
        Assert.Equal(1, kept.Unwrap());

        // Ok 來源 → TapAsync 會執行委派 → 應拋出
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result<int, string>.Ok(1).TapAsync((_, _) => ValueTask.CompletedTask, cts.Token));

        // Err 來源 → TapAsync 短路 → 不應拋出
        var untouched = await Result<int, string>.Err("boom")
            .TapAsync((_, _) => ValueTask.CompletedTask, cts.Token);
        Assert.Equal("boom", untouched.UnwrapErr());
    }

    // ================================================================
    // Option.OrElseAsync
    // ================================================================

    /// <summary>
    /// 驗證 Option 的 sync-source OrElseAsync：None 時執行備援，Some 時直接回傳自身。
    /// </summary>
    [Fact]
    public async Task Option_OrElseAsync_InvokesFactoryOnlyWhenNone()
    {
        var recovered = await Option<int>.None
            .OrElseAsync(() => new ValueTask<Option<int>>(Option<int>.Some(42)));
        Assert.Equal(42, recovered.Unwrap());

        var invoked = false;
        var kept = await Option<int>.Some(1)
            .OrElseAsync(() =>
            {
                invoked = true;
                return new ValueTask<Option<int>>(Option<int>.Some(42));
            });

        Assert.False(invoked);
        Assert.Equal(1, kept.Unwrap());
    }

    /// <summary>
    /// 驗證 Option.OrElseAsync 在 Some 短路時為同步完成。
    /// </summary>
    [Fact]
    public void Option_OrElseAsync_SomeSource_ReturnsSynchronouslyCompletedValueTask()
    {
        var pending = Option<int>.Some(1)
            .OrElseAsync(() => new ValueTask<Option<int>>(Option<int>.None));

        Assert.True(pending.IsCompletedSuccessfully);
    }

    /// <summary>
    /// 驗證 Option.OrElseAsync 的 CancellationToken 多載：Some 短路時不觀察取消訊號。
    /// </summary>
    [Fact]
    public async Task Option_OrElseAsync_WithCancelledToken_ThrowsOnlyWhenNotShortCircuited()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Option<int>.None.OrElseAsync(_ => new ValueTask<Option<int>>(Option<int>.Some(1)), cts.Token));

        var kept = await Option<int>.Some(9)
            .OrElseAsync(_ => new ValueTask<Option<int>>(Option<int>.Some(1)), cts.Token);
        Assert.Equal(9, kept.Unwrap());
    }

    /// <summary>
    /// 驗證「同步快取查詢 → 非同步備援」這個 OrElseAsync 的主要使用場景能完整組合。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_CacheMissThenFetch_ComposesEndToEnd()
    {
        var cache = new Dictionary<string, int> { ["hit"] = 100 };
        var fetchCount = 0;

        ValueTask<Option<int>> Fetch()
        {
            fetchCount++;
            return new ValueTask<Option<int>>(Option<int>.Some(200));
        }

        var fromCache = await cache.GetValueOrNone("hit").OrElseAsync(Fetch);
        var fromFetch = await cache.GetValueOrNone("miss").OrElseAsync(Fetch);

        Assert.Equal(100, fromCache.Unwrap());
        Assert.Equal(200, fromFetch.Unwrap());
        Assert.Equal(1, fetchCount); // 快取命中時完全沒有呼叫 Fetch
    }
}
