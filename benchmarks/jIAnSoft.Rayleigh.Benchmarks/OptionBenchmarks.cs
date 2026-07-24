using BenchmarkDotNet.Attributes;
using jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 驗證 <see cref="Option{T}"/> 的「zero-allocation」宣稱：容器本身（Some/None、Map/Bind/Match 鏈）不應有任何 Heap 配置。
/// 額外加入一組使用「捕獲外部變數的 lambda」的對照組，具體呈現 README 與 XML 文件中提到的界線：
/// 零配置僅保證於容器本身，呼叫端傳入會捕獲變數的 lambda 時，closure 配置仍由 C# 編譯器產生，不受此函式庫控制。
/// </summary>
[MemoryDiagnoser]
public class OptionBenchmarks
{
    private readonly Option<int> _some = Option<int>.Some(42);
    private readonly Option<int> _none = Option<int>.None;
    private int _externalState = 10;

    [Benchmark(Baseline = true)]
    public Option<int> CreateSome() => Option<int>.Some(42);

    [Benchmark]
    public Option<int> CreateNone() => Option<int>.None;

    /// <summary>Map -> Bind -> Match 鏈，全程使用 static lambda，不捕獲任何外部變數。</summary>
    [Benchmark]
    public string MapBindMatchChain_StaticLambda()
    {
        return _some
            .Map(static x => x * 2)
            .Bind(static x => x > 0 ? Option<int>.Some(x) : Option<int>.None)
            .Match(
                some: static v => $"value:{v}",
                none: static () => "none");
    }

    /// <summary>
    /// 與 <see cref="MapBindMatchChain_StaticLambda"/> 相同的邏輯，但 lambda 捕獲了 <see cref="_externalState"/>。
    /// 預期會顯示非零的 Allocated 欄位——這是 C# closure 的固有成本，並非 Option&lt;T&gt; 本身的配置。
    /// </summary>
    [Benchmark]
    public string MapBindMatchChain_CapturingClosure()
    {
        var offset = _externalState;
        return _some
            .Map(x => x * 2 + offset)
            .Bind(x => x > 0 ? Option<int>.Some(x) : Option<int>.None)
            .Match(
                some: v => $"value:{v}",
                none: () => "none");
    }

    [Benchmark]
    public bool TryGetValue_Some()
    {
        return _some.TryGetValue(out _);
    }

    [Benchmark]
    public int UnwrapOr_None()
    {
        return _none.UnwrapOr(-1);
    }
}
