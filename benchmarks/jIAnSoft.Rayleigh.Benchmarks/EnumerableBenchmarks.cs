using BenchmarkDotNet.Attributes;

namespace jIAnSoft.Rayleigh.Benchmarks;

/// <summary>
/// 量測 <see cref="EnumerableExtensions"/> 的集合入口方法與組合子。
/// </summary>
/// <remarks>
/// <para>
/// 這組 benchmark 的目的不是宣稱「比 LINQ 快」——多數方法的語意與 LINQ 對應物並不相同
/// （<c>FirstOrNone</c> 能區分「找不到」與「找到預設值」，<c>FirstOrDefault</c> 不能），
/// 因此速度不是選用它們的理由。真正要驗證的是<b>正確性沒有以效能為代價</b>：
/// 在提供更強語意的同時，配置量與耗時應與 LINQ 對應物相當。
/// </para>
/// <para>
/// 其中 <c>FirstOrNone</c> 對 <see cref="IList{T}"/> 走索引存取而完全不建立 enumerator，
/// 預期會比走介面列舉的路徑略快，並且零配置。
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class EnumerableBenchmarks
{
    private int[] _numbers = [];
    private Option<int>[] _options = [];
    private Result<int, string>[] _allOk = [];
    private Result<int, string>[] _withErr = [];
    private Dictionary<string, int> _dictionary = [];

    /// <summary>集合大小。刻意涵蓋「小集合」與「中等集合」，因為短路行為的效益隨長度放大。</summary>
    [Params(8, 1024)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _numbers = [.. Enumerable.Range(0, Size)];
        _options = [.. Enumerable.Range(0, Size).Select(x => x % 3 == 0 ? Option<int>.None : Option<int>.Some(x))];
        _allOk = [.. Enumerable.Range(0, Size).Select(Result<int, string>.Ok)];

        // 錯誤刻意放在中段，用以觀察 Sequence 的短路效益與 Partition 的全量走訪成本差異。
        _withErr =
        [
            .. Enumerable.Range(0, Size)
                .Select(x => x == Size / 2 ? Result<int, string>.Err("boom") : Result<int, string>.Ok(x))
        ];

        _dictionary = Enumerable.Range(0, Size).ToDictionary(x => $"key{x}", x => x);
    }

    // ==========================================
    // 入口方法：與 LINQ 對照
    // ==========================================

    /// <summary>基準線：LINQ 的 FirstOrDefault（語意較弱，無法區分「空」與「預設值」）。</summary>
    [Benchmark(Baseline = true, Description = "FirstOrDefault (LINQ, 語意較弱)")]
    public int FirstOrDefault_Linq() => _numbers.FirstOrDefault();

    /// <summary>FirstOrNone 對 IList 走索引存取，不建立 enumerator。</summary>
    [Benchmark(Description = "FirstOrNone (IList 快速路徑)")]
    public Option<int> FirstOrNone_List() => _numbers.FirstOrNone();

    /// <summary>非 IList 來源，必須實際列舉。</summary>
    [Benchmark(Description = "FirstOrNone (延遲序列)")]
    public Option<int> FirstOrNone_Lazy() => _numbers.Where(static x => x >= 0).FirstOrNone();

    /// <summary>帶條件的 FirstOrNone，驗證短路。</summary>
    [Benchmark(Description = "FirstOrNone (predicate, 短路)")]
    public Option<int> FirstOrNone_Predicate() => _numbers.FirstOrNone(static x => x == 2);

    /// <summary>基準線：字典的 GetValueOrDefault（無法區分「不存在」與「值為 0」）。</summary>
    [Benchmark(Description = "GetValueOrDefault (LINQ, 語意較弱)")]
    public int GetValueOrDefault_Bcl() => _dictionary.GetValueOrDefault("key1");

    /// <summary>GetValueOrNone 應與 BCL 版本成本相當。</summary>
    [Benchmark(Description = "GetValueOrNone")]
    public Option<int> GetValueOrNone_Rayleigh() => _dictionary.GetValueOrNone("key1");

    // ==========================================
    // Values / Sequence / Partition
    // ==========================================

    /// <summary>Values() 的陣列快速路徑（延遲執行，此處以 count 強制列舉完畢）。</summary>
    [Benchmark(Description = "Values (陣列快速路徑)")]
    public int Values_Array()
    {
        var count = 0;
        foreach (var _ in _options.Values())
        {
            count++;
        }

        return count;
    }

    /// <summary>全部 Ok 的 Sequence，必須走訪整個序列。</summary>
    [Benchmark(Description = "Sequence (全部 Ok, 走訪全部)")]
    public Result<List<int>, string> Sequence_AllOk() => _allOk.Sequence();

    /// <summary>錯誤位於中段的 Sequence，預期在中點短路，成本約為全量的一半。</summary>
    [Benchmark(Description = "Sequence (中段 Err, 短路)")]
    public Result<List<int>, string> Sequence_ShortCircuit() => _withErr.Sequence();

    /// <summary>Partition 走訪全部並配置兩份清單，與 Sequence 的短路形成對照。</summary>
    [Benchmark(Description = "Partition (走訪全部, 蒐集所有錯誤)")]
    public int Partition_All()
    {
        var (values, errors) = _withErr.Partition();
        return values.Count + errors.Count;
    }
}
