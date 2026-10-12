using BenchmarkDotNet.Attributes;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 同一 runtime、同一輸入，比較審查起始版本與正式 Values 實作。
/// 原始版固定保留在 ValuesIteratorBaseline，避免把 runtime 差異當成程式碼收益。
/// </summary>
[MemoryDiagnoser]
[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(Guid))]
[GenericTypeArguments(typeof(string))]
public class ValuesIteratorBenchmarks<T> where T : notnull
{
    private IEnumerable<Option<T>> _source = [];

    [Params(0, 8, 1024)]
    public int Size { get; set; }

    [Params("Array", "List", "Enumerable")]
    public string SourceKind { get; set; } = "Array";

    [GlobalSetup]
    public void Setup()
    {
        var items = Enumerable.Range(0, Size)
            .Select(i => i % 2 == 0 ? Option<T>.Some(CreateValue(i)) : Option<T>.None)
            .ToArray();
        _source = SourceKind switch
        {
            "Array" => items,
            "List" => new List<Option<T>>(items),
            _ => AsEnumerable(items)
        };

        if (!ValuesIteratorBaseline.Values(_source).SequenceEqual(_source.Values()))
        {
            throw new InvalidOperationException("Baseline and current results differ.");
        }
    }

    [Benchmark(Baseline = true)]
    public (int Count, T? Last) Before()
    {
        var count = 0;
        T? last = default;
        foreach (var value in ValuesIteratorBaseline.Values(_source))
        {
            count++;
            last = value;
        }

        return (count, last);
    }

    [Benchmark]
    public (int Count, T? Last) After()
    {
        var count = 0;
        T? last = default;
        foreach (var value in _source.Values())
        {
            count++;
            last = value;
        }

        return (count, last);
    }

    private static T CreateValue(int index)
    {
        if (typeof(T) == typeof(int))
        {
            return (T)(object)index;
        }

        if (typeof(T) == typeof(Guid))
        {
            return (T)(object)new Guid(index, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        throw new NotSupportedException(typeof(T).FullName);
    }

    private static IEnumerable<Option<T>> AsEnumerable(Option<T>[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
