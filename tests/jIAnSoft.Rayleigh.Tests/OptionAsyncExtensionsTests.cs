using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="OptionAsyncExtensions"/> 靜態類別中的非同步擴充方法，
/// 包含 Task 版本與 ValueTask 版本的 BindAsync、MapAsync、OrElseAsync。
/// </summary>
public class OptionAsyncExtensionsTests
{
    // ==========================================
    // Task - BindAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 Some 狀態下呼叫 binder 並回傳其結果。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_Some_CallsBinderAndReturnsResult()
    {
        var task = Task.FromResult(Option<int>.Some(5));

        var result = await task.BindAsync(
            v => Task.FromResult(Option<string>.Some($"value:{v}")));

        Assert.True(result.IsSome);
        Assert.Equal("value:5", result.Unwrap());
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 None 狀態下不呼叫 binder，直接回傳 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_None_DoesNotCallBinderAndReturnsNone()
    {
        var task = Task.FromResult(Option<int>.None);
        var binderCalled = false;

        var result = await task.BindAsync(v =>
        {
            binderCalled = true;
            return Task.FromResult(Option<string>.Some($"value:{v}"));
        });

        Assert.True(result.IsNone);
        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 Some 狀態下，當 binder 回傳 None 時結果為 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_Some_BinderReturnsNone_ReturnsNone()
    {
        var task = Task.FromResult(Option<int>.Some(5));

        var result = await task.BindAsync(
            _ => Task.FromResult(Option<string>.None));

        Assert.True(result.IsNone);
    }

    // ==========================================
    // Task - MapAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 在 Some 狀態下呼叫 mapper 並將結果包裝為 Some。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_Task_Some_CallsMapperAndReturnsSome()
    {
        var task = Task.FromResult(Option<int>.Some(10));

        var result = await task.MapAsync(v => Task.FromResult(v * 3));

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 在 None 狀態下不呼叫 mapper，直接回傳 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_Task_None_DoesNotCallMapperAndReturnsNone()
    {
        var task = Task.FromResult(Option<int>.None);
        var mapperCalled = false;

        var result = await task.MapAsync(v =>
        {
            mapperCalled = true;
            return Task.FromResult(v * 3);
        });

        Assert.True(result.IsNone);
        Assert.False(mapperCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 可以轉換值類型，例如 int 轉換為 string。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_Task_Some_TransformsType()
    {
        var task = Task.FromResult(Option<int>.Some(42));

        var result = await task.MapAsync(v => Task.FromResult($"number:{v}"));

        Assert.True(result.IsSome);
        Assert.Equal("number:42", result.Unwrap());
    }

    // ==========================================
    // Task - OrElseAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 Some 狀態下回傳自身，不呼叫 factory。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_Task_Some_ReturnsSelfAndDoesNotCallFactory()
    {
        var task = Task.FromResult(Option<int>.Some(7));
        var factoryCalled = false;

        var result = await task.OrElseAsync(() =>
        {
            factoryCalled = true;
            return Task.FromResult(Option<int>.Some(99));
        });

        Assert.True(result.IsSome);
        Assert.Equal(7, result.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 None 狀態下呼叫 factory 並回傳其結果。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_Task_None_CallsFactoryAndReturnsResult()
    {
        var task = Task.FromResult(Option<int>.None);

        var result = await task.OrElseAsync(
            () => Task.FromResult(Option<int>.Some(99)));

        Assert.True(result.IsSome);
        Assert.Equal(99, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 None 狀態下，當 factory 也回傳 None 時結果為 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_Task_None_FactoryReturnsNone_ReturnsNone()
    {
        var task = Task.FromResult(Option<int>.None);

        var result = await task.OrElseAsync(
            () => Task.FromResult(Option<int>.None));

        Assert.True(result.IsNone);
    }

    // ==========================================
    // ValueTask - BindAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 Some 狀態下呼叫 binder 並回傳其結果。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_ValueTask_Some_CallsBinderAndReturnsResult()
    {
        var task = ValueTask.FromResult(Option<int>.Some(5));

        var result = await task.BindAsync(
            v => ValueTask.FromResult(Option<string>.Some($"value:{v}")));

        Assert.True(result.IsSome);
        Assert.Equal("value:5", result.Unwrap());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 None 狀態下不呼叫 binder，直接回傳 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_ValueTask_None_DoesNotCallBinderAndReturnsNone()
    {
        var task = ValueTask.FromResult(Option<int>.None);
        var binderCalled = false;

        var result = await task.BindAsync(v =>
        {
            binderCalled = true;
            return ValueTask.FromResult(Option<string>.Some($"value:{v}"));
        });

        Assert.True(result.IsNone);
        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 Some 狀態下，當 binder 回傳 None 時結果為 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_ValueTask_Some_BinderReturnsNone_ReturnsNone()
    {
        var task = ValueTask.FromResult(Option<int>.Some(5));

        var result = await task.BindAsync(
            _ => ValueTask.FromResult(Option<string>.None));

        Assert.True(result.IsNone);
    }

    // ==========================================
    // ValueTask - MapAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 在 Some 狀態下呼叫 mapper 並將結果包裝為 Some。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_ValueTask_Some_CallsMapperAndReturnsSome()
    {
        var task = ValueTask.FromResult(Option<int>.Some(10));

        var result = await task.MapAsync(v => ValueTask.FromResult(v * 3));

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 在 None 狀態下不呼叫 mapper，直接回傳 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_ValueTask_None_DoesNotCallMapperAndReturnsNone()
    {
        var task = ValueTask.FromResult(Option<int>.None);
        var mapperCalled = false;

        var result = await task.MapAsync(v =>
        {
            mapperCalled = true;
            return ValueTask.FromResult(v * 3);
        });

        Assert.True(result.IsNone);
        Assert.False(mapperCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 可以轉換值類型，例如 int 轉換為 string。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_ValueTask_Some_TransformsType()
    {
        var task = ValueTask.FromResult(Option<int>.Some(42));

        var result = await task.MapAsync(v => ValueTask.FromResult($"number:{v}"));

        Assert.True(result.IsSome);
        Assert.Equal("number:42", result.Unwrap());
    }

    // ==========================================
    // ValueTask - OrElseAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 Some 狀態下回傳自身，不呼叫 factory。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_ValueTask_Some_ReturnsSelfAndDoesNotCallFactory()
    {
        var task = ValueTask.FromResult(Option<int>.Some(7));
        var factoryCalled = false;

        var result = await task.OrElseAsync(() =>
        {
            factoryCalled = true;
            return ValueTask.FromResult(Option<int>.Some(99));
        });

        Assert.True(result.IsSome);
        Assert.Equal(7, result.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 None 狀態下呼叫 factory 並回傳其結果。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_ValueTask_None_CallsFactoryAndReturnsResult()
    {
        var task = ValueTask.FromResult(Option<int>.None);

        var result = await task.OrElseAsync(
            () => ValueTask.FromResult(Option<int>.Some(99)));

        Assert.True(result.IsSome);
        Assert.Equal(99, result.Unwrap());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 None 狀態下，當 factory 也回傳 None 時結果為 None。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_ValueTask_None_FactoryReturnsNone_ReturnsNone()
    {
        var task = ValueTask.FromResult(Option<int>.None);

        var result = await task.OrElseAsync(
            () => ValueTask.FromResult(Option<int>.None));

        Assert.True(result.IsNone);
    }

    // ==========================================
    // CancellationToken overloads
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 BindAsync（CancellationToken 多載）會將 token 轉發給 binder。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_WithCancellationToken_PassesTokenToBinder()
    {
        using var cts = new CancellationTokenSource();
        var task = Task.FromResult(Option<int>.Some(5));
        var receivedToken = CancellationToken.None;

        var result = await task.BindAsync((v, ct) =>
        {
            receivedToken = ct;
            return Task.FromResult(Option<string>.Some($"value:{v}"));
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal("value:5", result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync（CancellationToken 多載）在 None 狀態下即使 token 已取消也不會呼叫 binder 或拋出例外（no-op 短路）。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_WithCancellationToken_None_DoesNotThrowEvenIfAlreadyCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromResult(Option<int>.None);
        var binderCalled = false;

        var result = await task.BindAsync((v, _) =>
        {
            binderCalled = true;
            return Task.FromResult(Option<string>.Some($"value:{v}"));
        }, cts.Token);

        Assert.True(result.IsNone);
        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync（CancellationToken 多載）在 Some 狀態下，若 token 已取消則在呼叫 binder 前拋出 OperationCanceledException。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_Task_WithCancellationToken_Some_AlreadyCancelled_ThrowsBeforeCallingBinder()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromResult(Option<int>.Some(5));
        var binderCalled = false;

        await Assert.ThrowsAsync<OperationCanceledException>(() => task.BindAsync((v, _) =>
        {
            binderCalled = true;
            return Task.FromResult(Option<string>.Some($"value:{v}"));
        }, cts.Token));

        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 MapAsync（CancellationToken 多載）會將 token 轉發給 mapper。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_Task_WithCancellationToken_PassesTokenToMapper()
    {
        using var cts = new CancellationTokenSource();
        var task = Task.FromResult(Option<int>.Some(10));
        var receivedToken = CancellationToken.None;

        var result = await task.MapAsync((v, ct) =>
        {
            receivedToken = ct;
            return Task.FromResult(v * 3);
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync（CancellationToken 多載）會將 token 轉發給 factory。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_Task_WithCancellationToken_PassesTokenToFactory()
    {
        using var cts = new CancellationTokenSource();
        var task = Task.FromResult(Option<int>.None);
        var receivedToken = CancellationToken.None;

        var result = await task.OrElseAsync(ct =>
        {
            receivedToken = ct;
            return Task.FromResult(Option<int>.Some(99));
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal(99, result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync（CancellationToken 多載）會將 token 轉發給 binder。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task BindAsync_ValueTask_WithCancellationToken_PassesTokenToBinder()
    {
        using var cts = new CancellationTokenSource();
        var task = ValueTask.FromResult(Option<int>.Some(5));
        var receivedToken = CancellationToken.None;

        var result = await task.BindAsync((v, ct) =>
        {
            receivedToken = ct;
            return ValueTask.FromResult(Option<string>.Some($"value:{v}"));
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal("value:5", result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync（CancellationToken 多載）會將 token 轉發給 mapper。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task MapAsync_ValueTask_WithCancellationToken_PassesTokenToMapper()
    {
        using var cts = new CancellationTokenSource();
        var task = ValueTask.FromResult(Option<int>.Some(10));
        var receivedToken = CancellationToken.None;

        var result = await task.MapAsync((v, ct) =>
        {
            receivedToken = ct;
            return ValueTask.FromResult(v * 3);
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync（CancellationToken 多載）會將 token 轉發給 factory。
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution and completes when all awaited assertions and callbacks have finished.</returns>
    [Fact]
    public async Task OrElseAsync_ValueTask_WithCancellationToken_PassesTokenToFactory()
    {
        using var cts = new CancellationTokenSource();
        var task = ValueTask.FromResult(Option<int>.None);
        var receivedToken = CancellationToken.None;

        var result = await task.OrElseAsync(ct =>
        {
            receivedToken = ct;
            return ValueTask.FromResult(Option<int>.Some(99));
        }, cts.Token);

        Assert.True(result.IsSome);
        Assert.Equal(99, result.Unwrap());
        Assert.Equal(cts.Token, receivedToken);
    }
}
