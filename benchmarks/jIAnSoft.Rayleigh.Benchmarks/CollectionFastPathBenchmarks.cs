using BenchmarkDotNet.Attributes;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 量化 <c>Sequence</c> / <c>Partition</c> 的集合快速路徑（陣列與 <see cref="List{T}"/> 走
/// <see cref="ReadOnlySpan{T}"/>）相對於泛用 <see cref="IEnumerable{T}"/> 列舉路徑的效益。
/// </summary>
/// <remarks>
/// <para>
/// 每個組合子都以三種來源量測，這三種來源在快速路徑加入前走的是<b>同一段</b>程式碼：
/// </para>
/// <list type="bullet">
///   <item><description><b>陣列</b>：span 快速路徑。</description></item>
///   <item><description><b>List</b>：span 快速路徑（經由 <c>CollectionsMarshal.AsSpan</c>）。</description></item>
///   <item><description><b>延遲序列</b>：泛用列舉路徑，作為未最佳化路徑的對照組，
///   同時確保該路徑本身沒有因為改動而退化。</description></item>
/// </list>
/// <para>
/// <b>判讀方式</b>：本檔案的數字必須與「修改前」的同一組數字對照才有意義。
/// 且務必分辨兩件事——net10.0 相對 net8.0 的差距是 Runtime／JIT 自身的改善
/// （同一份 IL 就會發生，與本次程式碼修改無關）；同一 runtime 內
/// 「陣列／List」與「延遲序列」的差距，才是快速路徑的貢獻。
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class CollectionFastPathBenchmarks
{
    private Result<int, string>[] _okArray = [];
    private List<Result<int, string>> _okList = [];
    private Option<int>[] _someArray = [];
    private Result<int, string>[] _mixedArray = [];

    /// <summary>集合大小。小集合用來檢查快速路徑的型別測試成本是否反而壓過收益。</summary>
    [Params(8, 1024)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _okArray = [.. Enumerable.Range(0, Size).Select(Result<int, string>.Ok)];
        _okList = [.. _okArray];
        _someArray = [.. Enumerable.Range(0, Size).Select(Option<int>.Some)];
        _mixedArray =
        [
            .. Enumerable.Range(0, Size)
                .Select(x => x % 4 == 0 ? Result<int, string>.Err("boom") : Result<int, string>.Ok(x))
        ];
    }

    /// <summary>把陣列藏在一個最小的迭代器後面，強制走泛用列舉路徑。</summary>
    private static IEnumerable<T> AsLazy<T>(T[] source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
    }

    // ==========================================
    // Sequence（Result 版）
    // ==========================================

    [Benchmark(Baseline = true, Description = "Sequence<Result> 延遲序列 (泛用路徑)")]
    public Result<List<int>, string> Sequence_Result_Lazy() => AsLazy(_okArray).Sequence();

    [Benchmark(Description = "Sequence<Result> 陣列 (span 快速路徑)")]
    public Result<List<int>, string> Sequence_Result_Array() => _okArray.Sequence();

    [Benchmark(Description = "Sequence<Result> List (span 快速路徑)")]
    public Result<List<int>, string> Sequence_Result_List() => _okList.Sequence();

    // ==========================================
    // Sequence（Option 版）
    // ==========================================

    [Benchmark(Description = "Sequence<Option> 延遲序列 (泛用路徑)")]
    public Option<List<int>> Sequence_Option_Lazy() => AsLazy(_someArray).Sequence();

    [Benchmark(Description = "Sequence<Option> 陣列 (span 快速路徑)")]
    public Option<List<int>> Sequence_Option_Array() => _someArray.Sequence();

    // ==========================================
    // Partition
    // ==========================================

    [Benchmark(Description = "Partition 延遲序列 (泛用路徑)")]
    public int Partition_Lazy()
    {
        var (values, errors) = AsLazy(_mixedArray).Partition();
        return values.Count + errors.Count;
    }

    [Benchmark(Description = "Partition 陣列 (span 快速路徑 + 容量預配)")]
    public int Partition_Array()
    {
        var (values, errors) = _mixedArray.Partition();
        return values.Count + errors.Count;
    }
}
