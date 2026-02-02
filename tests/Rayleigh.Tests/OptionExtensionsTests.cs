using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="OptionExtensions"/> 靜態類別中的擴充方法，
/// 驗證巢狀 <see cref="Option{T}"/> 的展平 (Flatten) 行為是否正確。
/// </summary>
public class OptionExtensionsTests
{
    // ==========================================
    // Flatten - Some(Some(value)) -> Some(value)
    // ==========================================

    /// <summary>
    /// 驗證 Some(Some(42)) 經過 Flatten 後回傳 Some(42)，
    /// 確認外層與內層皆為 Some 時正確展平為單層 Option。
    /// </summary>
    [Fact]
    public void Flatten_SomeSome_ReturnsSomeWithInnerValue()
    {
        var nested = Option<Option<int>>.Some(Option<int>.Some(42));

        var result = nested.Flatten();

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Some(Some("hello")) 經過 Flatten 後回傳 Some("hello")，
    /// 測試字串類型的展平行為。
    /// </summary>
    [Fact]
    public void Flatten_SomeSome_String_ReturnsSomeWithInnerValue()
    {
        var nested = Option<Option<string>>.Some(Option<string>.Some("hello"));

        var result = nested.Flatten();

        Assert.True(result.IsSome);
        Assert.Equal("hello", result.Unwrap());
    }

    // ==========================================
    // Flatten - Some(None) -> None
    // ==========================================

    /// <summary>
    /// 驗證 Some(None) 經過 Flatten 後回傳 None，
    /// 確認外層為 Some 但內層為 None 時結果為 None。
    /// </summary>
    [Fact]
    public void Flatten_SomeNone_ReturnsNone()
    {
        var nested = Option<Option<int>>.Some(Option<int>.None);

        var result = nested.Flatten();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證字串類型的 Some(None) 經過 Flatten 後回傳 None。
    /// </summary>
    [Fact]
    public void Flatten_SomeNone_String_ReturnsNone()
    {
        var nested = Option<Option<string>>.Some(Option<string>.None);

        var result = nested.Flatten();

        Assert.True(result.IsNone);
    }

    // ==========================================
    // Flatten - None (outer) -> None
    // ==========================================

    /// <summary>
    /// 驗證外層為 None 的巢狀 Option 經過 Flatten 後回傳 None，
    /// 確認外層為 None 時不論內層狀態，結果皆為 None。
    /// </summary>
    [Fact]
    public void Flatten_OuterNone_ReturnsNone()
    {
        var nested = Option<Option<int>>.None;

        var result = nested.Flatten();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證字串類型外層為 None 的巢狀 Option 經過 Flatten 後回傳 None。
    /// </summary>
    [Fact]
    public void Flatten_OuterNone_String_ReturnsNone()
    {
        var nested = Option<Option<string>>.None;

        var result = nested.Flatten();

        Assert.True(result.IsNone);
    }

    // ==========================================
    // Flatten 等價性驗證
    // ==========================================

    /// <summary>
    /// 驗證 Flatten 的結果等同於 Bind(x => x)，
    /// 確認兩者在語意上完全一致。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_SomeSome()
    {
        var nested = Option<Option<int>>.Some(Option<int>.Some(99));

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證在 Some(None) 情境下，Flatten 的結果等同於 Bind(x => x)。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_SomeNone()
    {
        var nested = Option<Option<int>>.Some(Option<int>.None);

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證在外層 None 情境下，Flatten 的結果等同於 Bind(x => x)。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_OuterNone()
    {
        var nested = Option<Option<int>>.None;

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證 Map 產生的巢狀 Option 透過 Flatten 可以正確展平，
    /// 模擬實際使用場景中 Map 回傳 Option 導致巢狀的情況。
    /// </summary>
    [Fact]
    public void Flatten_AfterMap_ProducingNestedOption_FlattensCorrectly()
    {
        var input = Option<int>.Some(10);
        Option<int> TryDouble(int x) => x > 0 ? Option<int>.Some(x * 2) : Option<int>.None;

        var nested = input.Map(TryDouble);
        var result = nested.Flatten();

        Assert.True(result.IsSome);
        Assert.Equal(20, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Map 產生的巢狀 Option 在內層函數回傳 None 時，
    /// Flatten 後結果為 None。
    /// </summary>
    [Fact]
    public void Flatten_AfterMap_InnerReturnsNone_FlattensToNone()
    {
        var input = Option<int>.Some(-5);
        Option<int> TryDouble(int x) => x > 0 ? Option<int>.Some(x * 2) : Option<int>.None;

        var nested = input.Map(TryDouble);
        var result = nested.Flatten();

        Assert.True(result.IsNone);
    }
}
