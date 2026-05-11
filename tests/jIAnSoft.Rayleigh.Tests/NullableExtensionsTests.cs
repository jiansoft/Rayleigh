using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// 測試 <see cref="NullableExtensions"/> 靜態類別中的擴充方法，
/// 驗證可空參考類型與可空值類型正確轉換為 <see cref="Option{T}"/>。
/// </summary>
public class NullableExtensionsTests
{
    // ==========================================
    // 自訂測試用類別
    // ==========================================

    private record Person(string Name, int Age);

    // ==========================================
    // 參考類型 (class) - ToOption<T> where T : class
    // ==========================================

    /// <summary>
    /// 驗證非 null 的字串透過 ToOption 轉換後回傳 Some，且內含值與原始字串相同。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_String_NonNull_ReturnsSome()
    {
        var value = (string?)"hello";

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal("hello", result.Unwrap());
    }

    /// <summary>
    /// 驗證 null 字串透過 ToOption 轉換後回傳 None。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_String_Null_ReturnsNone()
    {
        var value = (string?)null;

        var result = value.ToOption();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證空字串 ("") 透過 ToOption 轉換後回傳 Some，因為空字串不是 null。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_String_Empty_ReturnsSome()
    {
        const string? value = "";

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal("", result.Unwrap());
    }

    /// <summary>
    /// 驗證非 null 的自訂類別實例透過 ToOption 轉換後回傳 Some，且內含值為原始物件。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_CustomClass_NonNull_ReturnsSome()
    {
        var person = (Person?)new Person("Alice", 30);

        var result = person.ToOption();

        Assert.True(result.IsSome);
        var unwrapped = result.Unwrap();
        Assert.Equal("Alice", unwrapped.Name);
        Assert.Equal(30, unwrapped.Age);
    }

    /// <summary>
    /// 驗證 null 的自訂類別實例透過 ToOption 轉換後回傳 None。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_CustomClass_Null_ReturnsNone()
    {
        var person = (Person?)null;

        var result = person.ToOption();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Some 狀態的 Option 轉換後可以正確使用 Match 取得值。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_NonNull_MatchReturnsSomeValue()
    {
        var value = (string?)"world";

        var result = value.ToOption().Match(
            some: v => v.ToUpperInvariant(),
            none: () => "NONE"
        );

        Assert.Equal("WORLD", result);
    }

    /// <summary>
    /// 驗證 None 狀態的 Option 透過 Match 執行 none 分支。
    /// </summary>
    [Fact]
    public void ToOption_ReferenceType_Null_MatchReturnsNoneValue()
    {
        var value = (string?)null;

        var result = value.ToOption().Match(
            some: v => v.ToUpperInvariant(),
            none: () => "NONE"
        );

        Assert.Equal("NONE", result);
    }

    // ==========================================
    // 值類型 (struct) - ToOption<T> where T : struct
    // ==========================================

    /// <summary>
    /// 驗證有值的 int? 透過 ToOption 轉換後回傳 Some，且內含值與原始值相同。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_HasValue_ReturnsSome()
    {
        var value = (int?)42;

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 null 的 int? 透過 ToOption 轉換後回傳 None。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_Null_ReturnsNone()
    {
        var value = (int?)null;

        var result = value.ToOption();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證值為零的 int? 透過 ToOption 轉換後回傳 Some(0)，因為 0 不是 null。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_Zero_ReturnsSome()
    {
        var value = (int?)0;

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal(0, result.Unwrap());
    }

    /// <summary>
    /// 驗證有值的 DateTime? 透過 ToOption 轉換後回傳 Some，且內含值與原始日期相同。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_DateTime_HasValue_ReturnsSome()
    {
        var expected = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var value = (DateTime?)expected;

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal(expected, result.Unwrap());
    }

    /// <summary>
    /// 驗證 null 的 DateTime? 透過 ToOption 轉換後回傳 None。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_DateTime_Null_ReturnsNone()
    {
        var value = (DateTime?)null;

        var result = value.ToOption();

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證有值的 int? 轉換後透過 Map 進行值轉換，結果正確。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_HasValue_MapWorksCorrectly()
    {
        var value = (int?)10;

        var result = value.ToOption().Map(v => v * 2);

        Assert.True(result.IsSome);
        Assert.Equal(20, result.Unwrap());
    }

    /// <summary>
    /// 驗證 null 的 int? 轉換後透過 Map 操作仍為 None。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_Null_MapReturnsNone()
    {
        var value = (int?)null;

        var result = value.ToOption().Map(v => v * 2);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證負數的 int? 透過 ToOption 轉換後回傳 Some，因為負數是有效值。
    /// </summary>
    [Fact]
    public void ToOption_ValueType_Int_NegativeValue_ReturnsSome()
    {
        var value = (int?)-100;

        var result = value.ToOption();

        Assert.True(result.IsSome);
        Assert.Equal(-100, result.Unwrap());
    }
}
