extern alias original;

using BenchmarkDotNet.Attributes;
using jIAnSoft.Rayleigh;
using Old = original::jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Comparison;

// Store the results in escaping arrays. This avoids measuring only a sub-nanosecond
// accessor after construction and Unwrap have both disappeared through inlining.
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(string))]
[GenericTypeArguments(typeof(LargeValue))]
public class ConstructionBatchComparison<T> where T : notnull
{
    private const int BatchSize = 1024;
    private readonly T[] _values = new T[BatchSize];
    private readonly Old.Result<T, int>[] _original = new Old.Result<T, int>[BatchSize];
    private readonly Result<T, int>[] _current = new Result<T, int>[BatchSize];

    [GlobalSetup]
    public void Setup()
    {
        if (typeof(T) == typeof(string))
        {
            Array.Fill(_values, (T)(object)"value");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public bool Original()
    {
        for (var i = 0; i < _values.Length; i++)
        {
            _original[i] = Old.Result<T, int>.Ok(_values[i]);
        }

        return _original[0].IsOk;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public bool Current()
    {
        for (var i = 0; i < _values.Length; i++)
        {
            _current[i] = Result<T, int>.Ok(_values[i]);
        }

        return _current[0].IsOk;
    }
}
