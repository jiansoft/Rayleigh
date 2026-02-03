using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="ResultAsyncExtensions"/> 靜態類別中的非同步擴充方法，
/// 包含 Task 版本與 ValueTask 版本的 BindAsync、MapAsync、MapErrAsync、
/// OrElseAsync、TapAsync、TapErrAsync。
/// </summary>
public class ResultAsyncExtensionsTests
{
    // ==========================================
    // Task - BindAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 Ok 狀態下呼叫 binder 並回傳其結果。
    /// </summary>
    [Fact]
    public async Task BindAsync_Task_Ok_CallsBinderAndReturnsResult()
    {
        var task = Task.FromResult(Result<int, string>.Ok(5));

        var result = await task.BindAsync(
            v => Task.FromResult(Result<string, string>.Ok($"value:{v}")));

        Assert.True(result.IsOk);
        Assert.Equal("value:5", result.Unwrap());
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 Err 狀態下不呼叫 binder，直接傳遞錯誤。
    /// </summary>
    [Fact]
    public async Task BindAsync_Task_Err_DoesNotCallBinderAndPassesThroughError()
    {
        var task = Task.FromResult(Result<int, string>.Err("error"));
        var binderCalled = false;

        var result = await task.BindAsync(v =>
        {
            binderCalled = true;
            return Task.FromResult(Result<string, string>.Ok($"value:{v}"));
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 BindAsync 在 Ok 狀態下，當 binder 回傳 Err 時結果為 Err。
    /// </summary>
    [Fact]
    public async Task BindAsync_Task_Ok_BinderReturnsErr_ReturnsErr()
    {
        var task = Task.FromResult(Result<int, string>.Ok(5));

        var result = await task.BindAsync(
            _ => Task.FromResult(Result<string, string>.Err("binder failed")));

        Assert.True(result.IsErr);
        Assert.Equal("binder failed", result.UnwrapErr());
    }

    // ==========================================
    // Task - MapAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 在 Ok 狀態下呼叫 mapper 並將結果包裝為 Ok。
    /// </summary>
    [Fact]
    public async Task MapAsync_Task_Ok_CallsMapperAndReturnsOk()
    {
        var task = Task.FromResult(Result<int, string>.Ok(10));

        var result = await task.MapAsync(v => Task.FromResult(v * 3));

        Assert.True(result.IsOk);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 在 Err 狀態下不呼叫 mapper，直接傳遞錯誤。
    /// </summary>
    [Fact]
    public async Task MapAsync_Task_Err_DoesNotCallMapperAndPassesThroughError()
    {
        var task = Task.FromResult(Result<int, string>.Err("error"));
        var mapperCalled = false;

        var result = await task.MapAsync(v =>
        {
            mapperCalled = true;
            return Task.FromResult(v * 3);
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(mapperCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 MapAsync 可以將 int 轉換為 string 類型。
    /// </summary>
    [Fact]
    public async Task MapAsync_Task_Ok_TransformsType()
    {
        var task = Task.FromResult(Result<int, string>.Ok(42));

        var result = await task.MapAsync(v => Task.FromResult($"number:{v}"));

        Assert.True(result.IsOk);
        Assert.Equal("number:42", result.Unwrap());
    }

    // ==========================================
    // Task - MapErrAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 MapErrAsync 在 Err 狀態下呼叫 mapper 並轉換錯誤類型。
    /// </summary>
    [Fact]
    public async Task MapErrAsync_Task_Err_CallsMapperAndTransformsError()
    {
        var task = Task.FromResult(Result<int, string>.Err("not found"));

        var result = await task.MapErrAsync(
            e => Task.FromResult(e.Length));

        Assert.True(result.IsErr);
        Assert.Equal(9, result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Task 版本的 MapErrAsync 在 Ok 狀態下不呼叫 mapper，直接傳遞成功值。
    /// </summary>
    [Fact]
    public async Task MapErrAsync_Task_Ok_DoesNotCallMapperAndPassesThroughValue()
    {
        var task = Task.FromResult(Result<int, string>.Ok(42));
        var mapperCalled = false;

        var result = await task.MapErrAsync(e =>
        {
            mapperCalled = true;
            return Task.FromResult(e.Length);
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.False(mapperCalled);
    }

    // ==========================================
    // Task - OrElseAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 Ok 狀態下回傳自身，不呼叫 factory。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_Task_Ok_ReturnsSelfAndDoesNotCallFactory()
    {
        var task = Task.FromResult(Result<int, string>.Ok(7));
        var factoryCalled = false;

        var result = await task.OrElseAsync(_ =>
        {
            factoryCalled = true;
            return Task.FromResult(Result<int, string>.Ok(99));
        });

        Assert.True(result.IsOk);
        Assert.Equal(7, result.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 Err 狀態下呼叫 factory，
    /// 並將錯誤值傳遞給 factory 作為參數。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_Task_Err_CallsFactoryWithErrorAndReturnsResult()
    {
        var task = Task.FromResult(Result<int, string>.Err("original error"));
        string? receivedError = null;

        var result = await task.OrElseAsync(err =>
        {
            receivedError = err;
            return Task.FromResult(Result<int, string>.Ok(99));
        });

        Assert.True(result.IsOk);
        Assert.Equal(99, result.Unwrap());
        Assert.Equal("original error", receivedError);
    }

    /// <summary>
    /// 驗證 Task 版本的 OrElseAsync 在 Err 狀態下，當 factory 也回傳 Err 時結果為 Err。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_Task_Err_FactoryReturnsErr_ReturnsErr()
    {
        var task = Task.FromResult(Result<int, string>.Err("first"));

        var result = await task.OrElseAsync(
            err => Task.FromResult(Result<int, string>.Err($"fallback:{err}")));

        Assert.True(result.IsErr);
        Assert.Equal("fallback:first", result.UnwrapErr());
    }

    // ==========================================
    // Task - TapAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 TapAsync 在 Ok 狀態下執行 action 且回傳原始 Result 不變。
    /// </summary>
    [Fact]
    public async Task TapAsync_Task_Ok_ExecutesActionAndReturnsSameResult()
    {
        var task = Task.FromResult(Result<int, string>.Ok(42));
        var actionExecuted = false;
        var receivedValue = 0;

        var result = await task.TapAsync(v =>
        {
            actionExecuted = true;
            receivedValue = v;
            return Task.CompletedTask;
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.True(actionExecuted);
        Assert.Equal(42, receivedValue);
    }

    /// <summary>
    /// 驗證 Task 版本的 TapAsync 在 Err 狀態下不執行 action，回傳原始錯誤。
    /// </summary>
    [Fact]
    public async Task TapAsync_Task_Err_DoesNotExecuteAction()
    {
        var task = Task.FromResult(Result<int, string>.Err("error"));
        var actionExecuted = false;

        var result = await task.TapAsync(_ =>
        {
            actionExecuted = true;
            return Task.CompletedTask;
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(actionExecuted);
    }

    // ==========================================
    // Task - TapErrAsync
    // ==========================================

    /// <summary>
    /// 驗證 Task 版本的 TapErrAsync 在 Err 狀態下執行 action 且回傳原始 Result 不變。
    /// </summary>
    [Fact]
    public async Task TapErrAsync_Task_Err_ExecutesActionAndReturnsSameResult()
    {
        var task = Task.FromResult(Result<int, string>.Err("error"));
        var actionExecuted = false;
        var receivedError = "";

        var result = await task.TapErrAsync(e =>
        {
            actionExecuted = true;
            receivedError = e;
            return Task.CompletedTask;
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.True(actionExecuted);
        Assert.Equal("error", receivedError);
    }

    /// <summary>
    /// 驗證 Task 版本的 TapErrAsync 在 Ok 狀態下不執行 action，回傳原始成功值。
    /// </summary>
    [Fact]
    public async Task TapErrAsync_Task_Ok_DoesNotExecuteAction()
    {
        var task = Task.FromResult(Result<int, string>.Ok(42));
        var actionExecuted = false;

        var result = await task.TapErrAsync(_ =>
        {
            actionExecuted = true;
            return Task.CompletedTask;
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.False(actionExecuted);
    }

    // ==========================================
    // ValueTask - BindAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 Ok 狀態下呼叫 binder 並回傳其結果。
    /// </summary>
    [Fact]
    public async Task BindAsync_ValueTask_Ok_CallsBinderAndReturnsResult()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(5));

        var result = await task.BindAsync(
            v => ValueTask.FromResult(Result<string, string>.Ok($"value:{v}")));

        Assert.True(result.IsOk);
        Assert.Equal("value:5", result.Unwrap());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 Err 狀態下不呼叫 binder，直接傳遞錯誤。
    /// </summary>
    [Fact]
    public async Task BindAsync_ValueTask_Err_DoesNotCallBinderAndPassesThroughError()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("error"));
        var binderCalled = false;

        var result = await task.BindAsync(v =>
        {
            binderCalled = true;
            return ValueTask.FromResult(Result<string, string>.Ok($"value:{v}"));
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(binderCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 BindAsync 在 Ok 狀態下，當 binder 回傳 Err 時結果為 Err。
    /// </summary>
    [Fact]
    public async Task BindAsync_ValueTask_Ok_BinderReturnsErr_ReturnsErr()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(5));

        var result = await task.BindAsync(
            _ => ValueTask.FromResult(Result<string, string>.Err("binder failed")));

        Assert.True(result.IsErr);
        Assert.Equal("binder failed", result.UnwrapErr());
    }

    // ==========================================
    // ValueTask - MapAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 在 Ok 狀態下呼叫 mapper 並將結果包裝為 Ok。
    /// </summary>
    [Fact]
    public async Task MapAsync_ValueTask_Ok_CallsMapperAndReturnsOk()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(10));

        var result = await task.MapAsync(v => ValueTask.FromResult(v * 3));

        Assert.True(result.IsOk);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 在 Err 狀態下不呼叫 mapper，直接傳遞錯誤。
    /// </summary>
    [Fact]
    public async Task MapAsync_ValueTask_Err_DoesNotCallMapperAndPassesThroughError()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("error"));
        var mapperCalled = false;

        var result = await task.MapAsync(v =>
        {
            mapperCalled = true;
            return ValueTask.FromResult(v * 3);
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(mapperCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapAsync 可以將 int 轉換為 string 類型。
    /// </summary>
    [Fact]
    public async Task MapAsync_ValueTask_Ok_TransformsType()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(42));

        var result = await task.MapAsync(v => ValueTask.FromResult($"number:{v}"));

        Assert.True(result.IsOk);
        Assert.Equal("number:42", result.Unwrap());
    }

    // ==========================================
    // ValueTask - MapErrAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 MapErrAsync 在 Err 狀態下呼叫 mapper 並轉換錯誤類型。
    /// </summary>
    [Fact]
    public async Task MapErrAsync_ValueTask_Err_CallsMapperAndTransformsError()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("not found"));

        var result = await task.MapErrAsync(
            e => ValueTask.FromResult(e.Length));

        Assert.True(result.IsErr);
        Assert.Equal(9, result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 MapErrAsync 在 Ok 狀態下不呼叫 mapper，直接傳遞成功值。
    /// </summary>
    [Fact]
    public async Task MapErrAsync_ValueTask_Ok_DoesNotCallMapperAndPassesThroughValue()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(42));
        var mapperCalled = false;

        var result = await task.MapErrAsync(e =>
        {
            mapperCalled = true;
            return ValueTask.FromResult(e.Length);
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.False(mapperCalled);
    }

    // ==========================================
    // ValueTask - OrElseAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 Ok 狀態下回傳自身，不呼叫 factory。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_ValueTask_Ok_ReturnsSelfAndDoesNotCallFactory()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(7));
        var factoryCalled = false;

        var result = await task.OrElseAsync(err =>
        {
            factoryCalled = true;
            return ValueTask.FromResult(Result<int, string>.Ok(99));
        });

        Assert.True(result.IsOk);
        Assert.Equal(7, result.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 Err 狀態下呼叫 factory，
    /// 並將錯誤值傳遞給 factory 作為參數。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_ValueTask_Err_CallsFactoryWithErrorAndReturnsResult()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("original error"));
        string? receivedError = null;

        var result = await task.OrElseAsync(err =>
        {
            receivedError = err;
            return ValueTask.FromResult(Result<int, string>.Ok(99));
        });

        Assert.True(result.IsOk);
        Assert.Equal(99, result.Unwrap());
        Assert.Equal("original error", receivedError);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 OrElseAsync 在 Err 狀態下，當 factory 也回傳 Err 時結果為 Err。
    /// </summary>
    [Fact]
    public async Task OrElseAsync_ValueTask_Err_FactoryReturnsErr_ReturnsErr()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("first"));

        var result = await task.OrElseAsync(
            err => ValueTask.FromResult(Result<int, string>.Err($"fallback:{err}")));

        Assert.True(result.IsErr);
        Assert.Equal("fallback:first", result.UnwrapErr());
    }

    // ==========================================
    // ValueTask - TapAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 TapAsync 在 Ok 狀態下執行 action 且回傳原始 Result 不變。
    /// </summary>
    [Fact]
    public async Task TapAsync_ValueTask_Ok_ExecutesActionAndReturnsSameResult()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(42));
        var actionExecuted = false;
        var receivedValue = 0;

        var result = await task.TapAsync(v =>
        {
            actionExecuted = true;
            receivedValue = v;
            return ValueTask.CompletedTask;
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.True(actionExecuted);
        Assert.Equal(42, receivedValue);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 TapAsync 在 Err 狀態下不執行 action，回傳原始錯誤。
    /// </summary>
    [Fact]
    public async Task TapAsync_ValueTask_Err_DoesNotExecuteAction()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("error"));
        var actionExecuted = false;

        var result = await task.TapAsync(_ =>
        {
            actionExecuted = true;
            return ValueTask.CompletedTask;
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.False(actionExecuted);
    }

    // ==========================================
    // ValueTask - TapErrAsync
    // ==========================================

    /// <summary>
    /// 驗證 ValueTask 版本的 TapErrAsync 在 Err 狀態下執行 action 且回傳原始 Result 不變。
    /// </summary>
    [Fact]
    public async Task TapErrAsync_ValueTask_Err_ExecutesActionAndReturnsSameResult()
    {
        var task = ValueTask.FromResult(Result<int, string>.Err("error"));
        var actionExecuted = false;
        var receivedError = "";

        var result = await task.TapErrAsync(e =>
        {
            actionExecuted = true;
            receivedError = e;
            return ValueTask.CompletedTask;
        });

        Assert.True(result.IsErr);
        Assert.Equal("error", result.UnwrapErr());
        Assert.True(actionExecuted);
        Assert.Equal("error", receivedError);
    }

    /// <summary>
    /// 驗證 ValueTask 版本的 TapErrAsync 在 Ok 狀態下不執行 action，回傳原始成功值。
    /// </summary>
    [Fact]
    public async Task TapErrAsync_ValueTask_Ok_DoesNotExecuteAction()
    {
        var task = ValueTask.FromResult(Result<int, string>.Ok(42));
        var actionExecuted = false;

        var result = await task.TapErrAsync(_ =>
        {
            actionExecuted = true;
            return ValueTask.CompletedTask;
        });

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
        Assert.False(actionExecuted);
    }
}
