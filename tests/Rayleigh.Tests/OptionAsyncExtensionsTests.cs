using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Xunit;

namespace Rayleigh.Tests;

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
    [Fact]
    public async Task OrElseAsync_ValueTask_None_FactoryReturnsNone_ReturnsNone()
    {
        var task = ValueTask.FromResult(Option<int>.None);

        var result = await task.OrElseAsync(
            () => ValueTask.FromResult(Option<int>.None));

        Assert.True(result.IsNone);
    }
}
