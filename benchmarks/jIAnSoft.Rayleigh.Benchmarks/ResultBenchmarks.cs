using BenchmarkDotNet.Attributes;
using jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 驗證 <see cref="Result{T,TE}"/> 的「zero-allocation」宣稱：容器本身（Ok/Err、Map/Bind/Match 鏈）不應有任何 Heap 配置，
/// 且 <c>ThrowIfUninitialized</c> 防護不應在成功路徑上造成可觀測的額外成本。
/// </summary>
[MemoryDiagnoser]
public class ResultBenchmarks
{
    private readonly Result<int, string> _ok = Result<int, string>.Ok(42);
    private readonly Result<int, string> _err = Result<int, string>.Err("boom");

    [Benchmark(Baseline = true)]
    public Result<int, string> CreateOk() => Result<int, string>.Ok(42);

    [Benchmark]
    public Result<int, string> CreateErr() => Result<int, string>.Err("boom");

    /// <summary>Map -> Bind -> Match 鏈，全程使用 static lambda，不捕獲任何外部變數。</summary>
    [Benchmark]
    public string MapBindMatchChain_StaticLambda()
    {
        return _ok
            .Map(static x => x * 2)
            .Bind(static x => x > 0 ? Result<int, string>.Ok(x) : Result<int, string>.Err("non-positive"))
            .Match(
                ok: static v => $"value:{v}",
                err: static e => $"error:{e}");
    }

    [Benchmark]
    public bool TryGetOk_Ok()
    {
        return _ok.TryGetOk(out _, out _);
    }

    [Benchmark]
    public int UnwrapOr_Err()
    {
        return _err.UnwrapOr(-1);
    }
}
