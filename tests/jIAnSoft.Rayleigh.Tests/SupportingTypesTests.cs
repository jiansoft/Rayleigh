using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// 驗證輔助型別 <see cref="OptionNone"/>、<see cref="Option"/> 靜態工廠、<see cref="Ok{T}"/>／<see cref="Err{TE}"/>
/// 包裹記錄，以及 <see cref="Unit"/> 的非泛型 <see cref="IComparable"/> 實作。
/// </summary>
/// <remarks>
/// 這些型別體積小、行為單純，因而長期缺乏測試覆蓋——但它們全都位於公開 API 表面：
/// <see cref="OptionNone"/> 是 <c>Option.None</c> 萬用標記的實際型別，任何人寫
/// <c>Option&lt;T&gt; x = Option.None;</c> 都會經過它。本檔案補上這塊缺口。
/// </remarks>
public class SupportingTypesTests
{
    // ================================================================
    // OptionNone — Option.None 萬用標記的實際型別
    // ================================================================

    /// <summary>
    /// 驗證所有 <see cref="OptionNone"/> 實例彼此相等——它是無欄位的標記型別，不存在「不同的 None」。
    /// </summary>
    [Fact]
    public void OptionNone_AllInstancesAreEqual()
    {
        var a = Option.None;
        var b = Option.None;
        var c = default(OptionNone);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals(c));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    /// <summary>
    /// 驗證 <see cref="OptionNone.Equals(object?)"/> 對非 OptionNone 物件與 null 回傳 false。
    /// </summary>
    [Fact]
    public void OptionNone_EqualsObject_ReturnsFalseForOtherTypes()
    {
        var none = Option.None;

        Assert.True(none.Equals((object)Option.None));
        Assert.False(none.Equals((object?)null));
        Assert.False(none.Equals("None"));
        Assert.False(none.Equals(0));
    }

    /// <summary>
    /// 驗證 <see cref="OptionNone"/> 的 GetHashCode 恆定，且 ToString 為 "None"。
    /// </summary>
    [Fact]
    public void OptionNone_HashCodeIsStable_AndToStringIsNone()
    {
        Assert.Equal(Option.None.GetHashCode(), default(OptionNone).GetHashCode());
        Assert.Equal("None", Option.None.ToString());
    }

    /// <summary>
    /// 驗證萬用 None 標記可隱式轉換為任意型別的 <see cref="Option{T}"/>——這是它存在的唯一理由。
    /// </summary>
    [Fact]
    public void OptionNone_ImplicitlyConvertsToAnyOptionType()
    {
        Option<int> ofInt = Option.None;
        Option<string> ofString = Option.None;
        Option<Guid> ofGuid = Option.None;

        Assert.True(ofInt.IsNone);
        Assert.True(ofString.IsNone);
        Assert.True(ofGuid.IsNone);
        Assert.Equal(Option<int>.None, ofInt);
    }

    // ================================================================
    // Option 靜態工廠
    // ================================================================

    /// <summary>
    /// 驗證 <see cref="Option.Some{T}(T)"/> 的型別推斷版本，與 <c>Option&lt;T&gt;.Some</c> 等價。
    /// </summary>
    [Fact]
    public void OptionStatic_Some_InfersTypeAndMatchesGenericFactory()
    {
        var inferred = Option.Some(42);
        var explicitly = Option<int>.Some(42);

        Assert.Equal(explicitly, inferred);
        Assert.Equal(42, inferred.Unwrap());
        Assert.Equal(Option<string>.Some("x"), Option.Some("x"));
    }

    /// <summary>
    /// 驗證 <see cref="Option.Some{T}(T)"/> 對 null 拋出 ArgumentNullException，與泛型工廠一致。
    /// </summary>
    [Fact]
    public void OptionStatic_Some_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));
    }

    /// <summary>
    /// 驗證 <see cref="Option.None"/> 每次取得的都是等價的標記值。
    /// </summary>
    [Fact]
    public void OptionStatic_None_ReturnsEquivalentMarker()
    {
        Assert.Equal(Option.None, Option.None);
    }

    // ================================================================
    // Ok<T> / Err<TE> 包裹記錄
    // ================================================================

    /// <summary>
    /// 驗證 <see cref="Ok{T}"/> 的值相等性、ToString，以及隱式轉換為 <see cref="Result{T,TE}"/>。
    /// </summary>
    [Fact]
    public void OkWrapper_HasValueEquality_AndConvertsToResult()
    {
        var a = new Ok<int>(42);
        var b = new Ok<int>(42);
        var c = new Ok<int>(7);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal("Ok(42)", a.ToString());

        Result<int, string> result = a;
        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 <see cref="Err{TE}"/> 的值相等性、ToString，以及隱式轉換為 <see cref="Result{T,TE}"/>。
    /// </summary>
    [Fact]
    public void ErrWrapper_HasValueEquality_AndConvertsToResult()
    {
        var a = new Err<string>("boom");
        var b = new Err<string>("boom");
        var c = new Err<string>("other");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal("Err(boom)", a.ToString());

        Result<int, string> result = a;
        Assert.True(result.IsErr);
        Assert.Equal("boom", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證包裹記錄是 <c>Result&lt;string, string&gt;</c>（T 與 TE 同型別）唯一可用的簡潔寫法。
    /// </summary>
    /// <remarks>
    /// 當 T 與 TE 具現化為同一型別時，<c>implicit operator Result&lt;T,TE&gt;(T)</c> 與
    /// <c>implicit operator Result&lt;T,TE&gt;(TE)</c> 的簽章會重複，直接寫 <c>return "value";</c>
    /// 會得到編譯錯誤 CS0457。包裹記錄的轉換來源型別是 <c>Ok&lt;string&gt;</c>／<c>Err&lt;string&gt;</c>，
    /// 兩者不同，因此不受影響。
    /// </remarks>
    [Fact]
    public void Wrappers_EnableConciseSyntax_WhenValueAndErrorShareTheSameType()
    {
        static Result<string, string> Parse(bool succeed)
            => succeed ? new Ok<string>("parsed") : new Err<string>("invalid");

        Assert.Equal("parsed", Parse(true).Unwrap());
        Assert.Equal("invalid", Parse(false).UnwrapErr());
    }

    // ================================================================
    // Unit 的非泛型 IComparable
    // ================================================================

    /// <summary>
    /// 驗證 <see cref="Unit.CompareTo(object?)"/> 對 null 回傳 1（依 .NET 慣例，任何值都大於 null）。
    /// </summary>
    [Fact]
    public void Unit_CompareToObject_WithNull_ReturnsOne()
    {
        Assert.Equal(1, Unit.Value.CompareTo(null));
    }

    /// <summary>
    /// 驗證 <see cref="Unit.CompareTo(object?)"/> 對另一個 Unit 回傳 0——所有 Unit 值皆相等。
    /// </summary>
    [Fact]
    public void Unit_CompareToObject_WithUnit_ReturnsZero()
    {
        Assert.Equal(0, Unit.Value.CompareTo((object)Unit.Value));
        Assert.Equal(0, Unit.Value.CompareTo((object)default(Unit)));
    }

    /// <summary>
    /// 驗證 <see cref="Unit.CompareTo(object?)"/> 對非 Unit 型別拋出 ArgumentException 並帶有正確的 paramName。
    /// </summary>
    [Fact]
    public void Unit_CompareToObject_WithWrongType_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Unit.Value.CompareTo("not a unit"));
        Assert.Equal("obj", ex.ParamName);
    }

    /// <summary>
    /// 驗證 <see cref="Unit"/> 透過非泛型 <see cref="IComparable"/> 介面可被僅識別該介面的舊有 API 使用。
    /// </summary>
    /// <remarks>這正是補上非泛型多載的目的——與 <see cref="Option{T}"/>、<see cref="Result{T,TE}"/> 保持一致。</remarks>
    [Fact]
    public void Unit_IsUsableThroughNonGenericIComparable()
    {
        IComparable comparable = Unit.Value;

        Assert.Equal(0, comparable.CompareTo(Unit.Value));
        Assert.Equal(1, comparable.CompareTo(null));
    }

    /// <summary>
    /// 驗證一組 Unit 可透過非泛型比較介面排序而不拋出例外。
    /// </summary>
    [Fact]
    public void Unit_SortingCollection_DoesNotThrow()
    {
        var units = new object[] { Unit.Value, default(Unit), Unit.Value };

        Array.Sort(units);

        Assert.Equal(3, units.Length);
        Assert.All(units, u => Assert.IsType<Unit>(u));
    }
}
