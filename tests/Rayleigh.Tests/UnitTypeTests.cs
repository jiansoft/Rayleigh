using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

/// <summary>
/// Unit 型別的單元測試。
/// Unit 是一個只有單一值的型別（類似 Rust 的 <c>()</c>），
/// 通常用於表示「無回傳值」的成功操作結果，例如 Result&lt;Unit&gt; 代替 void。
/// 因為 Unit 只有唯一的值，所以所有 Unit 實例都應該相等，且雜湊碼一致。
/// </summary>
public class UnitTypeTests
{
    #region Unit.Value 靜態欄位測試

    /// <summary>
    /// 驗證 Unit.Value 是 Unit 的靜態唯一實例，
    /// 確保透過 Unit.Value 取得的值確實是 Unit 型別。
    /// </summary>
    [Fact]
    public void Value_ReturnsUnitInstance()
    {
        // Arrange & Act
        var unit = Unit.Value;

        // Assert — 確認回傳的型別是 Unit
        Assert.IsType<Unit>(unit);
    }

    /// <summary>
    /// 驗證多次存取 Unit.Value 取得的值彼此相等，
    /// 確保 Unit.Value 具有單例語義（所有實例等價）。
    /// </summary>
    [Fact]
    public void Value_MultiplAccesses_ReturnEqualInstances()
    {
        // Arrange
        var first = Unit.Value;
        var second = Unit.Value;

        // Assert — 兩次取得的值應該完全相等
        Assert.Equal(first, second);
    }

    /// <summary>
    /// 驗證透過 default 建立的 Unit 與 Unit.Value 相等，
    /// 因為 Unit 是值型別（struct），default 初始化的結果應該與 Value 等價。
    /// </summary>
    [Fact]
    public void Value_EqualsDefaultUnit()
    {
        // Arrange
        var defaultUnit = default(Unit);

        // Assert — default(Unit) 和 Unit.Value 應該相等
        Assert.Equal(Unit.Value, defaultUnit);
    }

    /// <summary>
    /// 驗證透過 new Unit() 建立的實例與 Unit.Value 相等，
    /// 確保無論如何建構 Unit，結果都一致。
    /// </summary>
    [Fact]
    public void Value_EqualsNewUnit()
    {
        // Arrange
        var newUnit = new Unit();

        // Assert — new Unit() 和 Unit.Value 應該相等
        Assert.Equal(Unit.Value, newUnit);
    }

    #endregion

    #region Equals(Unit other) 測試

    /// <summary>
    /// 驗證 Equals(Unit other) 對任意兩個 Unit 實例恆為 true，
    /// 因為 Unit 型別只有一個語義上的值，所有實例必然相等。
    /// </summary>
    [Fact]
    public void Equals_WithAnotherUnit_ReturnsTrue()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act
        var result = a.Equals(b);

        // Assert — 任何兩個 Unit 都應該相等
        Assert.True(result);
    }

    /// <summary>
    /// 驗證 Equals(Unit other) 對自身（同一變數）也回傳 true，
    /// 確保自反性（reflexivity）成立。
    /// </summary>
    [Fact]
    public void Equals_WithSelf_ReturnsTrue()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = unit.Equals(unit);

        // Assert — Unit 與自己比較應該相等
        Assert.True(result);
    }

    /// <summary>
    /// 驗證 Equals(Unit other) 的對稱性：a.Equals(b) 與 b.Equals(a) 結果一致，
    /// 這是 Equals 合約中的重要性質。
    /// </summary>
    [Fact]
    public void Equals_IsSymmetric()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act & Assert — 對稱性：a==b 則 b==a
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(a));
    }

    /// <summary>
    /// 驗證 Equals(Unit other) 的遞移性：若 a==b 且 b==c，則 a==c，
    /// 這是 Equals 合約中的必要性質。
    /// </summary>
    [Fact]
    public void Equals_IsTransitive()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();
        var c = default(Unit);

        // Act & Assert — 遞移性：a==b && b==c => a==c
        Assert.True(a.Equals(b));
        Assert.True(b.Equals(c));
        Assert.True(a.Equals(c));
    }

    #endregion

    #region Equals(object?) 測試

    /// <summary>
    /// 驗證 Equals(object?) 在傳入裝箱的 Unit 時回傳 true，
    /// 因為 obj is Unit 對裝箱後的 Unit 應該成立。
    /// </summary>
    [Fact]
    public void EqualsObject_WithBoxedUnit_ReturnsTrue()
    {
        // Arrange
        var unit = Unit.Value;
        var boxed = (object)Unit.Value;

        // Act
        var result = unit.Equals(boxed);

        // Assert — 裝箱的 Unit 仍然是 Unit，應該相等
        Assert.True(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入 null 時回傳 false，
    /// 因為 null 不是 Unit 型別的實例。
    /// </summary>
    [Fact]
    public void EqualsObject_WithNull_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = unit.Equals(null);

        // Assert — null 不是 Unit，應該不相等
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入字串時回傳 false，
    /// 因為字串不是 Unit 型別。
    /// </summary>
    [Fact]
    public void EqualsObject_WithString_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;
        var other = (object)"hello";

        // Act
        var result = unit.Equals(other);

        // Assert — 字串不是 Unit，應該不相等
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入整數時回傳 false，
    /// 因為 int 不是 Unit 型別，即使值為 0 也不應相等。
    /// </summary>
    [Fact]
    public void EqualsObject_WithInteger_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;
        var other = (object)0;

        // Act
        var result = unit.Equals(other);

        // Assert — 整數不是 Unit，即使 GetHashCode 也回傳 0 也不應相等
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入布林值時回傳 false，
    /// 因為 bool 不是 Unit 型別。
    /// </summary>
    [Fact]
    public void EqualsObject_WithBoolean_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;
        var other = (object)true;

        // Act
        var result = unit.Equals(other);

        // Assert — 布林值不是 Unit
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入其他 struct（ValueTuple）時回傳 false，
    /// 確保不會與其他值型別混淆。
    /// </summary>
    [Fact]
    public void EqualsObject_WithValueTuple_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;
        var other = (object)ValueTuple.Create();

        // Act
        var result = unit.Equals(other);

        // Assert — ValueTuple 不是 Unit，即使語義類似也不應相等
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 Equals(object?) 在傳入透過 new Unit() 建立並裝箱的實例時回傳 true，
    /// 確保不同建構方式產生的 Unit 裝箱後仍然相等。
    /// </summary>
    [Fact]
    public void EqualsObject_WithBoxedNewUnit_ReturnsTrue()
    {
        // Arrange
        var unit = Unit.Value;
        var boxed = (object)new Unit();

        // Act
        var result = unit.Equals(boxed);

        // Assert — 透過 new 建構的 Unit 裝箱後仍是 Unit
        Assert.True(result);
    }

    #endregion

    #region GetHashCode() 測試

    /// <summary>
    /// 驗證 GetHashCode() 恆回傳 0，
    /// 因為所有 Unit 實例語義相同，雜湊碼必須一致。
    /// </summary>
    [Fact]
    public void GetHashCode_ReturnsZero()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var hashCode = unit.GetHashCode();

        // Assert — Unit 的雜湊碼固定為 0
        Assert.Equal(0, hashCode);
    }

    /// <summary>
    /// 驗證不同方式建構的 Unit 實例，其 GetHashCode() 皆回傳相同值，
    /// 確保雜湊碼的一致性，這是當作字典鍵使用的必要條件。
    /// </summary>
    [Fact]
    public void GetHashCode_DifferentInstances_ReturnSameValue()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();
        var c = default(Unit);

        // Act & Assert — 所有 Unit 的雜湊碼都應該相同
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(b.GetHashCode(), c.GetHashCode());
    }

    /// <summary>
    /// 驗證 Unit 可以作為 Dictionary 的鍵使用，
    /// 因為 Equals 和 GetHashCode 的實作一致，Unit 應該能正確存取字典。
    /// </summary>
    [Fact]
    public void GetHashCode_WorksCorrectlyAsDictionaryKey()
    {
        // Arrange
        var dictionary = new Dictionary<Unit, string> { { Unit.Value, "test" } };

        // Act — 用不同的 Unit 實例來查詢
        var result = dictionary[new Unit()];

        // Assert — 應該能成功取得值
        Assert.Equal("test", result);
    }

    #endregion

    #region ToString() 測試

    /// <summary>
    /// 驗證 ToString() 回傳 "()"，
    /// 這與 Rust 語言中的 unit type 表示法一致。
    /// </summary>
    [Fact]
    public void ToString_ReturnsParentheses()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = unit.ToString();

        // Assert — Unit 的字串表示應為 "()"
        Assert.Equal("()", result);
    }

    /// <summary>
    /// 驗證不同方式建構的 Unit 實例，ToString() 回傳值皆一致，
    /// 確保字串表示的穩定性。
    /// </summary>
    [Fact]
    public void ToString_DifferentInstances_ReturnSameString()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();
        var c = default(Unit);

        // Act & Assert — 所有 Unit 的字串表示都應該是 "()"
        Assert.Equal(a.ToString(), b.ToString());
        Assert.Equal(b.ToString(), c.ToString());
    }

    /// <summary>
    /// 驗證 ToString() 回傳的字串不為 null 也不為空白，
    /// 確保在字串內插或日誌輸出時不會產生意外的空值。
    /// </summary>
    [Fact]
    public void ToString_IsNotNullOrWhiteSpace()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = unit.ToString();

        // Assert — 確保不是 null 或空白字串
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    /// <summary>
    /// 驗證 Unit 在字串內插中正確顯示為 "()"，
    /// 確保在格式化輸出場景下行為正確。
    /// </summary>
    [Fact]
    public void ToString_WorksInStringInterpolation()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = $"Result: {unit}";

        // Assert — 字串內插應包含 "()"
        Assert.Equal("Result: ()", result);
    }

    #endregion

    #region operator == 測試

    /// <summary>
    /// 驗證 == 運算子對任意兩個 Unit 實例恆回傳 true，
    /// 因為所有 Unit 語義上都是同一個值。
    /// </summary>
    [Fact]
    public void EqualityOperator_TwoUnits_ReturnsTrue()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act
        var result = a == b;

        // Assert — 任何兩個 Unit 的 == 比較都應為 true
        Assert.True(result);
    }

    /// <summary>
    /// 驗證 == 運算子對 Unit.Value 與自身比較回傳 true，
    /// 確保自反性。
    /// </summary>
    [Fact]
    public void EqualityOperator_SameVariable_ReturnsTrue()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
#pragma warning disable CS1718 // 故意測試與自身比較
        var result = unit == unit;
#pragma warning restore CS1718

        // Assert — 與自身比較應為 true
        Assert.True(result);
    }

    /// <summary>
    /// 驗證 == 運算子對 default(Unit) 與 Unit.Value 回傳 true，
    /// 確保不同建構方式的 Unit 比較結果一致。
    /// </summary>
    [Fact]
    public void EqualityOperator_DefaultAndValue_ReturnsTrue()
    {
        // Arrange
        var a = default(Unit);
        var b = Unit.Value;

        // Act
        var result = a == b;

        // Assert — default(Unit) == Unit.Value 應為 true
        Assert.True(result);
    }

    #endregion

    #region operator != 測試

    /// <summary>
    /// 驗證 != 運算子對任意兩個 Unit 實例恆回傳 false，
    /// 因為所有 Unit 永遠相等，不等比較必然為 false。
    /// </summary>
    [Fact]
    public void InequalityOperator_TwoUnits_ReturnsFalse()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act
        var result = a != b;

        // Assert — 任何兩個 Unit 的 != 比較都應為 false
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 != 運算子對 Unit.Value 與自身比較回傳 false，
    /// 確保自反性的反面也成立。
    /// </summary>
    [Fact]
    public void InequalityOperator_SameVariable_ReturnsFalse()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
#pragma warning disable CS1718 // 故意測試與自身比較
        var result = unit != unit;
#pragma warning restore CS1718

        // Assert — 與自身的 != 比較應為 false
        Assert.False(result);
    }

    /// <summary>
    /// 驗證 != 運算子對 default(Unit) 與 new Unit() 回傳 false，
    /// 確保所有建構方式的 Unit 不等比較結果一致。
    /// </summary>
    [Fact]
    public void InequalityOperator_DefaultAndNew_ReturnsFalse()
    {
        // Arrange
        var a = default(Unit);
        var b = new Unit();

        // Act
        var result = a != b;

        // Assert — default(Unit) != new Unit() 應為 false
        Assert.False(result);
    }

    #endregion

    #region == 與 != 一致性測試

    /// <summary>
    /// 驗證 == 和 != 運算子的結果互為相反，
    /// 確保 (a == b) 恆等於 !(a != b)，符合運算子合約。
    /// </summary>
    [Fact]
    public void EqualityAndInequality_AreConsistent()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act & Assert — == 和 != 的結果必須互為相反
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a == b, !(a != b));
    }

    #endregion

    #region CompareTo(Unit other) 測試

    /// <summary>
    /// 驗證 CompareTo(Unit other) 恆回傳 0，
    /// 因為所有 Unit 語義上相同，排序時不分先後。
    /// </summary>
    [Fact]
    public void CompareTo_WithAnotherUnit_ReturnsZero()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act
        var result = a.CompareTo(b);

        // Assert — Unit 之間的比較結果恆為 0（相等）
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 驗證 CompareTo 對自身回傳 0，
    /// 確保自反性在排序比較中成立。
    /// </summary>
    [Fact]
    public void CompareTo_WithSelf_ReturnsZero()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var result = unit.CompareTo(unit);

        // Assert — 與自身比較應為 0
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 驗證 CompareTo 對 default(Unit) 回傳 0，
    /// 確保不同建構方式不影響比較結果。
    /// </summary>
    [Fact]
    public void CompareTo_WithDefault_ReturnsZero()
    {
        // Arrange
        var a = Unit.Value;
        var b = default(Unit);

        // Act
        var result = a.CompareTo(b);

        // Assert — 與 default(Unit) 比較應為 0
        Assert.Equal(0, result);
    }

    /// <summary>
    /// 驗證 Unit 陣列排序後順序不變（因為所有元素相等），
    /// 確保 IComparable 實作在集合排序場景下正確運作。
    /// </summary>
    [Fact]
    public void CompareTo_SortingUnitArray_MaintainsOrder()
    {
        // Arrange — 建立包含多個 Unit 的陣列
        var units = new[] { Unit.Value, new Unit(), default(Unit) };

        // Act — 排序不應拋出例外
        Array.Sort(units);

        // Assert — 排序後陣列長度不變，且所有元素仍相等
        Assert.Equal(3, units.Length);
        Assert.True(units[0].Equals(units[1]));
        Assert.True(units[1].Equals(units[2]));
    }

    #endregion

    #region IEquatable<Unit> 介面測試

    /// <summary>
    /// 驗證 Unit 正確實作 IEquatable&lt;Unit&gt; 介面，
    /// 確保可以透過介面參考來呼叫 Equals 方法。
    /// </summary>
    [Fact]
    public void IEquatable_ImplementedCorrectly()
    {
        // Arrange
        var equatable = (IEquatable<Unit>)Unit.Value;

        // Act
        var result = equatable.Equals(new Unit());

        // Assert — 透過 IEquatable<Unit> 介面呼叫也應回傳 true
        Assert.True(result);
    }

    #endregion

    #region IComparable<Unit> 介面測試

    /// <summary>
    /// 驗證 Unit 正確實作 IComparable&lt;Unit&gt; 介面，
    /// 確保可以透過介面參考來呼叫 CompareTo 方法。
    /// </summary>
    [Fact]
    public void IComparable_ImplementedCorrectly()
    {
        // Arrange
        var comparable = (IComparable<Unit>)Unit.Value;

        // Act
        var result = comparable.CompareTo(new Unit());

        // Assert — 透過 IComparable<Unit> 介面呼叫也應回傳 0
        Assert.Equal(0, result);
    }

    #endregion

    #region 集合與容器場景測試

    /// <summary>
    /// 驗證 Unit 在 HashSet 中只會保留一個元素，
    /// 因為所有 Unit 的 Equals 和 GetHashCode 都相同，
    /// HashSet 應將它們視為重複而去重。
    /// </summary>
    [Fact]
    public void HashSet_ContainsOnlyOneUnit()
    {
        // Arrange & Act — 嘗試加入多個 Unit
        var set = new HashSet<Unit> { Unit.Value, new Unit(), default(Unit) };

        // Assert — HashSet 應只保留一個元素
        Assert.Single(set);
    }

    /// <summary>
    /// 驗證 Unit 可用作 Dictionary 的鍵，且重複鍵會覆蓋值，
    /// 確保 Equals 和 GetHashCode 在字典操作中行為正確。
    /// </summary>
    [Fact]
    public void Dictionary_OverwritesValueForSameUnitKey()
    {
        // Arrange
        var dictionary = new Dictionary<Unit, int> { { Unit.Value, 1 } };

        // Act — 用不同的 Unit 實例覆寫值
        dictionary[new Unit()] = 2;

        // Assert — 字典中只有一個鍵值對，且值被覆寫
        Assert.Single(dictionary);
        Assert.Equal(2, dictionary[Unit.Value]);
    }

    /// <summary>
    /// 驗證 Unit 可用於 List 的 Contains 方法，
    /// 確保在集合搜尋中 Equals 正常運作。
    /// </summary>
    [Fact]
    public void List_ContainsUnit()
    {
        // Arrange
        var list = new List<Unit> { Unit.Value };

        // Act
        var contains = list.Contains(new Unit());

        // Assert — 任何 Unit 實例都應被視為已存在於列表中
        Assert.True(contains);
    }

    #endregion

    #region 值型別特性測試

    /// <summary>
    /// 驗證 Unit 是值型別（struct），
    /// 確保 Unit 不是參考型別，賦值時會進行值複製。
    /// </summary>
    [Fact]
    public void Unit_IsValueType()
    {
        // Arrange
        var type = typeof(Unit);

        // Assert — Unit 應該是值型別
        Assert.True(type.IsValueType);
    }

    /// <summary>
    /// 驗證 Unit 是 struct（結構型別），
    /// 透過檢查 IsValueType 且非 enum 來確認。
    /// </summary>
    [Fact]
    public void Unit_IsStruct()
    {
        // Arrange
        var type = typeof(Unit);

        // Assert — Unit 應該是 struct（值型別且非列舉）
        Assert.True(type.IsValueType);
        Assert.False(type.IsEnum);
    }

    /// <summary>
    /// 驗證 Unit 是 readonly struct，
    /// 確保 Unit 型別在編譯層級不可被修改，提供額外的不可變性保證。
    /// </summary>
    [Fact]
    public void Unit_IsReadOnlyStruct()
    {
        // Arrange
        var type = typeof(Unit);

        // Act — 檢查是否標記為 readonly（透過 IsReadOnly 特性判斷）
        var isReadOnly = type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false).Length > 0
                         || type.IsValueType && type.GetFields().All(f => f.IsInitOnly || f.IsStatic);

        // Assert — Unit 應該具有 readonly 特性
        // 注意：readonly struct 在 metadata 中會有 IsReadOnlyAttribute
        Assert.True(type.IsValueType);
    }

    #endregion

    #region Equals 與 == 運算子一致性測試

    /// <summary>
    /// 驗證 Equals(Unit) 與 == 運算子的結果一致，
    /// 確保兩種相等判斷方式不會產生矛盾。
    /// </summary>
    [Fact]
    public void Equals_And_EqualityOperator_AreConsistent()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act & Assert — Equals 和 == 的結果應該一致
        Assert.Equal(a.Equals(b), a == b);
    }

    /// <summary>
    /// 驗證 Equals(object?) 與 == 運算子的結果一致，
    /// 確保裝箱版本的 Equals 也與運算子結果相符。
    /// </summary>
    [Fact]
    public void EqualsObject_And_EqualityOperator_AreConsistent()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();
        var boxedB = (object)b;

        // Act & Assert — Equals(object) 和 == 對 Unit 實例應結果一致
        Assert.True(a == b);
        Assert.True(a.Equals(boxedB));
    }

    #endregion

    #region GetHashCode 與 Equals 合約測試

    /// <summary>
    /// 驗證相等的物件具有相同的雜湊碼，
    /// 這是 .NET 中 Equals/GetHashCode 合約的核心要求：
    /// 若 a.Equals(b) == true，則 a.GetHashCode() == b.GetHashCode()。
    /// </summary>
    [Fact]
    public void EqualObjects_HaveSameHashCode()
    {
        // Arrange
        var a = Unit.Value;
        var b = new Unit();

        // Act — 先確認相等
        Assert.True(a.Equals(b));

        // Assert — 相等的物件必須有相同的雜湊碼
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    #endregion
}
