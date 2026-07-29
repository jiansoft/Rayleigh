using System.Collections;
using System.Collections.ObjectModel;
using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// 驗證 <see cref="EnumerableExtensions"/> 的集合快速路徑（陣列／<see cref="List{T}"/> 走 span、
/// <see cref="IList{T}"/>／<see cref="IReadOnlyList{T}"/> 走索引存取）與泛用列舉路徑產生<b>完全相同</b>的結果。
/// </summary>
/// <remarks>
/// <para>
/// 快速路徑是純效能最佳化，正確性上必須與泛用路徑不可區分。這組測試的價值不在於證明它比較快
/// （那只有 BenchmarkDotNet 能證明），而在於確保未來調整快速路徑時，任何語意漂移都會立刻被抓到。
/// </para>
/// <para>
/// 每個案例都涵蓋三種來源型別：<b>陣列</b>（span 快速路徑）、<b><see cref="List{T}"/></b>（span 快速路徑）、
/// <b>延遲序列</b>（泛用列舉路徑）。這三者在快速路徑加入前走的是同一段程式碼，加入後則分岔為三條。
/// </para>
/// </remarks>
public class EnumerableFastPathTests
{
    // ================================================================
    // 測試輔助型別
    // ================================================================

    /// <summary>
    /// 只實作 <see cref="IEnumerable{T}"/> 的來源，且會記錄被列舉的次數與元素數，
    /// 用於驗證「不會重複列舉」與「短路確實停止列舉」。
    /// </summary>
    private sealed class CountingSequence<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }
        public int ItemsYielded { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            foreach (var item in items)
            {
                ItemsYielded++;
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 只實作 <see cref="IReadOnlyList{T}"/>（<b>不</b>實作 <see cref="IList{T}"/>）的集合。
    /// 這是 <c>is IList&lt;T&gt;</c> 型別測試無法命中的情況。
    /// </summary>
    private sealed class ReadOnlyListOnly<T>(IReadOnlyList<T> inner) : IReadOnlyList<T>
    {
        public T this[int index] => inner[index];
        public int Count => inner.Count;
        public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static IEnumerable<T> Lazy<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }

    // ================================================================
    // Sequence（Result 版）：三種來源必須一致
    // ================================================================

    /// <summary>
    /// 驗證全部 Ok 時，陣列／List／延遲序列三條路徑產生相同的成功清單。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(1024)]
    public void Sequence_Result_AllOk_AllSourceKindsAgree(int size)
    {
        var items = Enumerable.Range(0, size).Select(Result<int, string>.Ok).ToArray();
        var expected = Enumerable.Range(0, size).ToList();

        AssertOkWithValues(items.Sequence(), expected);
        AssertOkWithValues(new List<Result<int, string>>(items).Sequence(), expected);
        AssertOkWithValues(Lazy(items).Sequence(), expected);

        static void AssertOkWithValues(Result<List<int>, string> actual, List<int> expected)
        {
            Assert.True(actual.IsOk);
            Assert.Equal(expected, actual.Unwrap());
        }
    }

    /// <summary>
    /// 驗證遇到 Err 時，三條路徑都回傳<b>第一個</b>錯誤（而非最後一個或任意一個）。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(512)]
    public void Sequence_Result_FirstErrWins_AllSourceKindsAgree(int errorIndex)
    {
        const int size = 1024;
        var items = Enumerable.Range(0, size)
            .Select(i => i == errorIndex || i == errorIndex + 1
                ? Result<int, string>.Err($"boom-{i}")
                : Result<int, string>.Ok(i))
            .ToArray();

        var expected = $"boom-{errorIndex}";

        Assert.Equal(expected, items.Sequence().UnwrapErr());
        Assert.Equal(expected, new List<Result<int, string>>(items).Sequence().UnwrapErr());
        Assert.Equal(expected, Lazy(items).Sequence().UnwrapErr());
    }

    /// <summary>
    /// 驗證 Sequence 對延遲序列<b>只列舉一次</b>，且遇到 Err 就停止。
    /// </summary>
    /// <remarks>
    /// 泛用路徑會先呼叫 <c>TryGetNonEnumeratedCount</c> 預配容量。該方法不得觸發列舉——
    /// 若未來改成 <c>Count()</c> 之類的實作，本測試會失敗。
    /// </remarks>
    [Fact]
    public void Sequence_Result_LazySource_EnumeratesOnceAndShortCircuits()
    {
        var items = Enumerable.Range(0, 100)
            .Select(i => i == 10 ? Result<int, string>.Err("boom") : Result<int, string>.Ok(i));
        var source = new CountingSequence<Result<int, string>>(items);

        var result = source.Sequence();

        Assert.True(result.IsErr);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(11, source.ItemsYielded); // 0..9 為 Ok，第 11 個（索引 10）是 Err 後即停止
    }

    // ================================================================
    // Sequence（Option 版）
    // ================================================================

    /// <summary>
    /// 驗證 Option 版 Sequence 的三條路徑在全部 Some 時一致。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    public void Sequence_Option_AllSome_AllSourceKindsAgree(int size)
    {
        var items = Enumerable.Range(0, size).Select(Option<int>.Some).ToArray();
        var expected = Enumerable.Range(0, size).ToList();

        Assert.Equal(expected, items.Sequence().Unwrap());
        Assert.Equal(expected, new List<Option<int>>(items).Sequence().Unwrap());
        Assert.Equal(expected, Lazy(items).Sequence().Unwrap());
    }

    /// <summary>
    /// 驗證 Option 版 Sequence 遇到 None 時，三條路徑都回傳 None。
    /// </summary>
    [Fact]
    public void Sequence_Option_WithNone_AllSourceKindsReturnNone()
    {
        var items = Enumerable.Range(0, 64)
            .Select(i => i == 30 ? Option<int>.None : Option<int>.Some(i))
            .ToArray();

        Assert.True(items.Sequence().IsNone);
        Assert.True(new List<Option<int>>(items).Sequence().IsNone);
        Assert.True(Lazy(items).Sequence().IsNone);
    }

    /// <summary>
    /// 驗證 Option 版 Sequence 對延遲序列只列舉一次且短路。
    /// </summary>
    [Fact]
    public void Sequence_Option_LazySource_EnumeratesOnceAndShortCircuits()
    {
        var items = Enumerable.Range(0, 100).Select(i => i == 5 ? Option<int>.None : Option<int>.Some(i));
        var source = new CountingSequence<Option<int>>(items);

        Assert.True(source.Sequence().IsNone);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(6, source.ItemsYielded);
    }

    // ================================================================
    // Partition
    // ================================================================

    /// <summary>
    /// 驗證 Partition 的三條路徑產生相同的（成功值、錯誤）拆分，且保持原始順序。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    public void Partition_AllSourceKindsAgree(int size)
    {
        var items = Enumerable.Range(0, size)
            .Select(i => i % 3 == 0 ? Result<int, string>.Err($"e{i}") : Result<int, string>.Ok(i))
            .ToArray();

        var expectedValues = Enumerable.Range(0, size).Where(i => i % 3 != 0).ToList();
        var expectedErrors = Enumerable.Range(0, size).Where(i => i % 3 == 0).Select(i => $"e{i}").ToList();

        AssertPartition(items.Partition());
        AssertPartition(new List<Result<int, string>>(items).Partition());
        AssertPartition(Lazy(items).Partition());

        void AssertPartition((List<int> Values, List<string> Errors) actual)
        {
            Assert.Equal(expectedValues, actual.Values);
            Assert.Equal(expectedErrors, actual.Errors);
        }
    }

    /// <summary>
    /// 驗證 Partition 必然走訪<b>全部</b>元素（不短路），這是它與 Sequence 的核心差異。
    /// </summary>
    [Fact]
    public void Partition_LazySource_EnumeratesEveryElementExactlyOnce()
    {
        var items = Enumerable.Range(0, 50)
            .Select(i => i % 2 == 0 ? Result<int, string>.Err($"e{i}") : Result<int, string>.Ok(i));
        var source = new CountingSequence<Result<int, string>>(items);

        var (values, errors) = source.Partition();

        Assert.Equal(25, values.Count);
        Assert.Equal(25, errors.Count);
        Assert.Equal(1, source.EnumerationCount);
        Assert.Equal(50, source.ItemsYielded);
    }

    /// <summary>
    /// 驗證 Partition 的容量預配（只預配成功值清單）不會影響對外可觀察的行為：
    /// 全部為 Err 時，成功值清單仍必須是空的。
    /// </summary>
    [Fact]
    public void Partition_AllErr_ValuesListIsEmptyDespitePreallocation()
    {
        var items = Enumerable.Range(0, 100).Select(i => Result<int, string>.Err($"e{i}")).ToArray();

        var (values, errors) = items.Partition();

        Assert.Empty(values);
        Assert.Equal(100, errors.Count);
    }

    // ================================================================
    // 較大的 value type：驗證 ref readonly 走訪沒有改變語意
    // ================================================================

    private readonly record struct LargeValue(Guid A, Guid B, long C, long D);

    /// <summary>
    /// 以較大的 value type 具現化 Sequence，驗證 span 快速路徑的 <c>ref readonly</c> 走訪
    /// （避免每輪複製整個 struct）不會改變結果。
    /// </summary>
    [Fact]
    public void Sequence_LargeValueType_AllSourceKindsAgree()
    {
        var items = Enumerable.Range(0, 256)
            .Select(i => Result<LargeValue, string>.Ok(new LargeValue(Guid.Empty, Guid.Empty, i, i * 2)))
            .ToArray();

        var expected = items.Select(r => r.Unwrap()).ToList();

        Assert.Equal(expected, items.Sequence().Unwrap());
        Assert.Equal(expected, new List<Result<LargeValue, string>>(items).Sequence().Unwrap());
        Assert.Equal(expected, Lazy(items).Sequence().Unwrap());
    }

    // ================================================================
    // IReadOnlyList<T> 快速路徑（只實作 IReadOnlyList，不實作 IList）
    // ================================================================

    /// <summary>
    /// 驗證 FirstOrNone 對只實作 <see cref="IReadOnlyList{T}"/> 的來源仍能正確運作。
    /// </summary>
    [Fact]
    public void FirstOrNone_ReadOnlyListOnlySource_Works()
    {
        Assert.Equal(Option<int>.Some(10), new ReadOnlyListOnly<int>([10, 20, 30]).FirstOrNone());
        Assert.True(new ReadOnlyListOnly<int>([]).FirstOrNone().IsNone);

        // 值型別的預設值必須被視為「存在的元素」，而非「找不到」。
        Assert.Equal(Option<int>.Some(0), new ReadOnlyListOnly<int>([0]).FirstOrNone());
    }

    /// <summary>
    /// 驗證 SingleOrNone 對只實作 <see cref="IReadOnlyList{T}"/> 的來源仍能正確運作。
    /// </summary>
    [Fact]
    public void SingleOrNone_ReadOnlyListOnlySource_Works()
    {
        Assert.Equal(Option<int>.Some(10), new ReadOnlyListOnly<int>([10]).SingleOrNone());
        Assert.True(new ReadOnlyListOnly<int>([]).SingleOrNone().IsNone);
        Assert.True(new ReadOnlyListOnly<int>([10, 20]).SingleOrNone().IsNone);
    }

    /// <summary>
    /// 驗證 ElementAtOrNone 對只實作 <see cref="IReadOnlyList{T}"/> 的來源仍能正確運作，含邊界索引。
    /// </summary>
    [Fact]
    public void ElementAtOrNone_ReadOnlyListOnlySource_Works()
    {
        var source = new ReadOnlyListOnly<int>([10, 20, 30]);

        Assert.Equal(Option<int>.Some(10), source.ElementAtOrNone(0));
        Assert.Equal(Option<int>.Some(30), source.ElementAtOrNone(2));
        Assert.True(source.ElementAtOrNone(3).IsNone);
        Assert.True(source.ElementAtOrNone(-1).IsNone);
    }

    /// <summary>
    /// 驗證 <see cref="ReadOnlyCollection{T}"/>（同時實作 IList 與 IReadOnlyList）走的是 IList 分支且結果正確。
    /// </summary>
    [Fact]
    public void EntryPoints_ReadOnlyCollection_Works()
    {
        var source = new ReadOnlyCollection<int>([10, 20, 30]);

        Assert.Equal(Option<int>.Some(10), source.FirstOrNone());
        Assert.Equal(Option<int>.Some(20), source.ElementAtOrNone(1));
        Assert.True(source.SingleOrNone().IsNone);
    }

    // ================================================================
    // FirstOrNone(predicate) 的 IList 快速路徑
    // ================================================================

    /// <summary>
    /// 驗證帶 predicate 的 FirstOrNone 在 IList 快速路徑與泛用路徑上結果一致，包含「找不到」的情況。
    /// </summary>
    [Theory]
    [InlineData(2, true)]
    [InlineData(0, true)]
    [InlineData(999, false)]
    public void FirstOrNone_Predicate_ListAndLazyAgree(int target, bool expectedFound)
    {
        var items = Enumerable.Range(0, 100).ToArray();
        var list = new List<int>(items);

        var fromArray = items.FirstOrNone(x => x == target);
        var fromList = list.FirstOrNone(x => x == target);
        var fromLazy = Lazy(items).FirstOrNone(x => x == target);

        Assert.Equal(expectedFound, fromArray.IsSome);
        Assert.Equal(fromArray, fromList);
        Assert.Equal(fromArray, fromLazy);

        if (expectedFound)
        {
            Assert.Equal(target, fromArray.Unwrap());
        }
    }

    /// <summary>
    /// 驗證帶 predicate 的 FirstOrNone 會短路——找到第一個符合的元素後不再評估後續元素。
    /// </summary>
    [Fact]
    public void FirstOrNone_Predicate_ShortCircuitsOnList()
    {
        var list = new List<int>(Enumerable.Range(0, 100));
        var evaluated = 0;

        var result = list.FirstOrNone(x =>
        {
            evaluated++;
            return x == 3;
        });

        Assert.Equal(Option<int>.Some(3), result);
        Assert.Equal(4, evaluated);
    }

    /// <summary>
    /// 鎖定 predicate 多載的既有語意：走的是泛用列舉，因此若 <c>predicate</c> 在走訪期間修改來源
    /// <see cref="List{T}"/>，會得到 <see cref="List{T}"/> enumerator 一貫的
    /// <see cref="InvalidOperationException"/>——與直接對該集合寫 <c>foreach</c> 的行為一致。
    /// </summary>
    /// <remarks>
    /// 這正是本多載刻意不加集合快速路徑的理由之一：任何以索引或 span 取代 enumerator 的做法，
    /// 都會悄悄改變這個「走訪期間修改集合」的可觀察行為。本測試不主張這種寫法是好的用法，
    /// 只確保未來若有人再嘗試加入快速路徑，這項語意變更會立刻被發現。
    /// </remarks>
    [Fact]
    public void FirstOrNone_Predicate_MutatingPredicate_BehavesLikePlainForeach()
    {
        var list = new List<int>(Enumerable.Range(0, 10));

        Assert.Throws<InvalidOperationException>(() => list.FirstOrNone(x =>
        {
            if (x == 2)
            {
                list.Clear(); // 走訪期間清空來源
            }

            return x == 99;
        }));
    }
}
