using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="Option{T}"/> 的隱式轉換功能。
/// 驗證從原始型別 T 轉換為 Option&lt;T&gt; 的行為是否符合預期。
/// </summary>
public class OptionImplicitConversionTests
{
    /// <summary>
    /// 驗證整數值可以隱式轉換為 Option&lt;int&gt;，且結果應為 Some。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromValue_CreatesSome()
    {
        Option<int> option = 42;

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Unwrap());
    }

    /// <summary>
    /// 驗證字串值可以隱式轉換為 Option&lt;string&gt;，且結果應為 Some。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromString_CreatesSome()
    {
        Option<string> option = "hello";

        Assert.True(option.IsSome);
        Assert.Equal("hello", option.Unwrap());
    }

    /// <summary>
    /// 驗證在方法呼叫時，可以直接傳入原始值並自動轉換為 Option 參數。
    /// 這是隱式轉換最主要的應用場景。
    /// </summary>
    [Fact]
    public void ImplicitConversion_InMethodCall_Works()
    {
        string GetMessage(Option<string> name) => name.Match(
            some: n => $"Hello, {n}",
            none: () => "Hello, Guest"
        );

        // 可以直接傳入字串，隱式轉換為 Option<string>
        var message = GetMessage("Alice");

        Assert.Equal("Hello, Alice", message);
    }
}
