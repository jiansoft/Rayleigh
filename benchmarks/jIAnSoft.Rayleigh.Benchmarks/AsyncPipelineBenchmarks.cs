using BenchmarkDotNet.Attributes;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 驗證「以同步 <see cref="Result{T,TE}"/> 為起點」的非同步多載相對於 <c>Task.FromResult</c> 包裝的效益。
/// </summary>
/// <remarks>
/// <para>
/// 在這組多載出現之前，若管線的起點是同步取得的 <see cref="Result{T,TE}"/>，呼叫端只能寫成
/// <c>Task.FromResult(result).BindAsync(...)</c> 才能接上以 <see cref="Task{TResult}"/> 為 <c>this</c> 的多載。
/// 該寫法會配置一個 <see cref="Task{TResult}"/>，並在 <c>async</c> 方法中建立狀態機。
/// </para>
/// <para>
/// 新多載刻意<b>不</b>宣告為 <c>async</c>：短路（Err／None）時直接回傳同步完成的
/// <see cref="ValueTask{TResult}"/>，預期 Allocated 為 0 B。這組 benchmark 就是要把該宣稱量化。
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class AsyncPipelineBenchmarks
{
    private readonly Result<int, string> _ok = Result<int, string>.Ok(42);
    private readonly Result<int, string> _err = Result<int, string>.Err("boom");
    private readonly Option<int> _some = Option<int>.Some(42);
    private readonly Option<int> _none = Option<int>.None;

    /// <summary>基準線：舊寫法，為了型別相容而多包一層 Task。</summary>
    [Benchmark(Baseline = true, Description = "Err: Task.FromResult 包裝 (舊寫法)")]
    public async Task<Result<int, string>> Err_ViaTaskFromResult()
    {
        return await Task.FromResult(_err)
            .BindAsync(static x => Task.FromResult(Result<int, string>.Ok(x * 2)));
    }

    /// <summary>新多載：Err 短路，預期完全同步且零配置。</summary>
    [Benchmark(Description = "Err: sync-source 多載 (短路, 預期 0 B)")]
    public async ValueTask<Result<int, string>> Err_ViaSyncSource()
    {
        return await _err.BindAsync(
            static x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x * 2)));
    }

    /// <summary>不經過 await，直接觀察短路路徑是否為同步完成。</summary>
    [Benchmark(Description = "Err: sync-source 短路 (不 await)")]
    public bool Err_SyncSource_IsCompletedSynchronously()
    {
        var pending = _err.BindAsync(
            static x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x * 2)));
        return pending.IsCompletedSuccessfully;
    }

    /// <summary>成功路徑：binder 本身同步完成時，新多載同樣不應有額外配置。</summary>
    [Benchmark(Description = "Ok: sync-source 多載 (binder 同步完成)")]
    public async ValueTask<Result<int, string>> Ok_ViaSyncSource()
    {
        return await _ok.BindAsync(
            static x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x * 2)));
    }

    /// <summary>Option 版本的 None 短路。</summary>
    [Benchmark(Description = "None: Option sync-source 短路")]
    public async ValueTask<Option<int>> None_ViaSyncSource()
    {
        return await _none.BindAsync(static x => new ValueTask<Option<int>>(Option<int>.Some(x * 2)));
    }

    /// <summary>Option 版本的 Some 路徑。</summary>
    [Benchmark(Description = "Some: Option sync-source")]
    public async ValueTask<Option<int>> Some_ViaSyncSource()
    {
        return await _some.BindAsync(static x => new ValueTask<Option<int>>(Option<int>.Some(x * 2)));
    }
}

/// <summary>
/// 量測例外路徑相關的成本，用以佐證 throw helper 標記 <c>NoInlining</c> 的決策。
/// </summary>
/// <remarks>
/// <para>
/// <c>NoInlining</c> 的效益在於<b>縮小熱路徑的方法體積</b>，讓 JIT 更願意內聯外層方法——
/// 這是 code size 層面的影響，不會直接反映在單一方法的 ns/op 上。因此本類別量測的是
/// 「成功路徑的呼叫成本」：若 throw helper 的冷路徑 IL 被內聯進 <c>Unwrap()</c>，
/// 成功路徑會因方法體積膨脹而變慢或無法被內聯。
/// </para>
/// <para>
/// 另外量測例外路徑本身，作為「為什麼不該用例外做流程控制」的量化佐證——
/// 這正是 <see cref="Result{T,TE}"/> 存在的理由。
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class ThrowPathBenchmarks
{
    private readonly Result<int, string> _ok = Result<int, string>.Ok(42);
    private readonly Result<int, string> _err = Result<int, string>.Err("boom");
    private readonly Option<int> _some = Option<int>.Some(42);

    /// <summary>成功路徑：Unwrap 應被完整內聯，接近零成本。</summary>
    [Benchmark(Baseline = true, Description = "Result.Unwrap (成功路徑)")]
    public int Unwrap_Ok() => _ok.Unwrap();

    /// <summary>成功路徑：Option.Unwrap。</summary>
    [Benchmark(Description = "Option.Unwrap (成功路徑)")]
    public int Unwrap_Some() => _some.Unwrap();

    /// <summary>不拋出的取值方式，作為 Unwrap 的對照。</summary>
    [Benchmark(Description = "Result.TryGetOk (不拋出)")]
    public bool TryGetOk_Err() => _err.TryGetOk(out _, out _);

    /// <summary>錯誤路徑不拋例外，這是 Result 相對 Exception 的核心優勢。</summary>
    [Benchmark(Description = "Result 錯誤路徑 (不拋例外)")]
    public int ErrPath_NoException() => _err.UnwrapOr(-1);

    /// <summary>對照組：以例外處理同一個錯誤情境，量化「例外驅動流程控制」的代價。</summary>
    [Benchmark(Description = "對照組: 例外驅動流程控制")]
    public int ErrPath_WithException()
    {
        try
        {
            return _err.Unwrap();
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }
}
