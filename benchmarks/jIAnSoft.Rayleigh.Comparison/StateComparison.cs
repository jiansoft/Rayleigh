extern alias original;

using BenchmarkDotNet.Attributes;
using jIAnSoft.Rayleigh;
using Old = original::jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Comparison;

public class StateComparison
{
    private readonly Option<int> _option = Option<int>.Some(42);
    private readonly Old.Option<int> _oldOption = Old.Option<int>.Some(42);
    private readonly Result<int, string> _result = Result<int, string>.Ok(42);
    private readonly Old.Result<int, string> _oldResult = Old.Result<int, string>.Ok(42);
    private int _offset = 10;

    [GlobalSetup]
    public void Setup()
    {
        if (OriginalOptionMap() != 52 || CurrentOptionMap() != 52
            || OriginalOptionBind() != 52 || CurrentOptionBind() != 52
            || OriginalResultMap() != 52 || CurrentResultMap() != 52)
        {
            throw new InvalidOperationException("Stateful and capturing operations differ.");
        }
    }

    [Benchmark(Baseline = true), BenchmarkCategory("OptionMap")]
    public int OriginalOptionMap()
    {
        var offset = _offset;
        return _oldOption.Map(x => x + offset).Unwrap();
    }

    [Benchmark, BenchmarkCategory("OptionMap")]
    public int CurrentOptionMap() => _option.Map(_offset, static (x, offset) => x + offset).Unwrap();

    [Benchmark(Baseline = true), BenchmarkCategory("OptionBind")]
    public int OriginalOptionBind()
    {
        var offset = _offset;
        return _oldOption.Bind(x => Old.Option<int>.Some(x + offset)).Unwrap();
    }

    [Benchmark, BenchmarkCategory("OptionBind")]
    public int CurrentOptionBind() => _option.Bind(_offset, static (x, offset) => Option<int>.Some(x + offset)).Unwrap();

    [Benchmark(Baseline = true), BenchmarkCategory("ResultMap")]
    public int OriginalResultMap()
    {
        var offset = _offset;
        return _oldResult.Map(x => x + offset).Unwrap();
    }

    [Benchmark, BenchmarkCategory("ResultMap")]
    public int CurrentResultMap() => _result.Map(_offset, static (x, offset) => x + offset).Unwrap();

}
