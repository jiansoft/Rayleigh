using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

public class OptionTests
{
    #region Factory Methods / Construction

    /// <summary>
    /// 驗證 Some 工廠方法能正確建立包含值的 Option。
    /// 確保 IsSome 為 true 且 IsNone 為 false，這是 Option 最基礎的語意保證。
    /// </summary>
    [Fact]
    public void Some_WithValue_CreatesOptionWithValue()
    {
        var option = Option<int>.Some(42);

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
    }

    /// <summary>
    /// 驗證 Some 工廠方法可以正確包裝字串值。
    /// 確保對參考型別同樣能正確運作。
    /// </summary>
    [Fact]
    public void Some_WithStringValue_CreatesOptionWithValue()
    {
        var option = Option<string>.Some("hello");

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
    }

    /// <summary>
    /// 驗證 None 靜態屬性能正確建立空的 Option。
    /// 確保 IsSome 為 false 且 IsNone 為 true，這是 Option 無值狀態的語意保證。
    /// </summary>
    [Fact]
    public void None_CreatesEmptyOption()
    {
        var option = Option<int>.None;

        Assert.False(option.IsSome);
        Assert.True(option.IsNone);
    }

    /// <summary>
    /// 驗證使用預設建構子建立的 Option 為 None 狀態。
    /// 由於 struct 的預設值語意，必須確保預設建構子與 None 行為一致。
    /// </summary>
    [Fact]
    public void DefaultConstructor_CreatesNoneOption()
    {
        var option = new Option<int>();

        Assert.False(option.IsSome);
        Assert.True(option.IsNone);
    }

    /// <summary>
    /// 驗證將 null 傳入 Some 時會擲出 ArgumentNullException。
    /// 這是 Option 類型安全的核心保證：Some 狀態永遠不會包含 null 值。
    /// </summary>
    [Fact]
    public void Some_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null!));
    }

    /// <summary>
    /// 驗證透過輔助方法間接將 null 傳入 Some 時，同樣會擲出 ArgumentNullException。
    /// 確保即使不直接使用 null 字面值，防護機制依然有效。
    /// </summary>
    [Fact]
    public void Some_WithNullPassedViaHelper_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            string? value = null;
            _ = Option<string>.Some(value!);
        });
    }

    #endregion

    #region Contains

    /// <summary>
    /// 驗證 Contains 在 Some 狀態下且值相符時回傳 true。
    /// 確保值比較的正確性。
    /// </summary>
    [Fact]
    public void Contains_SomeWithMatchingValue_ReturnsTrue()
    {
        var option = Option<int>.Some(42);

        Assert.True(option.Contains(42));
    }

    /// <summary>
    /// 驗證 Contains 在 Some 狀態下但值不相符時回傳 false。
    /// 確保不會誤判不同的值為相等。
    /// </summary>
    [Fact]
    public void Contains_SomeWithDifferentValue_ReturnsFalse()
    {
        var option = Option<int>.Some(42);

        Assert.False(option.Contains(100));
    }

    /// <summary>
    /// 驗證 Contains 在 None 狀態下永遠回傳 false。
    /// 無值的 Option 不可能包含任何值。
    /// </summary>
    [Fact]
    public void Contains_None_ReturnsFalse()
    {
        var option = Option<int>.None;

        Assert.False(option.Contains(42));
    }

    /// <summary>
    /// 驗證 Contains 對字串值能正確比較。
    /// 確保參考型別使用值相等而非參考相等進行比較。
    /// </summary>
    [Fact]
    public void Contains_SomeStringWithMatchingValue_ReturnsTrue()
    {
        var option = Option<string>.Some("hello");

        Assert.True(option.Contains("hello"));
    }

    #endregion

    #region IsSomeAnd

    /// <summary>
    /// 驗證 IsSomeAnd 在 Some 狀態下且謂詞為 true 時回傳 true。
    /// 確保條件檢查在有值時能正確運作。
    /// </summary>
    [Fact]
    public void IsSomeAnd_SomeWithTruePredicate_ReturnsTrue()
    {
        var option = Option<int>.Some(42);

        Assert.True(option.IsSomeAnd(x => x > 40));
    }

    /// <summary>
    /// 驗證 IsSomeAnd 在 Some 狀態下但謂詞為 false 時回傳 false。
    /// 確保條件不符時不會誤報。
    /// </summary>
    [Fact]
    public void IsSomeAnd_SomeWithFalsePredicate_ReturnsFalse()
    {
        var option = Option<int>.Some(42);

        Assert.False(option.IsSomeAnd(x => x > 50));
    }

    /// <summary>
    /// 驗證 IsSomeAnd 在 None 狀態下永遠回傳 false，且不會執行謂詞。
    /// 無值時應直接回傳 false，避免對 null 值執行謂詞導致例外。
    /// </summary>
    [Fact]
    public void IsSomeAnd_None_ReturnsFalse()
    {
        var option = Option<int>.None;

        Assert.False(option.IsSomeAnd(x => x > 0));
    }

    #endregion

    #region Match (with return value)

    /// <summary>
    /// 驗證有回傳值的 Match 在 Some 狀態下執行 some 分支。
    /// Match 是 Option 最核心的模式比對方法，必須正確路由到對應分支。
    /// </summary>
    [Fact]
    public void Match_Some_ExecutesSomeBranch()
    {
        var option = Option<int>.Some(42);

        var result = option.Match(
            some: value => $"Value: {value}",
            none: () => "Empty"
        );

        Assert.Equal("Value: 42", result);
    }

    /// <summary>
    /// 驗證有回傳值的 Match 在 None 狀態下執行 none 分支。
    /// 確保無值時正確路由到 none 分支。
    /// </summary>
    [Fact]
    public void Match_None_ExecutesNoneBranch()
    {
        var option = Option<int>.None;

        var result = option.Match(
            some: value => $"Value: {value}",
            none: () => "Empty"
        );

        Assert.Equal("Empty", result);
    }

    #endregion

    #region Match (void / Action)

    /// <summary>
    /// 驗證無回傳值的 Match 在 Some 狀態下執行 some 動作。
    /// 確保副作用版本的 Match 同樣能正確路由。
    /// </summary>
    [Fact]
    public void MatchAction_Some_ExecutesSomeAction()
    {
        var option = Option<int>.Some(42);
        var executed = false;

        option.Match(
            some: _ => executed = true,
            none: () => { }
        );

        Assert.True(executed);
    }

    /// <summary>
    /// 驗證無回傳值的 Match 在 None 狀態下執行 none 動作。
    /// 確保副作用版本在無值時路由到 none 分支。
    /// </summary>
    [Fact]
    public void MatchAction_None_ExecutesNoneAction()
    {
        var option = Option<int>.None;
        var executed = false;

        option.Match(
            some: _ => { },
            none: () => executed = true
        );

        Assert.True(executed);
    }

    /// <summary>
    /// 驗證無回傳值的 Match 在 Some 狀態下不會執行 none 動作。
    /// 確保兩個分支互斥，只有一個會被執行。
    /// </summary>
    [Fact]
    public void MatchAction_Some_DoesNotExecuteNoneAction()
    {
        var option = Option<int>.Some(42);
        var noneExecuted = false;

        option.Match(
            some: _ => { },
            none: () => noneExecuted = true
        );

        Assert.False(noneExecuted);
    }

    #endregion

    #region Map

    /// <summary>
    /// 驗證 Map 在 Some 狀態下能正確轉換值。
    /// Map 是函數式組合的基礎，必須正確套用映射函數。
    /// </summary>
    [Fact]
    public void Map_Some_TransformsValue()
    {
        var option = Option<int>.Some(21);

        var result = option.Map(x => x * 2);

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Map 在 None 狀態下回傳 None，不會執行映射函數。
    /// None 應該透過所有轉換操作維持 None 狀態。
    /// </summary>
    [Fact]
    public void Map_None_ReturnsNone()
    {
        var option = Option<int>.None;

        var result = option.Map(x => x * 2);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Map 可以改變 Option 的內部型別。
    /// 確保型別轉換的正確性（int -> string）。
    /// </summary>
    [Fact]
    public void Map_Some_CanChangeType()
    {
        var option = Option<int>.Some(42);

        var result = option.Map(x => x.ToString());

        Assert.True(result.IsSome);
        Assert.Equal("42", result.Unwrap());
    }

    #endregion

    #region Filter

    /// <summary>
    /// 驗證 Filter 在 Some 狀態下且謂詞為 true 時保留值。
    /// 過濾通過時應回傳原始的 Some。
    /// </summary>
    [Fact]
    public void Filter_SomeWithTruePredicate_RetainsSome()
    {
        var option = Option<int>.Some(42);

        var result = option.Filter(x => x > 40);

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Filter 在 Some 狀態下但謂詞為 false 時回傳 None。
    /// 過濾失敗時應將 Some 轉為 None。
    /// </summary>
    [Fact]
    public void Filter_SomeWithFalsePredicate_ReturnsNone()
    {
        var option = Option<int>.Some(42);

        var result = option.Filter(x => x > 50);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Filter 在 None 狀態下直接回傳 None，不會執行謂詞。
    /// None 通過 Filter 後仍應為 None。
    /// </summary>
    [Fact]
    public void Filter_None_ReturnsNone()
    {
        var option = Option<int>.None;

        var result = option.Filter(x => x > 0);

        Assert.True(result.IsNone);
    }

    #endregion

    #region Bind

    /// <summary>
    /// 驗證 Bind 在 Some 狀態下能串接另一個返回 Some 的操作。
    /// Bind 是 monadic 組合的核心，必須正確展平巢狀 Option。
    /// </summary>
    [Fact]
    public void Bind_SomeToSome_ReturnsSome()
    {
        var option = Option<int>.Some(42);

        var result = option.Bind(x => Option<string>.Some(x.ToString()));

        Assert.True(result.IsSome);
        Assert.Equal("42", result.Unwrap());
    }

    /// <summary>
    /// 驗證 Bind 在 Some 狀態下串接返回 None 的操作時，最終回傳 None。
    /// 鏈式操作中任一步驟失敗應導致整體結果為 None。
    /// </summary>
    [Fact]
    public void Bind_SomeToNone_ReturnsNone()
    {
        var option = Option<int>.Some(42);

        var result = option.Bind(_ => Option<string>.None);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Bind 在 None 狀態下直接回傳 None，不會執行 binder 函數。
    /// None 應短路所有後續的 Bind 操作。
    /// </summary>
    [Fact]
    public void Bind_None_ReturnsNone()
    {
        var option = Option<int>.None;
        var binderCalled = false;

        var result = option.Bind(x =>
        {
            binderCalled = true;
            return Option<string>.Some(x.ToString());
        });

        Assert.True(result.IsNone);
        Assert.False(binderCalled);
    }

    #endregion

    #region Zip

    /// <summary>
    /// 驗證 Zip 在兩個 Some 狀態下回傳包含元組的 Some。
    /// 只有當兩者都有值時才能成功組合。
    /// </summary>
    [Fact]
    public void Zip_BothSome_ReturnsSomeTuple()
    {
        var a = Option<int>.Some(1);
        var b = Option<string>.Some("hello");

        var result = a.Zip(b);

        Assert.True(result.IsSome);
        Assert.Equal((1, "hello"), result.Unwrap());
    }

    /// <summary>
    /// 驗證 Zip 在第一個為 None 時回傳 None。
    /// 任一方為 None 則組合失敗。
    /// </summary>
    [Fact]
    public void Zip_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<string>.Some("hello");

        var result = a.Zip(b);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Zip 在第二個為 None 時回傳 None。
    /// 任一方為 None 則組合失敗。
    /// </summary>
    [Fact]
    public void Zip_SecondNone_ReturnsNone()
    {
        var a = Option<int>.Some(1);
        var b = Option<string>.None;

        var result = a.Zip(b);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Zip 在兩個都為 None 時回傳 None。
    /// </summary>
    [Fact]
    public void Zip_BothNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<string>.None;

        var result = a.Zip(b);

        Assert.True(result.IsNone);
    }

    #endregion

    #region ZipWith

    /// <summary>
    /// 驗證 ZipWith 在兩個 Some 狀態下使用 zipper 函數計算結果。
    /// 確保兩個值能透過自訂函數正確組合。
    /// </summary>
    [Fact]
    public void ZipWith_BothSome_AppliesZipper()
    {
        var width = Option<int>.Some(10);
        var height = Option<int>.Some(20);

        var area = width.ZipWith(height, (w, h) => w * h);

        Assert.True(area.IsSome);
        Assert.Equal(200, area.Unwrap());
    }

    /// <summary>
    /// 驗證 ZipWith 在任一方為 None 時回傳 None。
    /// 組合操作需要兩個值都存在才能進行。
    /// </summary>
    [Fact]
    public void ZipWith_OneNone_ReturnsNone()
    {
        var width = Option<int>.Some(10);
        var height = Option<int>.None;

        var area = width.ZipWith(height, (w, h) => w * h);

        Assert.True(area.IsNone);
    }

    /// <summary>
    /// 驗證 ZipWith 可以產生不同型別的結果。
    /// 確保型別轉換在組合操作中正確運作。
    /// </summary>
    [Fact]
    public void ZipWith_BothSome_CanChangeResultType()
    {
        var first = Option<string>.Some("John");
        var last = Option<string>.Some("Doe");

        var fullName = first.ZipWith(last, (f, l) => $"{f} {l}");

        Assert.True(fullName.IsSome);
        Assert.Equal("John Doe", fullName.Unwrap());
    }

    #endregion

    #region Or

    /// <summary>
    /// 驗證 Or 在 Some 狀態下回傳自身，忽略替代值。
    /// 有值時不需要使用備用來源。
    /// </summary>
    [Fact]
    public void Or_Some_ReturnsSelf()
    {
        var option = Option<int>.Some(42);
        var other = Option<int>.Some(99);

        var result = option.Or(other);

        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Or 在 None 狀態下回傳替代的 Option。
    /// 無值時應使用備用來源。
    /// </summary>
    [Fact]
    public void Or_None_ReturnsOther()
    {
        var option = Option<int>.None;
        var other = Option<int>.Some(99);

        var result = option.Or(other);

        Assert.Equal(99, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Or 在兩者都為 None 時回傳 None。
    /// 當備用來源也無值時，結果仍為 None。
    /// </summary>
    [Fact]
    public void Or_BothNone_ReturnsNone()
    {
        var option = Option<int>.None;
        var other = Option<int>.None;

        var result = option.Or(other);

        Assert.True(result.IsNone);
    }

    #endregion

    #region OrElse

    /// <summary>
    /// 驗證 OrElse 在 Some 狀態下回傳自身，不會執行工廠函數。
    /// 有值時應短路，避免不必要的計算。
    /// </summary>
    [Fact]
    public void OrElse_Some_ReturnsSelfAndDoesNotCallFactory()
    {
        var option = Option<int>.Some(42);
        var factoryCalled = false;

        var result = option.OrElse(() =>
        {
            factoryCalled = true;
            return Option<int>.Some(99);
        });

        Assert.Equal(42, result.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 OrElse 在 None 狀態下執行工廠函數並回傳其結果。
    /// 無值時應呼叫惰性工廠函數取得替代值。
    /// </summary>
    [Fact]
    public void OrElse_None_CallsFactoryAndReturnsResult()
    {
        var option = Option<int>.None;

        var result = option.OrElse(() => Option<int>.Some(99));

        Assert.Equal(99, result.Unwrap());
    }

    #endregion

    #region Tap

    /// <summary>
    /// 驗證 Tap 在 Some 狀態下執行指定的動作。
    /// Tap 用於執行副作用（如日誌記錄），應在有值時觸發。
    /// </summary>
    [Fact]
    public void Tap_Some_ExecutesAction()
    {
        var option = Option<int>.Some(42);
        var capturedValue = 0;

        var result = option.Tap(v => capturedValue = v);

        Assert.Equal(42, capturedValue);
        Assert.Equal(option, result);
    }

    /// <summary>
    /// 驗證 Tap 在 None 狀態下不會執行動作。
    /// 無值時不應觸發副作用。
    /// </summary>
    [Fact]
    public void Tap_None_DoesNotExecuteAction()
    {
        var option = Option<int>.None;
        var executed = false;

        var result = option.Tap(_ => executed = true);

        Assert.False(executed);
        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Tap 回傳原始 Option，支援鏈式呼叫。
    /// Tap 不應改變 Option 的狀態或值。
    /// </summary>
    [Fact]
    public void Tap_Some_ReturnsOriginalOption()
    {
        var option = Option<int>.Some(42);

        var result = option.Tap(_ => { });

        Assert.Equal(42, result.Unwrap());
    }

    #endregion

    #region TryGetValue

    /// <summary>
    /// 驗證 TryGetValue 在 Some 狀態下回傳 true 並輸出值。
    /// 類似 TryParse 模式，是 C# 慣用的安全取值方式。
    /// </summary>
    [Fact]
    public void TryGetValue_Some_ReturnsTrueAndOutputsValue()
    {
        var option = Option<int>.Some(42);

        var success = option.TryGetValue(out var value);

        Assert.True(success);
        Assert.Equal(42, value);
    }

    /// <summary>
    /// 驗證 TryGetValue 在 None 狀態下回傳 false 並輸出 default。
    /// 無值時應回傳 false，讓呼叫端知道取值失敗。
    /// </summary>
    [Fact]
    public void TryGetValue_None_ReturnsFalseAndOutputsDefault()
    {
        var option = Option<int>.None;

        var success = option.TryGetValue(out var value);

        Assert.False(success);
        Assert.Equal(default, value);
    }

    #endregion

    #region Unwrap

    /// <summary>
    /// 驗證 Unwrap 在 Some 狀態下回傳包含的值。
    /// 確保有值時能直接取出。
    /// </summary>
    [Fact]
    public void Unwrap_Some_ReturnsValue()
    {
        var option = Option<int>.Some(42);

        Assert.Equal(42, option.Unwrap());
    }

    /// <summary>
    /// 驗證 Unwrap 在 None 狀態下擲出 InvalidOperationException。
    /// 無值時強制取值應視為程式錯誤，必須擲出例外。
    /// </summary>
    [Fact]
    public void Unwrap_None_ThrowsInvalidOperationException()
    {
        var option = Option<int>.None;

        Assert.Throws<InvalidOperationException>(() => option.Unwrap());
    }

    #endregion

    #region UnwrapOr

    /// <summary>
    /// 驗證 UnwrapOr 在 Some 狀態下回傳包含的值，忽略預設值。
    /// 有值時應優先使用自身的值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Some_ReturnsValue()
    {
        var option = Option<int>.Some(42);

        Assert.Equal(42, option.UnwrapOr(0));
    }

    /// <summary>
    /// 驗證 UnwrapOr 在 None 狀態下回傳指定的預設值。
    /// 無值時應使用提供的預設值作為替代。
    /// </summary>
    [Fact]
    public void UnwrapOr_None_ReturnsDefault()
    {
        var option = Option<int>.None;

        Assert.Equal(0, option.UnwrapOr(0));
    }

    #endregion

    #region UnwrapOrElse

    /// <summary>
    /// 驗證 UnwrapOrElse 在 Some 狀態下回傳包含的值，不執行工廠函數。
    /// 有值時應短路，避免不必要的計算。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Some_ReturnsValueAndDoesNotCallFactory()
    {
        var option = Option<int>.Some(42);
        var factoryCalled = false;

        var result = option.UnwrapOrElse(() =>
        {
            factoryCalled = true;
            return 0;
        });

        Assert.Equal(42, result);
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 UnwrapOrElse 在 None 狀態下執行工廠函數並回傳其結果。
    /// 無值時應透過惰性計算取得替代值。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_None_CallsFactoryAndReturnsResult()
    {
        var option = Option<int>.None;

        var result = option.UnwrapOrElse(() => 99);

        Assert.Equal(99, result);
    }

    #endregion

    #region Expect

    /// <summary>
    /// 驗證 Expect 在 Some 狀態下回傳包含的值。
    /// 確保有值時行為等同於 Unwrap。
    /// </summary>
    [Fact]
    public void Expect_Some_ReturnsValue()
    {
        var option = Option<int>.Some(42);

        Assert.Equal(42, option.Expect("should have value"));
    }

    /// <summary>
    /// 驗證 Expect 在 None 狀態下擲出包含自訂訊息的 InvalidOperationException。
    /// 自訂訊息能幫助除錯，快速定位問題。
    /// </summary>
    [Fact]
    public void Expect_None_ThrowsInvalidOperationExceptionWithMessage()
    {
        var option = Option<int>.None;

        var ex = Assert.Throws<InvalidOperationException>(() => option.Expect("value is required"));

        Assert.Equal("value is required", ex.Message);
    }

    #endregion

    #region ToResult

    /// <summary>
    /// 驗證 ToResult 在 Some 狀態下轉換為 Ok Result。
    /// Option 有值時應成功轉換為 Result 的成功狀態。
    /// </summary>
    [Fact]
    public void ToResult_Some_ReturnsOkResult()
    {
        var option = Option<int>.Some(42);

        var result = option.ToResult("error");

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 ToResult 在 None 狀態下轉換為 Err Result，並使用提供的錯誤值。
    /// Option 無值時應轉換為 Result 的錯誤狀態。
    /// </summary>
    [Fact]
    public void ToResult_None_ReturnsErrResult()
    {
        var option = Option<int>.None;

        var result = option.ToResult("not found");

        Assert.True(result.IsErr);
        Assert.Equal("not found", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證使用工廠函數的 ToResult 在 Some 狀態下不會呼叫工廠函數。
    /// 有值時應短路，避免不必要的錯誤物件建立。
    /// </summary>
    [Fact]
    public void ToResult_SomeWithFactory_ReturnsOkAndDoesNotCallFactory()
    {
        var option = Option<int>.Some(42);
        var factoryCalled = false;

        var result = option.ToResult(() =>
        {
            factoryCalled = true;
            return "error";
        });

        Assert.True(result.IsOk);
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證使用工廠函數的 ToResult 在 None 狀態下執行工廠函數並回傳 Err Result。
    /// 無值時應透過惰性工廠函數產生錯誤物件。
    /// </summary>
    [Fact]
    public void ToResult_NoneWithFactory_CallsFactoryAndReturnsErr()
    {
        var option = Option<int>.None;

        var result = option.ToResult(() => "generated error");

        Assert.True(result.IsErr);
        Assert.Equal("generated error", result.UnwrapErr());
    }

    #endregion

    #region LINQ - Select

    /// <summary>
    /// 驗證 LINQ Select 在 Some 狀態下能正確轉換值。
    /// Select 是 Map 的 LINQ 版本，支援查詢語法。
    /// </summary>
    [Fact]
    public void Select_Some_TransformsValue()
    {
        var option = Option<int>.Some(21);

        var result = option.Select(x => x * 2);

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ Select 在 None 狀態下回傳 None。
    /// 確保 LINQ 語意在 None 時正確傳遞。
    /// </summary>
    [Fact]
    public void Select_None_ReturnsNone()
    {
        var option = Option<int>.None;

        var result = option.Select(x => x * 2);

        Assert.True(result.IsNone);
    }

    #endregion

    #region LINQ - SelectMany

    /// <summary>
    /// 驗證 LINQ SelectMany 在兩個 Some 狀態下能正確組合值。
    /// SelectMany 支援 LINQ 的多重 from 語法。
    /// </summary>
    [Fact]
    public void SelectMany_BothSome_CombinesValues()
    {
        var a = Option<int>.Some(10);

        var result = a.SelectMany(
            x => Option<int>.Some(x + 5),
            (x, y) => x + y
        );

        Assert.True(result.IsSome);
        Assert.Equal(25, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ SelectMany 在第一個為 None 時回傳 None。
    /// 來源為 None 時應短路整個查詢。
    /// </summary>
    [Fact]
    public void SelectMany_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;

        var result = a.SelectMany(
            x => Option<int>.Some(x + 5),
            (x, y) => x + y
        );

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ SelectMany 在中間步驟回傳 None 時，最終回傳 None。
    /// 鏈式操作中任何步驟失敗都應中斷。
    /// </summary>
    [Fact]
    public void SelectMany_IntermediateNone_ReturnsNone()
    {
        var a = Option<int>.Some(10);

        var result = a.SelectMany(
            _ => Option<int>.None,
            (x, y) => x + y
        );

        Assert.True(result.IsNone);
    }

    #endregion

    #region LINQ - Where

    /// <summary>
    /// 驗證 LINQ Where 在 Some 狀態下且條件成立時保留值。
    /// Where 是 Filter 的 LINQ 版本。
    /// </summary>
    [Fact]
    public void Where_SomeWithTruePredicate_RetainsSome()
    {
        var option = Option<int>.Some(42);

        var result = option.Where(x => x > 40);

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ Where 在 Some 狀態下但條件不成立時回傳 None。
    /// 過濾失敗時應轉為 None。
    /// </summary>
    [Fact]
    public void Where_SomeWithFalsePredicate_ReturnsNone()
    {
        var option = Option<int>.Some(42);

        var result = option.Where(x => x > 50);

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ Where 在 None 狀態下直接回傳 None。
    /// None 通過 Where 後仍為 None。
    /// </summary>
    [Fact]
    public void Where_None_ReturnsNone()
    {
        var option = Option<int>.None;

        var result = option.Where(x => x > 0);

        Assert.True(result.IsNone);
    }

    #endregion

    #region LINQ Query Syntax

    /// <summary>
    /// 驗證 LINQ 查詢語法的 select 能正確轉換 Some 中的值。
    /// 確保 from...select 語法與方法呼叫語法行為一致。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_Select_Some_TransformsValue()
    {
        var option = Option<int>.Some(21);

        var result = from x in option
                     select x * 2;

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的 select 在 None 時回傳 None。
    /// 確保查詢語法在無值時正確傳遞 None 語意。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_Select_None_ReturnsNone()
    {
        var option = Option<int>.None;

        var result = from x in option
                     select x * 2;

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的 where 子句能正確過濾值。
    /// 確保 from...where...select 語法完整運作。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_Where_FiltersCorrectly()
    {
        var option = Option<int>.Some(42);

        var result = from x in option
                     where x > 40
                     select x;

        Assert.True(result.IsSome);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的 where 子句在條件不成立時回傳 None。
    /// 確保過濾失敗時行為正確。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_Where_FilterFails_ReturnsNone()
    {
        var option = Option<int>.Some(42);

        var result = from x in option
                     where x > 100
                     select x;

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 能正確串接兩個 Option。
    /// 這是 SelectMany 的查詢語法形式，測試 monadic 組合。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_BothSome_CombinesValues()
    {
        var a = Option<int>.Some(10);
        var b = Option<int>.Some(20);

        var result = from x in a
                     from y in b
                     select x + y;

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 在第一個為 None 時回傳 None。
    /// 確保短路語意在查詢語法中正確運作。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_FirstNone_ReturnsNone()
    {
        var a = Option<int>.None;
        var b = Option<int>.Some(20);

        var result = from x in a
                     from y in b
                     select x + y;

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 在第二個為 None 時回傳 None。
    /// 確保鏈式查詢中後續步驟的失敗也能正確傳遞。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_SecondNone_ReturnsNone()
    {
        var a = Option<int>.Some(10);
        var b = Option<int>.None;

        var result = from x in a
                     from y in b
                     select x + y;

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法支援依賴前一步結果的多重 from。
    /// 第二個 from 的來源依賴第一個的值，模擬真實業務場景（如查詢使用者後查詢地址）。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_DependentMultipleFrom_Works()
    {
        static Option<string> GetName(int id) =>
            id == 1 ? Option<string>.Some("Alice") : Option<string>.None;

        static Option<int> GetAge(string name) =>
            name == "Alice" ? Option<int>.Some(30) : Option<int>.None;

        var id = Option<int>.Some(1);

        var result = from i in id
                     from name in GetName(i)
                     from age in GetAge(name)
                     select $"{name} is {age}";

        Assert.True(result.IsSome);
        Assert.Equal("Alice is 30", result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法支援 where 與多重 from 的組合。
    /// 結合過濾與串接操作，確保複雜查詢的正確性。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_WhereWithMultipleFrom_Works()
    {
        var a = Option<int>.Some(10);
        var b = Option<int>.Some(20);

        var result = from x in a
                     where x > 5
                     from y in b
                     select x + y;

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法中 where 過濾失敗時，後續 from 不會執行。
    /// 過濾失敗應短路整個查詢鏈。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_WhereFailsBeforeFrom_ReturnsNone()
    {
        var a = Option<int>.Some(3);
        var b = Option<int>.Some(20);

        var result = from x in a
                     where x > 5
                     from y in b
                     select x + y;

        Assert.True(result.IsNone);
    }

    #endregion

    #region Deconstruct

    /// <summary>
    /// 驗證 Deconstruct 在 Some 狀態下正確解構為 (true, value)。
    /// 解構語法讓 Option 可以用元組形式取值。
    /// </summary>
    [Fact]
    public void Deconstruct_Some_ReturnsTrueAndValue()
    {
        var option = Option<int>.Some(42);

        var (isSome, value) = option;

        Assert.True(isSome);
        Assert.Equal(42, value);
    }

    /// <summary>
    /// 驗證 Deconstruct 在 None 狀態下正確解構為 (false, default)。
    /// 確保無值時解構結果的一致性。
    /// </summary>
    [Fact]
    public void Deconstruct_None_ReturnsFalseAndDefault()
    {
        var option = Option<int>.None;

        var (isSome, value) = option;

        Assert.False(isSome);
        Assert.Equal(default, value);
    }

    /// <summary>
    /// 驗證 Deconstruct 搭配 switch 模式比對能正確處理 Some 狀態。
    /// 確保 Option 可以直接用於 C# switch 表達式。
    /// </summary>
    [Fact]
    public void Deconstruct_SwitchPatternMatching_Some()
    {
        var option = Option<int>.Some(42);

        var result = option switch
        {
            (true, var v) => $"Value: {v}",
            (false, _) => "None"
        };

        Assert.Equal("Value: 42", result);
    }

    /// <summary>
    /// 驗證 Deconstruct 搭配 switch 模式比對能正確處理 None 狀態。
    /// 確保 None 在 switch 表達式中能被正確匹配。
    /// </summary>
    [Fact]
    public void Deconstruct_SwitchPatternMatching_None()
    {
        var option = Option<int>.None;

        var result = option switch
        {
            (true, var v) => $"Value: {v}",
            (false, _) => "None"
        };

        Assert.Equal("None", result);
    }

    /// <summary>
    /// 驗證 Deconstruct 搭配 is 模式比對能正確匹配 Some 狀態。
    /// 確保 Option 可以在 if 語句的模式比對中使用。
    /// </summary>
    [Fact]
    public void Deconstruct_IsPatternMatching_Some()
    {
        var option = Option<string>.Some("hello");

        if (option is (true, var value))
        {
            Assert.Equal("hello", value);
        }
        else
        {
            Assert.Fail("Expected Some pattern to match");
        }
    }

    /// <summary>
    /// 驗證 Deconstruct 搭配 is 模式比對在 None 狀態下不會匹配 Some 模式。
    /// 確保模式比對的正確性。
    /// </summary>
    [Fact]
    public void Deconstruct_IsPatternMatching_None()
    {
        var option = Option<string>.None;

        if (option is (true, _))
        {
            Assert.Fail("Expected None, but matched Some");
        }

        Assert.True(true);
    }

    /// <summary>
    /// 驗證多個 Option 使用 switch 解構時能正確分辨不同狀態組合。
    /// 模擬實際場景中需要同時判斷多個 Option 的情境。
    /// </summary>
    [Fact]
    public void Deconstruct_MultipleOptions_SwitchExpression()
    {
        var name = Option<string>.Some("Alice");
        var age = Option<int>.Some(30);

        var result = (name, age) switch
        {
            ({ IsSome: true } n, { IsSome: true } a) => $"{n.Unwrap()} is {a.Unwrap()}",
            ({ IsSome: true } n, _) => $"{n.Unwrap()} has unknown age",
            (_, { IsSome: true } a) => $"Unknown person is {a.Unwrap()}",
            _ => "Unknown"
        };

        Assert.Equal("Alice is 30", result);
    }

    #endregion

    #region MapOr

    /// <summary>
    /// 驗證 MapOr 在 Some 狀態下套用映射函數。
    /// 有值時應使用映射函數轉換值。
    /// </summary>
    [Fact]
    public void MapOr_Some_AppliesMapper()
    {
        var option = Option<int>.Some(21);

        var result = option.MapOr(0, x => x * 2);

        Assert.Equal(42, result);
    }

    /// <summary>
    /// 驗證 MapOr 在 None 狀態下回傳預設值。
    /// 無值時應直接回傳提供的預設值。
    /// </summary>
    [Fact]
    public void MapOr_None_ReturnsDefault()
    {
        var option = Option<int>.None;

        var result = option.MapOr(0, x => x * 2);

        Assert.Equal(0, result);
    }

    /// <summary>
    /// 驗證 MapOr 可以轉換為不同的型別。
    /// 確保映射函數的型別轉換正確。
    /// </summary>
    [Fact]
    public void MapOr_Some_CanChangeType()
    {
        var option = Option<int>.Some(42);

        var result = option.MapOr("none", x => $"value: {x}");

        Assert.Equal("value: 42", result);
    }

    #endregion

    #region MapOrElse

    /// <summary>
    /// 驗證 MapOrElse 在 Some 狀態下套用映射函數，不執行預設工廠。
    /// 有值時應使用映射函數，避免不必要的預設值計算。
    /// </summary>
    [Fact]
    public void MapOrElse_Some_AppliesMapper()
    {
        var option = Option<int>.Some(21);
        var defaultCalled = false;

        var result = option.MapOrElse(
            () =>
            {
                defaultCalled = true;
                return 0;
            },
            x => x * 2
        );

        Assert.Equal(42, result);
        Assert.False(defaultCalled);
    }

    /// <summary>
    /// 驗證 MapOrElse 在 None 狀態下執行預設工廠函數。
    /// 無值時應透過惰性工廠函數計算預設值。
    /// </summary>
    [Fact]
    public void MapOrElse_None_CallsDefaultFactory()
    {
        var option = Option<int>.None;

        var result = option.MapOrElse(() => 99, x => x * 2);

        Assert.Equal(99, result);
    }

    #endregion

    #region Equality

    /// <summary>
    /// 驗證兩個包含相同值的 Some 被視為相等。
    /// 值相等是 Option 用於集合和比較操作的基礎。
    /// </summary>
    [Fact]
    public void Equals_BothSomeWithSameValue_ReturnsTrue()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// 驗證兩個包含不同值的 Some 不相等。
    /// 值不同時即使都是 Some 也應該不相等。
    /// </summary>
    [Fact]
    public void Equals_BothSomeWithDifferentValues_ReturnsFalse()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(99);

        Assert.False(a.Equals(b));
    }

    /// <summary>
    /// 驗證兩個 None 被視為相等。
    /// 所有 None 實例應該是等價的。
    /// </summary>
    [Fact]
    public void Equals_BothNone_ReturnsTrue()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// 驗證 Some 和 None 不相等。
    /// 不同狀態的 Option 永遠不相等。
    /// </summary>
    [Fact]
    public void Equals_SomeAndNone_ReturnsFalse()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.None;

        Assert.False(a.Equals(b));
        Assert.False(b.Equals(a));
    }

    /// <summary>
    /// 驗證 Equals(object?) 方法對裝箱的 Option 正確運作。
    /// 確保 override 的 Equals 方法能正確比較。
    /// </summary>
    [Fact]
    public void EqualsObject_WithBoxedOption_ReturnsCorrectResult()
    {
        var a = Option<int>.Some(42);
        var b = (object)Option<int>.Some(42);
        var c = (object)Option<int>.Some(99);
        var d = (object)"not an option";

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals(d));
    }

    /// <summary>
    /// 驗證 Equals(object?) 方法對 null 回傳 false。
    /// 確保不會因為 null 參數而擲出例外。
    /// </summary>
    [Fact]
    public void EqualsObject_WithNull_ReturnsFalse()
    {
        var option = Option<int>.Some(42);

        Assert.False(option.Equals(null));
    }

    #endregion

    #region GetHashCode

    /// <summary>
    /// 驗證兩個相等的 Some 具有相同的 HashCode。
    /// 這是 GetHashCode 合約的基本要求：相等的物件必須有相同的雜湊值。
    /// </summary>
    [Fact]
    public void GetHashCode_EqualSomeOptions_ReturnSameHashCode()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// 驗證兩個 None 具有相同的 HashCode。
    /// 所有 None 實例的雜湊值應該一致。
    /// </summary>
    [Fact]
    public void GetHashCode_BothNone_ReturnSameHashCode()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// 驗證不同值的 Some 通常具有不同的 HashCode。
    /// 雖然雜湊碰撞理論上可能發生，但對常見值應該產生不同的雜湊值。
    /// </summary>
    [Fact]
    public void GetHashCode_DifferentSomeValues_ReturnDifferentHashCodes()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(99);

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// 驗證 Some 和 None 具有不同的 HashCode。
    /// 不同狀態的 Option 應該產生不同的雜湊值。
    /// </summary>
    [Fact]
    public void GetHashCode_SomeAndNone_ReturnDifferentHashCodes()
    {
        var some = Option<int>.Some(0);
        var none = Option<int>.None;

        Assert.NotEqual(some.GetHashCode(), none.GetHashCode());
    }

    #endregion

    #region ToString

    /// <summary>
    /// 驗證 Some 的 ToString 回傳 "Some(value)" 格式。
    /// 清楚的字串表示有助於除錯和日誌記錄。
    /// </summary>
    [Fact]
    public void ToString_Some_ReturnsSomeFormat()
    {
        var option = Option<int>.Some(42);

        Assert.Equal("Some(42)", option.ToString());
    }

    /// <summary>
    /// 驗證字串值的 Some 的 ToString 回傳正確格式。
    /// 確保字串值不會被額外包裝引號。
    /// </summary>
    [Fact]
    public void ToString_SomeString_ReturnsSomeFormat()
    {
        var option = Option<string>.Some("hello");

        Assert.Equal("Some(hello)", option.ToString());
    }

    /// <summary>
    /// 驗證 None 的 ToString 回傳 "None"。
    /// 確保無值狀態的字串表示是明確的。
    /// </summary>
    [Fact]
    public void ToString_None_ReturnsNone()
    {
        var option = Option<int>.None;

        Assert.Equal("None", option.ToString());
    }

    #endregion

    #region Operators == / !=

    /// <summary>
    /// 驗證 == 運算子對相等的 Some 回傳 true。
    /// 確保運算子重載與 Equals 方法行為一致。
    /// </summary>
    [Fact]
    public void EqualityOperator_EqualSome_ReturnsTrue()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);

        Assert.True(a == b);
    }

    /// <summary>
    /// 驗證 == 運算子對不等的 Option 回傳 false。
    /// 確保不等時運算子行為正確。
    /// </summary>
    [Fact]
    public void EqualityOperator_DifferentOptions_ReturnsFalse()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(99);

        Assert.False(a == b);
    }

    /// <summary>
    /// 驗證 != 運算子對不等的 Option 回傳 true。
    /// 確保 != 運算子是 == 的邏輯反轉。
    /// </summary>
    [Fact]
    public void InequalityOperator_DifferentOptions_ReturnsTrue()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.None;

        Assert.True(a != b);
    }

    /// <summary>
    /// 驗證 != 運算子對相等的 Option 回傳 false。
    /// 確保 != 與 == 互斥。
    /// </summary>
    [Fact]
    public void InequalityOperator_EqualOptions_ReturnsFalse()
    {
        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);

        Assert.False(a != b);
    }

    /// <summary>
    /// 驗證兩個 None 的 == 比較回傳 true。
    /// None == None 應為 true。
    /// </summary>
    [Fact]
    public void EqualityOperator_BothNone_ReturnsTrue()
    {
        var a = Option<int>.None;
        var b = Option<int>.None;

        Assert.True(a == b);
    }

    #endregion

    #region Chaining

    /// <summary>
    /// 驗證 Map -> Bind -> Filter 的鏈式操作在所有步驟都成功時回傳正確結果。
    /// 鏈式操作是函數式程式設計的核心模式，必須確保各種操作能正確組合。
    /// </summary>
    [Fact]
    public void Chaining_MapThenBindThenFilter_AllSucceed()
    {
        var option = Option<int>.Some(5);

        var result = option
            .Map(x => x * 2)
            .Bind(x => x > 5 ? Option<string>.Some($"big: {x}") : Option<string>.None)
            .Filter(s => s.StartsWith("big", StringComparison.Ordinal));

        Assert.True(result.IsSome);
        Assert.Equal("big: 10", result.Unwrap());
    }

    /// <summary>
    /// 驗證鏈式操作中某一步驟失敗時，後續步驟不會執行。
    /// 短路語意確保效能並避免在 None 上執行不必要的操作。
    /// </summary>
    [Fact]
    public void Chaining_BindReturnsNone_ShortCircuits()
    {
        var option = Option<int>.Some(3);
        var filterCalled = false;

        var result = option
            .Map(x => x * 2)
            .Bind(x => x > 10 ? Option<string>.Some($"big: {x}") : Option<string>.None)
            .Filter(s =>
            {
                filterCalled = true;
                return s.StartsWith("big", StringComparison.Ordinal);
            });

        Assert.True(result.IsNone);
        Assert.False(filterCalled);
    }

    /// <summary>
    /// 驗證 Tap 在鏈式操作中不會改變 Option 的值或狀態。
    /// Tap 插入副作用後，後續操作應繼續使用原始值。
    /// </summary>
    [Fact]
    public void Chaining_TapDoesNotAlterChain()
    {
        var tappedValue = 0;
        var option = Option<int>.Some(10);

        var result = option
            .Tap(v => tappedValue = v)
            .Map(x => x * 3)
            .Filter(x => x > 20);

        Assert.True(result.IsSome);
        Assert.Equal(30, result.Unwrap());
        Assert.Equal(10, tappedValue);
    }

    /// <summary>
    /// 驗證複雜的鏈式操作：Map -> Filter -> Bind -> Map -> Tap -> ToResult。
    /// 模擬真實業務邏輯中多步驟的轉換流程。
    /// </summary>
    [Fact]
    public void Chaining_ComplexChain_ProducesCorrectResult()
    {
        var logged = new List<string>();

        var option = Option<int>.Some(42);

        var result = option
            .Map(x => x * 2)
            .Filter(x => x > 50)
            .Bind(x => Option<string>.Some($"result: {x}"))
            .Map(s => s.ToUpperInvariant())
            .Tap(s => logged.Add(s))
            .ToResult("failed");

        Assert.True(result.IsOk);
        Assert.Equal("RESULT: 84", result.Unwrap());
        Assert.Single(logged);
        Assert.Equal("RESULT: 84", logged[0]);
    }

    /// <summary>
    /// 驗證從 None 開始的鏈式操作，所有步驟都被短路。
    /// None 應該安全地通過所有操作而不會擲出例外。
    /// </summary>
    [Fact]
    public void Chaining_StartingFromNone_AllShortCircuited()
    {
        var mapCalled = false;
        var filterCalled = false;
        var bindCalled = false;
        var tapCalled = false;

        var result = Option<int>.None
            .Map(x =>
            {
                mapCalled = true;
                return x * 2;
            })
            .Filter(x =>
            {
                filterCalled = true;
                return x > 0;
            })
            .Bind(x =>
            {
                bindCalled = true;
                return Option<string>.Some(x.ToString());
            })
            .Tap(_ => tapCalled = true);

        Assert.True(result.IsNone);
        Assert.False(mapCalled);
        Assert.False(filterCalled);
        Assert.False(bindCalled);
        Assert.False(tapCalled);
    }

    /// <summary>
    /// 驗證 Or 和 OrElse 在鏈式操作中的恢復能力。
    /// 確保 None 狀態可以透過 Or/OrElse 恢復為 Some 並繼續後續操作。
    /// </summary>
    [Fact]
    public void Chaining_OrRecovery_Works()
    {
        var result = Option<int>.None
            .Or(Option<int>.None)
            .OrElse(() => Option<int>.Some(42))
            .Map(x => x * 2);

        Assert.True(result.IsSome);
        Assert.Equal(84, result.Unwrap());
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// 驗證 Some(0) 是有效的 Some 狀態，不會被視為 None。
    /// 零值（value type 的 default）應該被正確包裝為 Some。
    /// </summary>
    [Fact]
    public void Some_WithZero_IsSome()
    {
        var option = Option<int>.Some(0);

        Assert.True(option.IsSome);
        Assert.Equal(0, option.Unwrap());
    }

    /// <summary>
    /// 驗證 Some("") 是有效的 Some 狀態。
    /// 空字串不是 null，應該被正確包裝為 Some。
    /// </summary>
    [Fact]
    public void Some_WithEmptyString_IsSome()
    {
        var option = Option<string>.Some("");

        Assert.True(option.IsSome);
        Assert.Equal("", option.Unwrap());
    }

    /// <summary>
    /// 驗證 default(Option&lt;T&gt;) 等同於 None。
    /// struct 的 default 值語意必須與 None 一致，避免意外行為。
    /// </summary>
    [Fact]
    public void DefaultStruct_IsNone()
    {
        var option = default(Option<int>);

        Assert.True(option.IsNone);
        Assert.False(option.IsSome);
    }

    /// <summary>
    /// 驗證 Map 在 None 的連續調用中不會累積或產生意外狀態。
    /// 連續多次 Map 操作在 None 上應全部被忽略。
    /// </summary>
    [Fact]
    public void MultipleMapOnNone_RemainsNone()
    {
        var result = Option<int>.None
            .Map(x => x * 2)
            .Map(x => x + 1)
            .Map(x => x.ToString());

        Assert.True(result.IsNone);
    }

    /// <summary>
    /// 驗證 Filter 與 Contains 在邊界值（如 int.MaxValue）上正確運作。
    /// 確保沒有溢位或邊界條件問題。
    /// </summary>
    [Fact]
    public void EdgeCase_MaxValue_WorksCorrectly()
    {
        var option = Option<int>.Some(int.MaxValue);

        Assert.True(option.Contains(int.MaxValue));
        Assert.True(option.IsSomeAnd(x => x == int.MaxValue));
    }

    /// <summary>
    /// 驗證 Option 可以包裝 record 型別並正確比較。
    /// record 的值語意應該在 Option 中正確運作。
    /// </summary>
    [Fact]
    public void Some_WithRecord_EqualsCorrectly()
    {
        var a = Option<TestRecord>.Some(new TestRecord(1, "Alice"));
        var b = Option<TestRecord>.Some(new TestRecord(1, "Alice"));
        var c = Option<TestRecord>.Some(new TestRecord(2, "Bob"));

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    /// <summary>
    /// 驗證 Option 可以包裝元組型別。
    /// 元組的值語意應該在 Option 中正確運作。
    /// </summary>
    [Fact]
    public void Some_WithTuple_WorksCorrectly()
    {
        var option = Option<(int, string)>.Some((42, "hello"));

        Assert.True(option.IsSome);
        Assert.Equal((42, "hello"), option.Unwrap());
    }

    /// <summary>
    /// 驗證 UnwrapOr 使用字串預設值在 None 時回傳正確值。
    /// 確保參考型別的預設值處理正確。
    /// </summary>
    [Fact]
    public void UnwrapOr_NoneWithStringDefault_ReturnsDefault()
    {
        var option = Option<string>.None;

        var result = option.UnwrapOr("default");

        Assert.Equal("default", result);
    }

    /// <summary>
    /// 驗證 None 上的 Unwrap 擲出的例外包含描述性訊息。
    /// 有意義的例外訊息有助於快速除錯。
    /// </summary>
    [Fact]
    public void Unwrap_None_ExceptionMessageIsDescriptive()
    {
        var option = Option<int>.None;

        var ex = Assert.Throws<InvalidOperationException>(() => option.Unwrap());

        Assert.Contains("None", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 驗證 Map 不會在 None 上執行映射函數，即使該函數會擲出例外。
    /// 確保 None 的短路行為不只是回傳 None，而是完全不執行函數。
    /// </summary>
    [Fact]
    public void Map_None_DoesNotExecuteMapperEvenIfItWouldThrow()
    {
        var option = Option<int>.None;

        var result = option.Map<int>(_ => throw new InvalidOperationException("should not be called"));

        Assert.True(result.IsNone);
    }

    #endregion

    #region Helper Types

    private record TestRecord(int Id, string Name);

    #endregion
}
