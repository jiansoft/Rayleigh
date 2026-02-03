using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="ResultExtensions"/> 靜態類別中的擴充方法，
/// 驗證巢狀 <see cref="Result{T,E}"/> 的展平 (Flatten) 行為是否正確。
/// </summary>
public class ResultExtensionsTests
{
    // ==========================================
    // Flatten - Ok(Ok(value)) -> Ok(value)
    // ==========================================

    /// <summary>
    /// 驗證 Ok(Ok(42)) 經過 Flatten 後回傳 Ok(42)，
    /// 確認外層與內層皆為 Ok 時正確展平為單層 Result。
    /// </summary>
    [Fact]
    public void Flatten_OkOk_ReturnsOkWithInnerValue()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(42));

        var result = nested.Flatten();

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證字串類型的 Ok(Ok("hello")) 經過 Flatten 後回傳 Ok("hello")。
    /// </summary>
    [Fact]
    public void Flatten_OkOk_String_ReturnsOkWithInnerValue()
    {
        var nested = Result<Result<string, string>, string>.Ok(Result<string, string>.Ok("hello"));

        var result = nested.Flatten();

        Assert.True(result.IsOk);
        Assert.Equal("hello", result.Unwrap());
    }

    // ==========================================
    // Flatten - Ok(Err("inner")) -> Err("inner")
    // ==========================================

    /// <summary>
    /// 驗證 Ok(Err("inner error")) 經過 Flatten 後回傳 Err("inner error")，
    /// 確認外層為 Ok 但內層為 Err 時，內層的錯誤被正確傳遞。
    /// </summary>
    [Fact]
    public void Flatten_OkErr_ReturnsInnerError()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("inner error"));

        var result = nested.Flatten();

        Assert.True(result.IsErr);
        Assert.Equal("inner error", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證使用整數錯誤類型時，Ok(Err(404)) 經過 Flatten 後回傳 Err(404)。
    /// </summary>
    [Fact]
    public void Flatten_OkErr_IntError_ReturnsInnerError()
    {
        var nested = Result<Result<string, int>, int>.Ok(Result<string, int>.Err(404));

        var result = nested.Flatten();

        Assert.True(result.IsErr);
        Assert.Equal(404, result.UnwrapErr());
    }

    // ==========================================
    // Flatten - Err("outer") -> Err("outer")
    // ==========================================

    /// <summary>
    /// 驗證外層為 Err("outer error") 的巢狀 Result 經過 Flatten 後回傳 Err("outer error")，
    /// 確認外層錯誤被正確傳遞，不論內層狀態。
    /// </summary>
    [Fact]
    public void Flatten_OuterErr_ReturnsOuterError()
    {
        var nested = Result<Result<int, string>, string>.Err("outer error");

        var result = nested.Flatten();

        Assert.True(result.IsErr);
        Assert.Equal("outer error", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證使用整數錯誤類型時，外層 Err(500) 經過 Flatten 後回傳 Err(500)。
    /// </summary>
    [Fact]
    public void Flatten_OuterErr_IntError_ReturnsOuterError()
    {
        var nested = Result<Result<string, int>, int>.Err(500);

        var result = nested.Flatten();

        Assert.True(result.IsErr);
        Assert.Equal(500, result.UnwrapErr());
    }

    // ==========================================
    // Flatten 等價性驗證
    // ==========================================

    /// <summary>
    /// 驗證 Flatten 的結果等同於 Bind(x => x)，
    /// 確認在 Ok(Ok(value)) 情境下兩者語意完全一致。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_OkOk()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(99));

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證在 Ok(Err) 情境下，Flatten 的結果等同於 Bind(x => x)。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_OkErr()
    {
        var nested = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("fail"));

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證在外層 Err 情境下，Flatten 的結果等同於 Bind(x => x)。
    /// </summary>
    [Fact]
    public void Flatten_EquivalentToBind_OuterErr()
    {
        var nested = Result<Result<int, string>, string>.Err("outer");

        var flattenResult = nested.Flatten();
        var bindResult = nested.Bind(x => x);

        Assert.Equal(flattenResult, bindResult);
    }

    /// <summary>
    /// 驗證 Map 產生的巢狀 Result 透過 Flatten 可以正確展平，
    /// 模擬實際使用場景中 Map 回傳 Result 導致巢狀的情況。
    /// </summary>
    [Fact]
    public void Flatten_AfterMap_ProducingNestedResult_FlattensCorrectly()
    {
        var source = Result<int, string>.Ok(10);
        Result<int, string> Validate(int x) =>
            x > 0 ? Result<int, string>.Ok(x * 2) : Result<int, string>.Err("must be positive");

        var nested = source.Map(Validate);
        var result = nested.Flatten();

        Assert.True(result.IsOk);
        Assert.Equal(20, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Map 產生的巢狀 Result 在內層函數回傳 Err 時，
    /// Flatten 後結果為 Err 且包含內層錯誤訊息。
    /// </summary>
    [Fact]
    public void Flatten_AfterMap_InnerReturnsErr_FlattensToErr()
    {
        var source = Result<int, string>.Ok(-5);
        Result<int, string> Validate(int x) =>
            x > 0 ? Result<int, string>.Ok(x * 2) : Result<int, string>.Err("must be positive");

        var nested = source.Map(Validate);
        var result = nested.Flatten();

        Assert.True(result.IsErr);
        Assert.Equal("must be positive", result.UnwrapErr());
    }
}
