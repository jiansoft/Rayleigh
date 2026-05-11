using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="EnumerableExtensions"/> 靜態類別中的擴充方法，
/// 驗證從 <see cref="Option{T}"/> 集合中過濾出 Some 值的行為是否正確。
/// </summary>
public class EnumerableExtensionsTests
{
    // ==========================================
    // Values - 混合 Some 與 None
    // ==========================================

    /// <summary>
    /// 驗證包含 Some 與 None 混合的集合，Values 只回傳 Some 內的值，
    /// 正確過濾掉所有 None 項目。
    /// </summary>
    [Fact]
    public void Values_MixedSomeAndNone_ReturnsOnlySomeValues()
    {
        var source = new[]
        {
            Option<int>.Some(1),
            Option<int>.None,
            Option<int>.Some(2),
            Option<int>.None,
            Option<int>.Some(3)
        };

        var result = source.Values().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal([1, 2, 3], result);
    }

    /// <summary>
    /// 驗證字串類型的混合集合，Values 只回傳 Some 內的字串值。
    /// </summary>
    [Fact]
    public void Values_MixedSomeAndNone_Strings_ReturnsOnlySomeValues()
    {
        var source = new[]
        {
            Option<string>.Some("Alice"),
            Option<string>.None,
            Option<string>.Some("Bob"),
            Option<string>.None
        };

        var result = source.Values().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(["Alice", "Bob"], result);
    }

    // ==========================================
    // Values - 全部 None
    // ==========================================

    /// <summary>
    /// 驗證全部為 None 的集合，Values 回傳空集合。
    /// </summary>
    [Fact]
    public void Values_AllNone_ReturnsEmpty()
    {
        var source = new[]
        {
            Option<int>.None,
            Option<int>.None,
            Option<int>.None
        };

        var result = source.Values().ToList();

        Assert.Empty(result);
    }

    /// <summary>
    /// 驗證字串類型全部為 None 的集合，Values 回傳空集合。
    /// </summary>
    [Fact]
    public void Values_AllNone_Strings_ReturnsEmpty()
    {
        var source = new[]
        {
            Option<string>.None,
            Option<string>.None
        };

        var result = source.Values().ToList();

        Assert.Empty(result);
    }

    // ==========================================
    // Values - 全部 Some
    // ==========================================

    /// <summary>
    /// 驗證全部為 Some 的集合，Values 回傳所有值，數量與原始集合相同。
    /// </summary>
    [Fact]
    public void Values_AllSome_ReturnsAllValues()
    {
        var source = new[]
        {
            Option<int>.Some(10),
            Option<int>.Some(20),
            Option<int>.Some(30)
        };

        var result = source.Values().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal([10, 20, 30], result);
    }

    /// <summary>
    /// 驗證字串類型全部為 Some 的集合，Values 回傳所有字串值。
    /// </summary>
    [Fact]
    public void Values_AllSome_Strings_ReturnsAllValues()
    {
        var source = new[]
        {
            Option<string>.Some("X"),
            Option<string>.Some("Y"),
            Option<string>.Some("Z")
        };

        var result = source.Values().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(["X", "Y", "Z"], result);
    }

    // ==========================================
    // Values - 空集合
    // ==========================================

    /// <summary>
    /// 驗證空的來源集合，Values 回傳空集合，不會拋出例外。
    /// </summary>
    [Fact]
    public void Values_EmptySource_ReturnsEmpty()
    {
        var source = Array.Empty<Option<int>>();

        var result = source.Values().ToList();

        Assert.Empty(result);
    }

    /// <summary>
    /// 驗證空的字串型別來源集合，Values 回傳空集合。
    /// </summary>
    [Fact]
    public void Values_EmptySource_Strings_ReturnsEmpty()
    {
        var source = Array.Empty<Option<string>>();

        var result = source.Values().ToList();

        Assert.Empty(result);
    }

    // ==========================================
    // Values - 搭配 LINQ 使用
    // ==========================================

    /// <summary>
    /// 驗證 Values 的回傳值可以繼續搭配 LINQ 操作，
    /// 確認惰性求值 (lazy evaluation) 的正確性。
    /// </summary>
    [Fact]
    public void Values_CanChainWithLinq_WorksCorrectly()
    {
        var source = new[]
        {
            Option<int>.Some(1),
            Option<int>.None,
            Option<int>.Some(2),
            Option<int>.Some(3)
        };

        var result = source.Values().Select(x => x * 10).ToList();

        Assert.Equal([10, 20, 30], result);
    }

    /// <summary>
    /// 驗證 Values 搭配 Where 過濾後僅保留符合條件的值。
    /// </summary>
    [Fact]
    public void Values_WithWhereFilter_ReturnsFilteredValues()
    {
        var source = new[]
        {
            Option<int>.Some(1),
            Option<int>.None,
            Option<int>.Some(5),
            Option<int>.Some(3),
            Option<int>.None,
            Option<int>.Some(8)
        };

        var result = source.Values().Where(x => x > 3).ToList();

        Assert.Equal([5, 8], result);
    }

    /// <summary>
    /// 驗證只有一個 Some 元素的集合，Values 回傳恰好一個值。
    /// </summary>
    [Fact]
    public void Values_SingleSome_ReturnsSingleValue()
    {
        var source = new[] { Option<int>.Some(42) };

        var result = source.Values().ToList();

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }

    /// <summary>
    /// 驗證只有一個 None 元素的集合，Values 回傳空集合。
    /// </summary>
    [Fact]
    public void Values_SingleNone_ReturnsEmpty()
    {
        var source = new[] { Option<int>.None };

        var result = source.Values().ToList();

        Assert.Empty(result);
    }
}
