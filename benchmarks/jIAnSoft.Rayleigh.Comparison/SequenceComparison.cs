extern alias original;

using BenchmarkDotNet.Attributes;
using jIAnSoft.Rayleigh;
using Old = original::jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Comparison;

[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(Guid))]
[GenericTypeArguments(typeof(string))]
public class SequenceComparison<T> where T : notnull
{
    [Params("ArrayEmpty", "ArraySmall", "ArraySuccess", "ArrayFirst", "ArrayLast", "ListSuccess", "ListFirst")]
    public string Scenario { get; set; } = "ArrayEmpty";

    private IEnumerable<Result<T, string>> _current = null!;
    private IEnumerable<Old.Result<T, string>> _original = null!;
    private IEnumerable<Option<T>> _options = null!;
    private IEnumerable<Old.Option<T>> _oldOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        var size = Scenario.EndsWith("Empty", StringComparison.Ordinal) ? 0
            : Scenario.EndsWith("Small", StringComparison.Ordinal) ? 8 : 65_536;
        var errorIndex = Scenario.EndsWith("First", StringComparison.Ordinal) ? 0
            : Scenario.EndsWith("Last", StringComparison.Ordinal) ? size - 1 : -1;
        var values = Enumerable.Range(0, size).Select(CreateValue).ToArray();
        _current = Shape(values.Select((v, i) => i == errorIndex ? Result<T, string>.Err("error") : Result<T, string>.Ok(v)).ToArray());
        _original = Shape(values.Select((v, i) => i == errorIndex ? Old.Result<T, string>.Err("error") : Old.Result<T, string>.Ok(v)).ToArray());
        _options = Shape(values.Select((v, i) => i == errorIndex ? Option<T>.None : Option<T>.Some(v)).ToArray());
        _oldOptions = Shape(values.Select((v, i) => i == errorIndex ? Old.Option<T>.None : Old.Option<T>.Some(v)).ToArray());

        // Verify expected output as well as equivalence, outside measurement.
        foreach (var actual in new[] { OriginalResult(), CurrentResult(), OriginalOption(), CurrentOption() })
        {
            if (errorIndex >= 0 ? actual is not null : actual is null || !actual.SequenceEqual(values))
            {
                throw new InvalidOperationException("Sequence did not produce the expected output.");
            }
        }
    }

    private IEnumerable<TItem> Shape<TItem>(TItem[] array)
        => Scenario.StartsWith("List", StringComparison.Ordinal) ? array.ToList() : array;

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

        return (T)(object)index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Result")]
    public List<T>? OriginalResult() => Old.EnumerableExtensions.Sequence(_original).UnwrapOr(null!);

    [Benchmark, BenchmarkCategory("Result")]
    public List<T>? CurrentResult() => _current.Sequence().UnwrapOr(null!);

    [Benchmark(Baseline = true), BenchmarkCategory("Option")]
    public List<T>? OriginalOption() => Old.EnumerableExtensions.Sequence(_oldOptions).UnwrapOr(null!);

    [Benchmark, BenchmarkCategory("Option")]
    public List<T>? CurrentOption() => _options.Sequence().UnwrapOr(null!);
}
