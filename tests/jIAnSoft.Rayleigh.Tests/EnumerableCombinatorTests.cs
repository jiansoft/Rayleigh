using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// 驗證 <see cref="EnumerableExtensions"/> 中的集合入口方法（FirstOrNone 等）與組合子（Sequence、Partition）。
/// </summary>
/// <remarks>
/// 這些方法的核心價值在於「對值型別同樣正確」——
/// <c>FirstOrDefault</c> 與 <c>GetValueOrDefault</c> 無法區分「找不到」與「找到了預設值」，
/// 因此測試刻意大量使用 <c>0</c> 這個 <c>default(int)</c> 作為有效資料。
/// </remarks>
public class EnumerableCombinatorTests
{
    // ================================================================
    // 參數驗證時機
    // ================================================================

    /// <summary>
    /// 驗證 Values() 對 null 來源會「立即」拋出 ArgumentNullException，而非延遲到列舉時才拋出 NullReferenceException。
    /// </summary>
    /// <remarks>
    /// 含 yield return 的方法屬延遲執行，若把 null 檢查寫在 iterator 內，
    /// 呼叫端會在遠離錯誤來源的 foreach 現場才收到難以理解的 NullReferenceException。
    /// 本測試刻意「只呼叫、不列舉」，以確保檢查發生在正確的時機。
    /// </remarks>
    [Fact]
    public void Values_NullSource_ThrowsImmediatelyWithoutEnumeration()
    {
        IEnumerable<Option<int>> source = null!;

        // 注意：此處沒有 foreach，例外必須在呼叫當下就發生
        var ex = Assert.Throws<ArgumentNullException>(() => source.Values());
        Assert.Equal("source", ex.ParamName);
    }

    /// <summary>
    /// 驗證所有集合入口方法對 null 來源都拋出 ArgumentNullException。
    /// </summary>
    [Fact]
    public void EntryPoints_NullSource_ThrowArgumentNullException()
    {
        IEnumerable<int> nullSource = null!;
        IEnumerable<Option<int>> nullOptions = null!;
        IEnumerable<Result<int, string>> nullResults = null!;
        IReadOnlyDictionary<string, int> nullDict = null!;

        Assert.Throws<ArgumentNullException>(() => nullSource.FirstOrNone());
        Assert.Throws<ArgumentNullException>(() => nullSource.FirstOrNone(_ => true));
        Assert.Throws<ArgumentNullException>(() => nullSource.SingleOrNone());
        Assert.Throws<ArgumentNullException>(() => nullSource.ElementAtOrNone(0));
        Assert.Throws<ArgumentNullException>(() => nullOptions.Sequence());
        Assert.Throws<ArgumentNullException>(() => nullResults.Sequence());
        Assert.Throws<ArgumentNullException>(() => nullResults.Partition());
        Assert.Throws<ArgumentNullException>(() => nullDict.GetValueOrNone("k"));
        Assert.Throws<ArgumentNullException>(() => new Dictionary<string, int>().GetValueOrNone(null!));
    }

    /// <summary>
    /// 驗證 FirstOrNone 的 predicate 多載對 null predicate 拋出 ArgumentNullException。
    /// </summary>
    [Fact]
    public void FirstOrNone_NullPredicate_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new[] { 1 }.FirstOrNone(null!));
        Assert.Equal("predicate", ex.ParamName);
    }

    // ================================================================
    // FirstOrNone
    // ================================================================

    /// <summary>
    /// 驗證 FirstOrNone 能區分「空序列」與「第一個元素恰好是 default(T)」——
    /// 這正是 FirstOrDefault 做不到的事。
    /// </summary>
    [Fact]
    public void FirstOrNone_DistinguishesEmptyFromDefaultValue()
    {
        Assert.Equal(Option<int>.Some(0), new[] { 0, 5 }.FirstOrNone());
        Assert.Equal(Option<int>.None, Array.Empty<int>().FirstOrNone());

        // 對照組：FirstOrDefault 對這兩者回傳相同的 0，無從區分
        Assert.Equal(0, new[] { 0, 5 }.FirstOrDefault());
        Assert.Equal(0, Array.Empty<int>().FirstOrDefault());
    }

    /// <summary>
    /// 驗證 FirstOrNone 對非 IList 來源（延遲序列）同樣正確。
    /// </summary>
    [Fact]
    public void FirstOrNone_LazySequence_ReturnsFirstElement()
    {
        var lazy = Enumerable.Range(10, 3).Where(x => x > 10);

        Assert.Equal(Option<int>.Some(11), lazy.FirstOrNone());
        Assert.Equal(Option<int>.None, Enumerable.Empty<int>().FirstOrNone());
    }

    /// <summary>
    /// 驗證帶條件的 FirstOrNone。
    /// </summary>
    [Fact]
    public void FirstOrNone_WithPredicate_ReturnsFirstMatch()
    {
        var source = new[] { 1, 2, 3, 4 };

        Assert.Equal(Option<int>.Some(2), source.FirstOrNone(x => x % 2 == 0));
        Assert.Equal(Option<int>.None, source.FirstOrNone(x => x > 100));
    }

    /// <summary>
    /// 驗證 FirstOrNone 的短路行為：找到第一個符合的元素後不再列舉。
    /// </summary>
    [Fact]
    public void FirstOrNone_WithPredicate_ShortCircuits()
    {
        var visited = 0;

        var result = Enumerable.Range(1, 100)
            .Select(x =>
            {
                visited++;
                return x;
            })
            .FirstOrNone(x => x == 3);

        Assert.Equal(Option<int>.Some(3), result);
        Assert.Equal(3, visited);
    }

    // ================================================================
    // SingleOrNone / ElementAtOrNone
    // ================================================================

    /// <summary>
    /// 驗證 SingleOrNone：恰好一個元素才回傳 Some，空或多個皆為 None（且不拋出例外）。
    /// </summary>
    [Theory]
    [InlineData(new int[] { }, false, 0)]
    [InlineData(new[] { 7 }, true, 7)]
    [InlineData(new[] { 7, 8 }, false, 0)]
    public void SingleOrNone_ReturnsSomeOnlyForExactlyOneElement(int[] source, bool expectedIsSome, int expectedValue)
    {
        var result = source.SingleOrNone();

        Assert.Equal(expectedIsSome, result.IsSome);
        if (expectedIsSome)
        {
            Assert.Equal(expectedValue, result.Unwrap());
        }
    }

    /// <summary>
    /// 驗證 SingleOrNone 對非 IList 來源同樣正確，且多元素時不拋出例外（與 LINQ Single 的差異）。
    /// </summary>
    [Fact]
    public void SingleOrNone_LazySequenceWithMultipleElements_ReturnsNoneWithoutThrowing()
    {
        var lazy = Enumerable.Range(1, 5).Where(x => x > 3); // 4, 5

        Assert.Equal(Option<int>.None, lazy.SingleOrNone());
        Assert.Throws<InvalidOperationException>(() => lazy.Single());
    }

    /// <summary>
    /// 驗證 ElementAtOrNone 的邊界處理，包含負數索引。
    /// </summary>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void ElementAtOrNone_HandlesOutOfRangeIndices(int index, bool expectedIsSome)
    {
        var source = new[] { 10, 20, 30 };

        Assert.Equal(expectedIsSome, source.ElementAtOrNone(index).IsSome);
        Assert.Equal(expectedIsSome, source.Where(_ => true).ElementAtOrNone(index).IsSome);
    }

    // ================================================================
    // GetValueOrNone
    // ================================================================

    /// <summary>
    /// 驗證 GetValueOrNone 能區分「鍵不存在」與「值為 default(TValue)」。
    /// </summary>
    [Fact]
    public void GetValueOrNone_DistinguishesMissingKeyFromDefaultValue()
    {
        var counts = new Dictionary<string, int> { ["zero"] = 0 };

        Assert.Equal(Option<int>.Some(0), counts.GetValueOrNone("zero"));
        Assert.Equal(Option<int>.None, counts.GetValueOrNone("missing"));

        // 對照組：GetValueOrDefault 對這兩者回傳相同的 0
        Assert.Equal(0, counts.GetValueOrDefault("zero"));
        Assert.Equal(0, counts.GetValueOrDefault("missing"));
    }

    // ================================================================
    // Sequence
    // ================================================================

    /// <summary>
    /// 驗證 Option 的 Sequence：全部 Some 才成功。
    /// </summary>
    [Fact]
    public void Sequence_Option_AllSome_ReturnsSomeWithAllValues()
    {
        Option<int>[] source = [Option<int>.Some(1), Option<int>.Some(2), Option<int>.Some(3)];

        var result = source.Sequence();

        Assert.True(result.IsSome);
        Assert.Equal([1, 2, 3], result.Unwrap());
    }

    /// <summary>
    /// 驗證 Option 的 Sequence 遇到 None 即整體失敗，且會短路停止列舉。
    /// </summary>
    [Fact]
    public void Sequence_Option_WithNone_ReturnsNoneAndShortCircuits()
    {
        var visited = 0;

        var result = new[] { 1, 2, 3, 4 }
            .Select(x =>
            {
                visited++;
                return x == 2 ? Option<int>.None : Option<int>.Some(x);
            })
            .Sequence();

        Assert.True(result.IsNone);
        Assert.Equal(2, visited); // 走到第 2 個就停止，未走訪 3 與 4
    }

    /// <summary>
    /// 驗證空序列的 Sequence 回傳「包含空清單」的 Some，而非 None。
    /// </summary>
    [Fact]
    public void Sequence_Option_EmptySource_ReturnsSomeEmptyList()
    {
        var result = Array.Empty<Option<int>>().Sequence();

        Assert.True(result.IsSome);
        Assert.Empty(result.Unwrap());
    }

    /// <summary>
    /// 驗證 Result 的 Sequence：全部 Ok 才成功。
    /// </summary>
    [Fact]
    public void Sequence_Result_AllOk_ReturnsOkWithAllValues()
    {
        Result<int, string>[] source =
        [
            Result<int, string>.Ok(1),
            Result<int, string>.Ok(2)
        ];

        var result = source.Sequence();

        Assert.True(result.IsOk);
        Assert.Equal([1, 2], result.Unwrap());
    }

    /// <summary>
    /// 驗證 Result 的 Sequence 回傳「第一個」遇到的錯誤，並短路停止列舉。
    /// </summary>
    [Fact]
    public void Sequence_Result_WithErr_ReturnsFirstErrorAndShortCircuits()
    {
        var visited = 0;

        var result = new[] { 1, 2, 3, 4 }
            .Select(x =>
            {
                visited++;
                return x >= 2 ? Result<int, string>.Err($"bad:{x}") : Result<int, string>.Ok(x);
            })
            .Sequence();

        Assert.True(result.IsErr);
        Assert.Equal("bad:2", result.UnwrapErr()); // 第一個錯誤，不是最後一個
        Assert.Equal(2, visited);
    }

    /// <summary>
    /// 驗證 Sequence 對值型別錯誤（enum）同樣正確運作。
    /// </summary>
    [Fact]
    public void Sequence_Result_WithEnumError_PropagatesError()
    {
        Result<int, ResultTests.TestError>[] source =
        [
            Result<int, ResultTests.TestError>.Ok(1),
            Result<int, ResultTests.TestError>.Err(ResultTests.TestError.NotFound)
        ];

        var result = source.Sequence();

        Assert.True(result.IsErr);
        Assert.Equal(ResultTests.TestError.NotFound, result.UnwrapErr());
    }

    // ================================================================
    // Partition
    // ================================================================

    /// <summary>
    /// 驗證 Partition 蒐集「全部」的成功值與錯誤，不會在第一個錯誤短路。
    /// </summary>
    [Fact]
    public void Partition_CollectsAllValuesAndAllErrors()
    {
        Result<int, string>[] source =
        [
            Result<int, string>.Ok(1),
            Result<int, string>.Err("e1"),
            Result<int, string>.Ok(2),
            Result<int, string>.Err("e2")
        ];

        var (values, errors) = source.Partition();

        Assert.Equal([1, 2], values);
        Assert.Equal(["e1", "e2"], errors);
    }

    /// <summary>
    /// 驗證 Partition 對空序列回傳兩個空清單。
    /// </summary>
    [Fact]
    public void Partition_EmptySource_ReturnsTwoEmptyLists()
    {
        var (values, errors) = Array.Empty<Result<int, string>>().Partition();

        Assert.Empty(values);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 驗證 Partition 與 Sequence 的差異：前者蒐集所有錯誤，後者在第一個錯誤即短路。
    /// </summary>
    [Fact]
    public void Partition_VersusSequence_DiffersInErrorCollection()
    {
        Result<int, string>[] source =
        [
            Result<int, string>.Err("first"),
            Result<int, string>.Err("second")
        ];

        var (_, errors) = source.Partition();
        var sequenced = source.Sequence();

        Assert.Equal(["first", "second"], errors); // Partition：全部蒐集
        Assert.Equal("first", sequenced.UnwrapErr()); // Sequence：只有第一個
    }
}
