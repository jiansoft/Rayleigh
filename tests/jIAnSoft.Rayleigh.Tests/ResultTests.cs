using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// Contains behavioral tests for Result construction, success and error state handling, transformations, extraction, LINQ support, equality, comparison, and default-struct safeguards.
/// This class exists as the executable specification for railway-style Result flow: Ok carries a successful value, Err carries an expected failure, and uninitialized values must not silently behave like valid results.
/// Individual tests accept no caller-supplied input, return no values, and use xUnit assertions and expected exceptions to describe the required contract.
/// </summary>
public class ResultTests
{
    // ================================================================
    // Factory Methods: Ok / Err
    // ================================================================

    /// <summary>
    /// 驗證 Result.Ok 建立的結果為成功狀態，且 IsOk 為 true、IsErr 為 false。
    /// </summary>
    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        var result = Result<int, string>.Ok(42);

        Assert.True(result.IsOk);
        Assert.False(result.IsErr);
    }

    /// <summary>
    /// 驗證 Result.Err 建立的結果為失敗狀態，且 IsErr 為 true、IsOk 為 false。
    /// </summary>
    [Fact]
    public void Err_CreatesErrorResult()
    {
        var result = Result<int, string>.Err("something went wrong");

        Assert.True(result.IsErr);
        Assert.False(result.IsOk);
    }

    /// <summary>
    /// 驗證 Result.Err 傳入 null 時會拋出 ArgumentNullException。
    /// </summary>
    [Fact]
    public void Err_NullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Err(null!));
    }

    // ================================================================
    // Implicit Operators
    // ================================================================

    /// <summary>
    /// 驗證成功值 T 可以隱式轉換為 Result，轉換後為成功狀態。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromValue_CreatesOkResult()
    {
        Result<int, string> result = 42;

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證錯誤值 TE 可以隱式轉換為 Result，轉換後為失敗狀態。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromError_CreatesErrResult()
    {
        // 使用不同型別避免 T 和 TE 衝突
        Result<int, string> result = "error message";

        Assert.True(result.IsErr);
        Assert.Equal("error message", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Ok&lt;T&gt; 包裹記錄可以隱式轉換為 Result，轉換後為成功狀態。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromOkWrapper_CreatesOkResult()
    {
        Result<int, string> result = new Ok<int>(100);

        Assert.True(result.IsOk);
        Assert.Equal(100, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Err&lt;TE&gt; 包裹記錄可以隱式轉換為 Result，轉換後為失敗狀態。
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromErrWrapper_CreatesErrResult()
    {
        Result<int, string> result = new Err<string>("wrapped error");

        Assert.True(result.IsErr);
        Assert.Equal("wrapped error", result.UnwrapErr());
    }

    // ================================================================
    // Contains / ContainsErr
    // ================================================================

    /// <summary>
    /// 驗證 Contains 在成功狀態下，當值匹配時回傳 true。
    /// </summary>
    [Fact]
    public void Contains_OkWithMatchingValue_ReturnsTrue()
    {
        var result = Result<int, string>.Ok(42);

        Assert.True(result.Contains(42));
    }

    /// <summary>
    /// 驗證 Contains 在成功狀態下，當值不匹配時回傳 false。
    /// </summary>
    [Fact]
    public void Contains_OkWithNonMatchingValue_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(42);

        Assert.False(result.Contains(100));
    }

    /// <summary>
    /// 驗證 Contains 在失敗狀態下永遠回傳 false。
    /// </summary>
    [Fact]
    public void Contains_Err_ReturnsFalse()
    {
        var result = Result<int, string>.Err("error");

        Assert.False(result.Contains(42));
    }

    /// <summary>
    /// 驗證 ContainsErr 在失敗狀態下，當錯誤匹配時回傳 true。
    /// </summary>
    [Fact]
    public void ContainsErr_ErrWithMatchingError_ReturnsTrue()
    {
        var result = Result<int, string>.Err("not found");

        Assert.True(result.ContainsErr("not found"));
    }

    /// <summary>
    /// 驗證 ContainsErr 在失敗狀態下，當錯誤不匹配時回傳 false。
    /// </summary>
    [Fact]
    public void ContainsErr_ErrWithNonMatchingError_ReturnsFalse()
    {
        var result = Result<int, string>.Err("not found");

        Assert.False(result.ContainsErr("timeout"));
    }

    /// <summary>
    /// 驗證 ContainsErr 在成功狀態下永遠回傳 false。
    /// </summary>
    [Fact]
    public void ContainsErr_Ok_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(42);

        Assert.False(result.ContainsErr("error"));
    }

    // ================================================================
    // IsOkAnd / IsErrAnd
    // ================================================================

    /// <summary>
    /// 驗證 IsOkAnd 在成功狀態下，當條件成立時回傳 true。
    /// </summary>
    [Fact]
    public void IsOkAnd_OkWithTruePredicate_ReturnsTrue()
    {
        var result = Result<int, string>.Ok(42);

        Assert.True(result.IsOkAnd(x => x > 40));
    }

    /// <summary>
    /// 驗證 IsOkAnd 在成功狀態下，當條件不成立時回傳 false。
    /// </summary>
    [Fact]
    public void IsOkAnd_OkWithFalsePredicate_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(42);

        Assert.False(result.IsOkAnd(x => x > 50));
    }

    /// <summary>
    /// 驗證 IsOkAnd 在失敗狀態下永遠回傳 false，不會呼叫 predicate。
    /// </summary>
    [Fact]
    public void IsOkAnd_Err_ReturnsFalseWithoutCallingPredicate()
    {
        var result = Result<int, string>.Err("error");
        var predicateCalled = false;

        var actual = result.IsOkAnd(_ =>
        {
            predicateCalled = true;
            return true;
        });

        Assert.False(actual);
        Assert.False(predicateCalled);
    }

    /// <summary>
    /// 驗證 IsErrAnd 在失敗狀態下，當條件成立時回傳 true。
    /// </summary>
    [Fact]
    public void IsErrAnd_ErrWithTruePredicate_ReturnsTrue()
    {
        var result = Result<int, string>.Err("timeout");

        Assert.True(result.IsErrAnd(e => e.Contains("timeout")));
    }

    /// <summary>
    /// 驗證 IsErrAnd 在失敗狀態下，當條件不成立時回傳 false。
    /// </summary>
    [Fact]
    public void IsErrAnd_ErrWithFalsePredicate_ReturnsFalse()
    {
        var result = Result<int, string>.Err("timeout");

        Assert.False(result.IsErrAnd(e => e.Contains("not found")));
    }

    /// <summary>
    /// 驗證 IsErrAnd 在成功狀態下永遠回傳 false，不會呼叫 predicate。
    /// </summary>
    [Fact]
    public void IsErrAnd_Ok_ReturnsFalseWithoutCallingPredicate()
    {
        var result = Result<int, string>.Ok(42);
        var predicateCalled = false;

        var actual = result.IsErrAnd(_ =>
        {
            predicateCalled = true;
            return true;
        });

        Assert.False(actual);
        Assert.False(predicateCalled);
    }

    // ================================================================
    // Match (with return value)
    // ================================================================

    /// <summary>
    /// 驗證 Match 在成功狀態下執行 ok 分支並回傳對應結果。
    /// </summary>
    [Fact]
    public void Match_Ok_ExecutesOkBranch()
    {
        var result = Result<int, string>.Ok(42);

        var message = result.Match(
            ok: v => $"value={v}",
            err: e => $"error={e}"
        );

        Assert.Equal("value=42", message);
    }

    /// <summary>
    /// 驗證 Match 在失敗狀態下執行 err 分支並回傳對應結果。
    /// </summary>
    [Fact]
    public void Match_Err_ExecutesErrBranch()
    {
        var result = Result<int, string>.Err("fail");

        var message = result.Match(
            ok: v => $"value={v}",
            err: e => $"error={e}"
        );

        Assert.Equal("error=fail", message);
    }

    // ================================================================
    // Match (void / Action)
    // ================================================================

    /// <summary>
    /// 驗證無回傳值的 Match 在成功狀態下執行 ok 動作。
    /// </summary>
    [Fact]
    public void MatchAction_Ok_ExecutesOkAction()
    {
        var result = Result<int, string>.Ok(42);
        var okCalled = false;
        var errCalled = false;

        result.Match(
            ok: _ => okCalled = true,
            err: _ => errCalled = true
        );

        Assert.True(okCalled);
        Assert.False(errCalled);
    }

    /// <summary>
    /// 驗證無回傳值的 Match 在失敗狀態下執行 err 動作。
    /// </summary>
    [Fact]
    public void MatchAction_Err_ExecutesErrAction()
    {
        var result = Result<int, string>.Err("fail");
        var okCalled = false;
        var errCalled = false;

        result.Match(
            ok: _ => okCalled = true,
            err: _ => errCalled = true
        );

        Assert.False(okCalled);
        Assert.True(errCalled);
    }

    // ================================================================
    // Map
    // ================================================================

    /// <summary>
    /// 驗證 Map 在成功狀態下將值轉換為新類型。
    /// </summary>
    [Fact]
    public void Map_Ok_TransformsValue()
    {
        var result = Result<int, string>.Ok(21);

        var mapped = result.Map(x => x * 2);

        Assert.True(mapped.IsOk);
        Assert.Equal(42, mapped.Unwrap());
    }

    /// <summary>
    /// 驗證 Map 在失敗狀態下保留原始錯誤，不呼叫 mapper。
    /// </summary>
    [Fact]
    public void Map_Err_PreservesError()
    {
        var result = Result<int, string>.Err("fail");
        var mapperCalled = false;

        var mapped = result.Map(x =>
        {
            mapperCalled = true;
            return x * 2;
        });

        Assert.True(mapped.IsErr);
        Assert.Equal("fail", mapped.UnwrapErr());
        Assert.False(mapperCalled);
    }

    // ================================================================
    // MapErr
    // ================================================================

    /// <summary>
    /// 驗證 MapErr 在失敗狀態下將錯誤轉換為新類型。
    /// </summary>
    [Fact]
    public void MapErr_Err_TransformsError()
    {
        var result = Result<int, string>.Err("fail");

        var mapped = result.MapErr(e => $"[ERROR] {e}");

        Assert.True(mapped.IsErr);
        Assert.Equal("[ERROR] fail", mapped.UnwrapErr());
    }

    /// <summary>
    /// 驗證 MapErr 在成功狀態下保留原始值，不呼叫 mapper。
    /// </summary>
    [Fact]
    public void MapErr_Ok_PreservesValue()
    {
        var result = Result<int, string>.Ok(42);
        var mapperCalled = false;

        var mapped = result.MapErr(e =>
        {
            mapperCalled = true;
            return $"[ERROR] {e}";
        });

        Assert.True(mapped.IsOk);
        Assert.Equal(42, mapped.Unwrap());
        Assert.False(mapperCalled);
    }

    // ================================================================
    // Bind
    // ================================================================

    /// <summary>
    /// 驗證 Bind 在成功狀態下執行 binder 並回傳其結果（成功路徑）。
    /// </summary>
    [Fact]
    public void Bind_OkWithOkBinder_ReturnsBinderResult()
    {
        var result = Result<int, string>.Ok(10);

        var bound = result.Bind(x => Result<string, string>.Ok($"value={x}"));

        Assert.True(bound.IsOk);
        Assert.Equal("value=10", bound.Unwrap());
    }

    /// <summary>
    /// 驗證 Bind 在成功狀態下執行 binder 並回傳其結果（失敗路徑）。
    /// </summary>
    [Fact]
    public void Bind_OkWithErrBinder_ReturnsBinderError()
    {
        var result = Result<int, string>.Ok(-1);

        var bound = result.Bind(x =>
            x > 0
                ? Result<string, string>.Ok($"value={x}")
                : Result<string, string>.Err("must be positive")
        );

        Assert.True(bound.IsErr);
        Assert.Equal("must be positive", bound.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Bind 在失敗狀態下保留原始錯誤，不呼叫 binder。
    /// </summary>
    [Fact]
    public void Bind_Err_PreservesErrorWithoutCallingBinder()
    {
        var result = Result<int, string>.Err("original error");
        var binderCalled = false;

        var bound = result.Bind(x =>
        {
            binderCalled = true;
            return Result<string, string>.Ok($"value={x}");
        });

        Assert.True(bound.IsErr);
        Assert.Equal("original error", bound.UnwrapErr());
        Assert.False(binderCalled);
    }

    // ================================================================
    // Or
    // ================================================================

    /// <summary>
    /// 驗證 Or 在成功狀態下回傳自身，忽略替代值。
    /// </summary>
    [Fact]
    public void Or_Ok_ReturnsSelf()
    {
        var result = Result<int, string>.Ok(42);
        var other = Result<int, string>.Ok(999);

        var actual = result.Or(other);

        Assert.Equal(42, actual.Unwrap());
    }

    /// <summary>
    /// 驗證 Or 在失敗狀態下回傳替代的 Result。
    /// </summary>
    [Fact]
    public void Or_Err_ReturnsOther()
    {
        var result = Result<int, string>.Err("fail");
        var other = Result<int, string>.Ok(999);

        var actual = result.Or(other);

        Assert.Equal(999, actual.Unwrap());
    }

    // ================================================================
    // OrElse
    // ================================================================

    /// <summary>
    /// 驗證 OrElse 在成功狀態下回傳自身，不呼叫工廠函數。
    /// </summary>
    [Fact]
    public void OrElse_Ok_ReturnsSelfWithoutCallingFactory()
    {
        var result = Result<int, string>.Ok(42);
        var factoryCalled = false;

        var actual = result.OrElse(_ =>
        {
            factoryCalled = true;
            return Result<int, string>.Ok(999);
        });

        Assert.Equal(42, actual.Unwrap());
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 OrElse 在失敗狀態下呼叫工廠函數並回傳其結果。
    /// </summary>
    [Fact]
    public void OrElse_Err_CallsFactoryAndReturnsResult()
    {
        var result = Result<int, string>.Err("fail");

        var actual = result.OrElse(err => Result<int, string>.Ok(err.Length));

        Assert.True(actual.IsOk);
        Assert.Equal(4, actual.Unwrap());
    }

    // ================================================================
    // Tap
    // ================================================================

    /// <summary>
    /// 驗證 Tap 在成功狀態下執行副作用動作，並回傳原始 Result。
    /// </summary>
    [Fact]
    public void Tap_Ok_ExecutesActionAndReturnsSelf()
    {
        var result = Result<int, string>.Ok(42);
        var captured = 0;

        var returned = result.Tap(v => captured = v);

        Assert.Equal(42, captured);
        Assert.Equal(result, returned);
    }

    /// <summary>
    /// 驗證 Tap 在失敗狀態下不執行動作，並回傳原始 Result。
    /// </summary>
    [Fact]
    public void Tap_Err_DoesNotExecuteAction()
    {
        var result = Result<int, string>.Err("fail");
        var actionCalled = false;

        var returned = result.Tap(_ => actionCalled = true);

        Assert.False(actionCalled);
        Assert.Equal(result, returned);
    }

    // ================================================================
    // TapErr
    // ================================================================

    /// <summary>
    /// 驗證 TapErr 在失敗狀態下執行副作用動作，並回傳原始 Result。
    /// </summary>
    [Fact]
    public void TapErr_Err_ExecutesActionAndReturnsSelf()
    {
        var result = Result<int, string>.Err("fail");
        var captured = "";

        var returned = result.TapErr(e => captured = e);

        Assert.Equal("fail", captured);
        Assert.Equal(result, returned);
    }

    /// <summary>
    /// 驗證 TapErr 在成功狀態下不執行動作，並回傳原始 Result。
    /// </summary>
    [Fact]
    public void TapErr_Ok_DoesNotExecuteAction()
    {
        var result = Result<int, string>.Ok(42);
        var actionCalled = false;

        var returned = result.TapErr(_ => actionCalled = true);

        Assert.False(actionCalled);
        Assert.Equal(result, returned);
    }

    // ================================================================
    // TryGetOk (single out)
    // ================================================================

    /// <summary>
    /// 驗證 TryGetOk（單一 out 參數）在成功狀態下回傳 true 並輸出值。
    /// </summary>
    [Fact]
    public void TryGetOk_Ok_ReturnsTrueWithValue()
    {
        var result = Result<int, string>.Ok(42);

        var success = result.TryGetOk(out var value);

        Assert.True(success);
        Assert.Equal(42, value);
    }

    /// <summary>
    /// 驗證 TryGetOk（單一 out 參數）在失敗狀態下回傳 false 並輸出 default。
    /// </summary>
    [Fact]
    public void TryGetOk_Err_ReturnsFalseWithDefault()
    {
        var result = Result<int, string>.Err("fail");

        var success = result.TryGetOk(out var value);

        Assert.False(success);
        Assert.Equal(default, value);
    }

    // ================================================================
    // TryGetOk (double out)
    // ================================================================

    /// <summary>
    /// 驗證 TryGetOk（雙 out 參數）在成功狀態下回傳 true，輸出值並將 error 設為 default。
    /// </summary>
    [Fact]
    public void TryGetOk_DoubleOut_Ok_ReturnsTrueWithValueAndDefaultError()
    {
        var result = Result<int, string>.Ok(42);

        var success = result.TryGetOk(out var value, out var error);

        Assert.True(success);
        Assert.Equal(42, value);
        Assert.Null(error);
    }

    /// <summary>
    /// 驗證 TryGetOk（雙 out 參數）在失敗狀態下回傳 false，輸出錯誤並將 value 設為 default。
    /// </summary>
    [Fact]
    public void TryGetOk_DoubleOut_Err_ReturnsFalseWithErrorAndDefaultValue()
    {
        var result = Result<int, string>.Err("fail");

        var success = result.TryGetOk(out var value, out var error);

        Assert.False(success);
        Assert.Equal(default, value);
        Assert.Equal("fail", error);
    }

    // ================================================================
    // TryGetErr
    // ================================================================

    /// <summary>
    /// 驗證 TryGetErr 在失敗狀態下回傳 true 並輸出錯誤。
    /// </summary>
    [Fact]
    public void TryGetErr_Err_ReturnsTrueWithError()
    {
        var result = Result<int, string>.Err("fail");

        var hasError = result.TryGetErr(out var error);

        Assert.True(hasError);
        Assert.Equal("fail", error);
    }

    /// <summary>
    /// 驗證 TryGetErr 在成功狀態下回傳 false 並輸出 default。
    /// </summary>
    [Fact]
    public void TryGetErr_Ok_ReturnsFalseWithDefault()
    {
        var result = Result<int, string>.Ok(42);

        var hasError = result.TryGetErr(out var error);

        Assert.False(hasError);
        Assert.Null(error);
    }

    // ================================================================
    // Unwrap
    // ================================================================

    /// <summary>
    /// 驗證 Unwrap 在成功狀態下回傳值。
    /// </summary>
    [Fact]
    public void Unwrap_Ok_ReturnsValue()
    {
        var result = Result<int, string>.Ok(42);

        Assert.Equal(42, result.Unwrap());
    }

    /// <summary>
    /// 驗證 Unwrap 在失敗狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Unwrap_Err_ThrowsInvalidOperationException()
    {
        var result = Result<int, string>.Err("fail");

        var ex = Assert.Throws<InvalidOperationException>(() => result.Unwrap());
        Assert.Contains("fail", ex.Message);
    }

    // ================================================================
    // UnwrapOr
    // ================================================================

    /// <summary>
    /// 驗證 UnwrapOr 在成功狀態下回傳原始值，忽略預設值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Ok_ReturnsValue()
    {
        var result = Result<int, string>.Ok(42);

        Assert.Equal(42, result.UnwrapOr(0));
    }

    /// <summary>
    /// 驗證 UnwrapOr 在失敗狀態下回傳指定的預設值。
    /// </summary>
    [Fact]
    public void UnwrapOr_Err_ReturnsDefaultValue()
    {
        var result = Result<int, string>.Err("fail");

        Assert.Equal(0, result.UnwrapOr(0));
    }

    // ================================================================
    // UnwrapOrElse
    // ================================================================

    /// <summary>
    /// 驗證 UnwrapOrElse 在成功狀態下回傳原始值，不呼叫工廠函數。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Ok_ReturnsValueWithoutCallingFactory()
    {
        var result = Result<int, string>.Ok(42);
        var factoryCalled = false;

        var value = result.UnwrapOrElse(_ =>
        {
            factoryCalled = true;
            return -1;
        });

        Assert.Equal(42, value);
        Assert.False(factoryCalled);
    }

    /// <summary>
    /// 驗證 UnwrapOrElse 在失敗狀態下呼叫工廠函數並回傳其結果。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Err_CallsFactoryWithErrorAndReturnsResult()
    {
        var result = Result<int, string>.Err("fail");

        var value = result.UnwrapOrElse(e => e.Length);

        Assert.Equal(4, value);
    }

    // ================================================================
    // Expect
    // ================================================================

    /// <summary>
    /// 驗證 Expect 在成功狀態下回傳值。
    /// </summary>
    [Fact]
    public void Expect_Ok_ReturnsValue()
    {
        var result = Result<int, string>.Ok(42);

        Assert.Equal(42, result.Expect("should not fail"));
    }

    /// <summary>
    /// 驗證 Expect 在失敗狀態下拋出包含自定義訊息的 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Expect_Err_ThrowsInvalidOperationExceptionWithMessage()
    {
        var result = Result<int, string>.Err("fail");

        var ex = Assert.Throws<InvalidOperationException>(
            () => result.Expect("custom error message")
        );
        Assert.Equal("custom error message", ex.Message);
    }

    // ================================================================
    // UnwrapErr
    // ================================================================

    /// <summary>
    /// 驗證 UnwrapErr 在失敗狀態下回傳錯誤。
    /// </summary>
    [Fact]
    public void UnwrapErr_Err_ReturnsError()
    {
        var result = Result<int, string>.Err("fail");

        Assert.Equal("fail", result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 UnwrapErr 在成功狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void UnwrapErr_Ok_ThrowsInvalidOperationException()
    {
        var result = Result<int, string>.Ok(42);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapErr());
    }

    // ================================================================
    // ExpectErr
    // ================================================================

    /// <summary>
    /// 驗證 ExpectErr 在失敗狀態下回傳錯誤。
    /// </summary>
    [Fact]
    public void ExpectErr_Err_ReturnsError()
    {
        var result = Result<int, string>.Err("fail");

        Assert.Equal("fail", result.ExpectErr("should be error"));
    }

    /// <summary>
    /// 驗證 ExpectErr 在成功狀態下拋出包含自定義訊息的 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ExpectErr_Ok_ThrowsInvalidOperationExceptionWithMessage()
    {
        var result = Result<int, string>.Ok(42);

        var ex = Assert.Throws<InvalidOperationException>(
            () => result.ExpectErr("expected error but got ok")
        );
        Assert.Equal("expected error but got ok", ex.Message);
    }

    // ================================================================
    // ToOption
    // ================================================================

    /// <summary>
    /// 驗證 ToOption 在成功狀態下回傳 Some，包含原始值。
    /// </summary>
    [Fact]
    public void ToOption_Ok_ReturnsSomeWithValue()
    {
        var result = Result<int, string>.Ok(42);

        var option = result.ToOption();

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Unwrap());
    }

    /// <summary>
    /// 驗證 ToOption 在失敗狀態下回傳 None。
    /// </summary>
    [Fact]
    public void ToOption_Err_ReturnsNone()
    {
        var result = Result<int, string>.Err("fail");

        var option = result.ToOption();

        Assert.True(option.IsNone);
    }

    // ================================================================
    // Err() (returns error as Option)
    // ================================================================

    /// <summary>
    /// 驗證 Err() 方法在失敗狀態下回傳 Some，包含錯誤值。
    /// </summary>
    [Fact]
    public void ErrMethod_Err_ReturnsSomeWithError()
    {
        var result = Result<int, string>.Err("fail");

        var option = result.Err();

        Assert.True(option.IsSome);
        Assert.Equal("fail", option.Unwrap());
    }

    /// <summary>
    /// 驗證 Err() 方法在成功狀態下回傳 None。
    /// </summary>
    [Fact]
    public void ErrMethod_Ok_ReturnsNone()
    {
        var result = Result<int, string>.Ok(42);

        var option = result.Err();

        Assert.True(option.IsNone);
    }

    // ================================================================
    // Select (LINQ)
    // ================================================================

    /// <summary>
    /// 驗證 Select（LINQ）在成功狀態下等同於 Map，轉換值。
    /// </summary>
    [Fact]
    public void Select_Ok_MapsValue()
    {
        var result = Result<int, string>.Ok(10);

        var selected = result.Select(x => x * 3);

        Assert.True(selected.IsOk);
        Assert.Equal(30, selected.Unwrap());
    }

    /// <summary>
    /// 驗證 Select（LINQ）在失敗狀態下保留原始錯誤。
    /// </summary>
    [Fact]
    public void Select_Err_PreservesError()
    {
        var result = Result<int, string>.Err("fail");

        var selected = result.Select(x => x * 3);

        Assert.True(selected.IsErr);
        Assert.Equal("fail", selected.UnwrapErr());
    }

    // ================================================================
    // SelectMany (LINQ)
    // ================================================================

    /// <summary>
    /// 驗證 SelectMany（LINQ）在兩步均成功時回傳最終結果。
    /// </summary>
    [Fact]
    public void SelectMany_BothOk_ReturnsCombinedResult()
    {
        var result = Result<int, string>.Ok(10);

        var combined = result.SelectMany(
            x => Result<int, string>.Ok(x + 5),
            (original, intermediate) => original + intermediate
        );

        Assert.True(combined.IsOk);
        Assert.Equal(25, combined.Unwrap()); // 10 + 15
    }

    /// <summary>
    /// 驗證 SelectMany（LINQ）在第一步失敗時保留錯誤。
    /// </summary>
    [Fact]
    public void SelectMany_FirstErr_PreservesError()
    {
        var result = Result<int, string>.Err("first error");

        var combined = result.SelectMany(
            x => Result<int, string>.Ok(x + 5),
            (original, intermediate) => original + intermediate
        );

        Assert.True(combined.IsErr);
        Assert.Equal("first error", combined.UnwrapErr());
    }

    /// <summary>
    /// 驗證 SelectMany（LINQ）在第二步失敗時回傳第二步的錯誤。
    /// </summary>
    [Fact]
    public void SelectMany_SecondErr_ReturnsSecondError()
    {
        var result = Result<int, string>.Ok(10);

        var combined = result.SelectMany(
            _ => Result<int, string>.Err("second error"),
            (original, intermediate) => original + intermediate
        );

        Assert.True(combined.IsErr);
        Assert.Equal("second error", combined.UnwrapErr());
    }

    // ================================================================
    // LINQ Query Syntax
    // ================================================================

    /// <summary>
    /// 驗證 LINQ 查詢語法的 select 子句可以正確轉換成功的值。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_Select_Ok_TransformsValue()
    {
        var result = Result<int, string>.Ok(7);

        var query = from x in result
                    select x * 6;

        Assert.True(query.IsOk);
        Assert.Equal(42, query.Unwrap());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 子句在成功路徑下組合結果。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_AllOk_CombinesResults()
    {
        static Result<int, string> GetA() => Result<int, string>.Ok(10);
        static Result<int, string> GetB(int a) => Result<int, string>.Ok(a + 20);

        var query = from a in GetA()
                    from b in GetB(a)
                    select a + b;

        Assert.True(query.IsOk);
        Assert.Equal(40, query.Unwrap()); // 10 + 30
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 子句在第一步失敗時短路。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_FirstErr_ShortCircuits()
    {
        static Result<int, string> GetA() => Result<int, string>.Err("a failed");
        static Result<int, string> GetB(int a) => Result<int, string>.Ok(a + 20);

        var query = from a in GetA()
                    from b in GetB(a)
                    select a + b;

        Assert.True(query.IsErr);
        Assert.Equal("a failed", query.UnwrapErr());
    }

    /// <summary>
    /// 驗證 LINQ 查詢語法的多重 from 子句在第二步失敗時回傳第二步的錯誤。
    /// </summary>
    [Fact]
    public void LinqQuerySyntax_MultipleFrom_SecondErr_ReturnsSecondError()
    {
        static Result<int, string> GetA() => Result<int, string>.Ok(10);
        static Result<int, string> GetB(int _) => Result<int, string>.Err("b failed");

        var query = from a in GetA()
                    from b in GetB(a)
                    select a + b;

        Assert.True(query.IsErr);
        Assert.Equal("b failed", query.UnwrapErr());
    }

    // ================================================================
    // MapOr
    // ================================================================

    /// <summary>
    /// 驗證 MapOr 在成功狀態下應用 mapper 並回傳轉換結果。
    /// </summary>
    [Fact]
    public void MapOr_Ok_AppliesMapper()
    {
        var result = Result<int, string>.Ok(10);

        var value = result.MapOr("default", v => $"value={v}");

        Assert.Equal("value=10", value);
    }

    /// <summary>
    /// 驗證 MapOr 在失敗狀態下回傳預設值。
    /// </summary>
    [Fact]
    public void MapOr_Err_ReturnsDefaultValue()
    {
        var result = Result<int, string>.Err("fail");

        var value = result.MapOr("default", v => $"value={v}");

        Assert.Equal("default", value);
    }

    // ================================================================
    // MapOrElse
    // ================================================================

    /// <summary>
    /// 驗證 MapOrElse 在成功狀態下應用 mapper 函數。
    /// </summary>
    [Fact]
    public void MapOrElse_Ok_AppliesMapper()
    {
        var result = Result<int, string>.Ok(10);

        var value = result.MapOrElse(
            fallback: e => $"error={e}",
            mapper: v => $"value={v}"
        );

        Assert.Equal("value=10", value);
    }

    /// <summary>
    /// 驗證 MapOrElse 在失敗狀態下應用 fallback 函數。
    /// </summary>
    [Fact]
    public void MapOrElse_Err_AppliesFallback()
    {
        var result = Result<int, string>.Err("fail");

        var value = result.MapOrElse(
            fallback: e => $"error={e}",
            mapper: v => $"value={v}"
        );

        Assert.Equal("error=fail", value);
    }

    // ================================================================
    // Deconstruct
    // ================================================================

    /// <summary>
    /// 驗證 Deconstruct 在成功狀態下正確解構為 (true, value, default(TE))。
    /// </summary>
    [Fact]
    public void Deconstruct_Ok_ReturnsIsOkTrueWithValueAndNullError()
    {
        var result = Result<int, string>.Ok(42);

        var (isOk, value, error) = result;

        Assert.True(isOk);
        Assert.Equal(42, value);
        Assert.Null(error);
    }

    /// <summary>
    /// 驗證 Deconstruct 在失敗狀態下正確解構為 (false, default(T), error)。
    /// </summary>
    [Fact]
    public void Deconstruct_Err_ReturnsIsOkFalseWithDefaultValueAndError()
    {
        var result = Result<int, string>.Err("fail");

        var (isOk, value, error) = result;

        Assert.False(isOk);
        Assert.Equal(default, value);
        Assert.Equal("fail", error);
    }

    /// <summary>
    /// 驗證 Deconstruct 可搭配 switch 表達式使用，正確匹配成功與失敗情境。
    /// </summary>
    [Fact]
    public void Deconstruct_SwitchExpression_MatchesBothCases()
    {
        var okResult = Result<int, string>.Ok(42);
        var errResult = Result<int, string>.Err("fail");

        var okMessage = okResult switch
        {
            (true, var v, _) => $"Ok:{v}",
            (false, _, var e) => $"Err:{e}"
        };

        var errMessage = errResult switch
        {
            (true, var v, _) => $"Ok:{v}",
            (false, _, var e) => $"Err:{e}"
        };

        Assert.Equal("Ok:42", okMessage);
        Assert.Equal("Err:fail", errMessage);
    }

    /// <summary>
    /// 驗證 Deconstruct 在未初始化狀態下拋出 InvalidOperationException，而非解構為看似合法的 (false, default, default)。
    /// </summary>
    /// <remarks>
    /// 解構是「讀取內部值」的操作，因此與 Match／Map 同樣中毒化未初始化狀態。
    /// 若不拋出，<c>(false, _, var e)</c> 這條 XML 文件推薦的 pattern matching 分支，
    /// 在 TE 為 enum 時會拿到 <c>default(TE)</c>——一個憑空捏造的業務錯誤。
    /// </remarks>
    [Fact]
    public void Deconstruct_Uninitialized_Throws()
    {
        var result = default(Result<int, string>);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var (_, _, _) = result;
        });

        Assert.Contains("uninitialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同 <see cref="Deconstruct_Uninitialized_Throws"/>，但以<b>值型別</b>錯誤具現化——
    /// 這正是本防護原本最需要生效、卻最容易被誤判為「已經正常」的路徑：
    /// TestError.NotFound = 0，若不拋出，呼叫端會得到一個看似合法的 Err(NotFound)。
    /// </summary>
    [Fact]
    public void Deconstruct_UninitializedWithEnumError_Throws()
    {
        var result = default(Result<int, TestError>);

        Assert.Throws<InvalidOperationException>(() =>
        {
            var (_, _, _) = result;
        });
    }

    /// <summary>
    /// 驗證 Deconstruct 在 Ok／Err 兩個已初始化狀態下不受防護影響，行為與先前一致。
    /// </summary>
    [Fact]
    public void Deconstruct_Initialized_IsUnaffectedByUninitializedGuard()
    {
        var (okIsOk, okValue, okError) = Result<int, TestError>.Ok(42);
        Assert.True(okIsOk);
        Assert.Equal(42, okValue);
        Assert.Equal(default, okError);

        var (errIsOk, errValue, errError) = Result<int, TestError>.Err(TestError.Unauthorized);
        Assert.False(errIsOk);
        Assert.Equal(default, errValue);
        Assert.Equal(TestError.Unauthorized, errError);
    }

    // ================================================================
    // IsUninitialized
    // ================================================================

    /// <summary>
    /// 驗證 IsUninitialized 能安全地區分「未初始化」與「合法的 Err」，
    /// 包含錯誤值恰好等於 default(TE) 的 enum 情境——這是 IsErr 無法區分的。
    /// </summary>
    [Fact]
    public void IsUninitialized_DistinguishesDefaultStructFromLegitimateErr()
    {
        Assert.True(default(Result<int, string>).IsUninitialized);
        Assert.True(default(Result<int, TestError>).IsUninitialized);

        Assert.False(Result<int, string>.Ok(1).IsUninitialized);
        Assert.False(Result<int, string>.Err("boom").IsUninitialized);

        // TestError.NotFound == default(TestError)：這是先前版本會誤判的關鍵案例。
        var errWithDefaultValue = Result<int, TestError>.Err(default);
        Assert.False(errWithDefaultValue.IsUninitialized);
        Assert.True(errWithDefaultValue.IsErr);
    }

    /// <summary>
    /// 驗證 IsUninitialized 本身不會拋出例外——它是唯一能「安全」偵測未初始化狀態的成員，
    /// 若它自己會拋，就失去存在意義了。
    /// </summary>
    [Fact]
    public void IsUninitialized_OnUninitialized_DoesNotThrow()
    {
        var result = default(Result<int, TestError>);

        Assert.True(result.IsUninitialized);

        // 對照：IsErr 同樣回傳 true 且不拋，但緊接著取值就會拋——這正是需要 IsUninitialized 的理由。
        Assert.True(result.IsErr);
        Assert.Throws<InvalidOperationException>(() => result.TryGetErr(out _));
    }

    // ================================================================
    // Or：兩個運算元都必須已初始化
    // ================================================================

    /// <summary>
    /// 驗證 Or 對「替代值」也做未初始化檢查，避免未初始化的 Result 從組合子流出，
    /// 在遠離錯誤來源的後續呼叫點才爆炸。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Or_WithUninitializedOther_Throws(bool selfIsOk)
    {
        var self = selfIsOk ? Result<int, string>.Ok(1) : Result<int, string>.Err("boom");

        var ex = Assert.Throws<InvalidOperationException>(() => self.Or(default));

        Assert.Contains("uninitialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同上，以值型別錯誤具現化。
    /// </summary>
    [Fact]
    public void Or_WithUninitializedOtherAndEnumError_Throws()
    {
        var self = Result<int, TestError>.Err(TestError.Unauthorized);

        Assert.Throws<InvalidOperationException>(() => self.Or(default));
    }

    /// <summary>
    /// 驗證兩個運算元都已初始化時，Or 的行為完全不變。
    /// </summary>
    [Fact]
    public void Or_BothInitialized_BehavesAsBefore()
    {
        Assert.Equal(Result<int, string>.Ok(1), Result<int, string>.Ok(1).Or(Result<int, string>.Ok(2)));
        Assert.Equal(Result<int, string>.Ok(2), Result<int, string>.Err("boom").Or(Result<int, string>.Ok(2)));
        Assert.Equal(Result<int, string>.Err("b"), Result<int, string>.Err("a").Or(Result<int, string>.Err("b")));
    }

    // ================================================================
    // Equality
    // ================================================================

    /// <summary>
    /// 驗證兩個相同值的 Ok Result 相等。
    /// </summary>
    [Fact]
    public void Equals_TwoOkWithSameValue_ReturnsTrue()
    {
        var r1 = Result<int, string>.Ok(42);
        var r2 = Result<int, string>.Ok(42);

        Assert.True(r1.Equals(r2));
        Assert.True(r1 == r2);
        Assert.False(r1 != r2);
    }

    /// <summary>
    /// 驗證兩個不同值的 Ok Result 不相等。
    /// </summary>
    [Fact]
    public void Equals_TwoOkWithDifferentValues_ReturnsFalse()
    {
        var r1 = Result<int, string>.Ok(42);
        var r2 = Result<int, string>.Ok(100);

        Assert.False(r1.Equals(r2));
        Assert.False(r1 == r2);
        Assert.True(r1 != r2);
    }

    /// <summary>
    /// 驗證兩個相同錯誤的 Err Result 相等。
    /// </summary>
    [Fact]
    public void Equals_TwoErrWithSameError_ReturnsTrue()
    {
        var r1 = Result<int, string>.Err("error");
        var r2 = Result<int, string>.Err("error");

        Assert.True(r1.Equals(r2));
        Assert.True(r1 == r2);
    }

    /// <summary>
    /// 驗證兩個不同錯誤的 Err Result 不相等。
    /// </summary>
    [Fact]
    public void Equals_TwoErrWithDifferentErrors_ReturnsFalse()
    {
        var r1 = Result<int, string>.Err("error1");
        var r2 = Result<int, string>.Err("error2");

        Assert.False(r1.Equals(r2));
        Assert.False(r1 == r2);
    }

    /// <summary>
    /// 驗證 Ok 和 Err 永遠不相等，即使底層值相同類型。
    /// </summary>
    [Fact]
    public void Equals_OkAndErr_ReturnsFalse()
    {
        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("error");

        Assert.False(ok.Equals(err));
        Assert.False(ok == err);
    }

    /// <summary>
    /// 驗證 Equals(object) 在傳入非 Result 型別時回傳 false。
    /// </summary>
    [Fact]
    public void EqualsObject_NonResultType_ReturnsFalse()
    {
        var result = Result<int, string>.Ok(42);

        // 明確轉型為 object 以避免隱式轉換至 Result<int, string> 的 Equals 多載
        Assert.False(result.Equals((object)"not a result"));
        Assert.False(result.Equals((object)42));
        Assert.False(result.Equals((object?)null));
    }

    /// <summary>
    /// 驗證 Equals(object) 在傳入相同值的裝箱 Result 時回傳 true。
    /// </summary>
    [Fact]
    public void EqualsObject_BoxedResultWithSameValue_ReturnsTrue()
    {
        var r1 = Result<int, string>.Ok(42);
        var r2 = (object)Result<int, string>.Ok(42);

        Assert.True(r1.Equals(r2));
    }

    /// <summary>
    /// 驗證兩個相等的 Result 產生相同的 HashCode。
    /// </summary>
    [Fact]
    public void GetHashCode_EqualResults_HaveSameHashCode()
    {
        var r1 = Result<int, string>.Ok(42);
        var r2 = Result<int, string>.Ok(42);
        var e1 = Result<int, string>.Err("error");
        var e2 = Result<int, string>.Err("error");

        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
        Assert.Equal(e1.GetHashCode(), e2.GetHashCode());
    }

    /// <summary>
    /// 驗證 Ok 和 Err 產生不同的 HashCode。
    /// </summary>
    [Fact]
    public void GetHashCode_OkAndErr_HaveDifferentHashCodes()
    {
        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("42");

        Assert.NotEqual(ok.GetHashCode(), err.GetHashCode());
    }

    // ================================================================
    // ToString
    // ================================================================

    /// <summary>
    /// 驗證 ToString 在成功狀態下輸出 "Ok(value)" 格式。
    /// </summary>
    [Fact]
    public void ToString_Ok_ReturnsOkFormat()
    {
        var result = Result<int, string>.Ok(42);

        Assert.Equal("Ok(42)", result.ToString());
    }

    /// <summary>
    /// 驗證 ToString 在失敗狀態下輸出 "Err(error)" 格式。
    /// </summary>
    [Fact]
    public void ToString_Err_ReturnsErrFormat()
    {
        var result = Result<int, string>.Err("fail");

        Assert.Equal("Err(fail)", result.ToString());
    }

    // ================================================================
    // Uninitialized (default struct) - comprehensive checks
    // ================================================================

    /// <summary>
    /// 驗證未初始化的 Result（default struct）的 IsOk 為 false、IsErr 為 true——這兩個屬性本身是單純的欄位讀取，不會拋出例外。
    /// </summary>
    [Fact]
    public void Uninitialized_IsOkIsFalse_IsErrIsTrue()
    {
        var result = default(Result<int, string>);

        Assert.False(result.IsOk);
        Assert.True(result.IsErr);
    }

    /// <summary>
    /// 列舉所有「未初始化 Result 應拋出 InvalidOperationException」的操作，供 <see cref="Operation_OnUninitializedResult_ThrowsInvalidOperationException"/> 使用。
    /// 新增一個會存取 <c>_value</c>/<c>_error</c> 的方法時，只需在此補上一筆資料即可獲得覆蓋，不需要複製整個測試方法。
    /// </summary>
    public static IEnumerable<object[]> UninitializedThrowingOperations()
    {
        yield return new object[] { "Contains", (Action<Result<int, string>>)(r => r.Contains(0)) };
        yield return new object[] { "ContainsErr", (Action<Result<int, string>>)(r => r.ContainsErr("e")) };
        yield return new object[] { "IsOkAnd", (Action<Result<int, string>>)(r => r.IsOkAnd(_ => true)) };
        yield return new object[] { "IsErrAnd", (Action<Result<int, string>>)(r => r.IsErrAnd(_ => true)) };
        yield return new object[] { "Match(TResult)", (Action<Result<int, string>>)(r => r.Match(v => v, _ => 0)) };
        yield return new object[] { "Match(Action)", (Action<Result<int, string>>)(r => r.Match(_ => { }, _ => { })) };
        yield return new object[] { "Map", (Action<Result<int, string>>)(r => r.Map(x => x)) };
        yield return new object[] { "MapErr", (Action<Result<int, string>>)(r => r.MapErr(e => e)) };
        yield return new object[] { "Bind", (Action<Result<int, string>>)(r => r.Bind(x => Result<int, string>.Ok(x))) };
        yield return new object[] { "Or(self 未初始化)", (Action<Result<int, string>>)(r => r.Or(Result<int, string>.Ok(1))) };
        // Or 的兩個運算元都必須已初始化——只驗證 self 會讓未初始化狀態從組合子流出。
        yield return new object[] { "Or(other 未初始化)", (Action<Result<int, string>>)(_ => Result<int, string>.Ok(1).Or(default)) };
        yield return new object[] { "OrElse", (Action<Result<int, string>>)(r => r.OrElse(_ => Result<int, string>.Ok(1))) };
        yield return new object[] { "Tap", (Action<Result<int, string>>)(r => r.Tap(_ => { })) };
        yield return new object[] { "TapErr", (Action<Result<int, string>>)(r => r.TapErr(_ => { })) };
        yield return new object[] { "TryGetOk(1 out)", (Action<Result<int, string>>)(r => r.TryGetOk(out _)) };
        yield return new object[] { "TryGetOk(2 out)", (Action<Result<int, string>>)(r => r.TryGetOk(out _, out _)) };
        yield return new object[] { "TryGetErr", (Action<Result<int, string>>)(r => r.TryGetErr(out _)) };
        yield return new object[] { "Unwrap", (Action<Result<int, string>>)(r => r.Unwrap()) };
        yield return new object[] { "UnwrapOr", (Action<Result<int, string>>)(r => r.UnwrapOr(0)) };
        yield return new object[] { "UnwrapOrElse", (Action<Result<int, string>>)(r => r.UnwrapOrElse(_ => 0)) };
        yield return new object[] { "Expect", (Action<Result<int, string>>)(r => r.Expect("msg")) };
        yield return new object[] { "UnwrapErr", (Action<Result<int, string>>)(r => r.UnwrapErr()) };
        yield return new object[] { "ExpectErr", (Action<Result<int, string>>)(r => r.ExpectErr("msg")) };
        yield return new object[] { "ToOption", (Action<Result<int, string>>)(r => r.ToOption()) };
        yield return new object[] { "Err", (Action<Result<int, string>>)(r => r.Err()) };
        yield return new object[] { "MapOr", (Action<Result<int, string>>)(r => r.MapOr("default", v => $"{v}")) };
        yield return new object[] { "MapOrElse", (Action<Result<int, string>>)(r => r.MapOrElse(_ => "fallback", v => $"{v}")) };
        yield return new object[] { "Select", (Action<Result<int, string>>)(r => r.Select(x => x)) };
        yield return new object[]
        {
            "SelectMany", (Action<Result<int, string>>)(r => r.SelectMany(x => Result<int, string>.Ok(x), (a, b) => a + b))
        };
        yield return new object[] { "CompareTo", (Action<Result<int, string>>)(r => r.CompareTo(Result<int, string>.Ok(1))) };
        // Deconstruct 同樣是「讀取內部值」的操作，必須中毒化——
        // 這是 26.7.26 修正 ToString 時漏掉的成員，且它是文件推薦的 pattern matching 入口。
        yield return new object[]
        {
            "Deconstruct", (Action<Result<int, string>>)(r =>
            {
                var (_, _, _) = r;
            })
        };
    }

    /// <summary>
    /// 驗證會存取內部值／錯誤的每一個公開操作，在未初始化（default struct）的 Result 上呼叫時都會拋出 InvalidOperationException。
    /// </summary>
    /// <param name="operationName">操作名稱，用於在測試總管中識別是哪一筆資料失敗，並確保 <see cref="UninitializedThrowingOperations"/> 的每筆資料都有名稱。</param>
    /// <param name="operation">實際要執行的操作。</param>
    [Theory]
    [MemberData(nameof(UninitializedThrowingOperations))]
    public void Operation_OnUninitializedResult_ThrowsInvalidOperationException(
        string operationName, Action<Result<int, string>> operation)
    {
        Assert.False(string.IsNullOrWhiteSpace(operationName));

        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => operation(result));
    }

    // ================================================================
    // Chaining
    // ================================================================

    /// <summary>
    /// 驗證多個方法的鏈式呼叫在成功路徑下正確運作。
    /// </summary>
    [Fact]
    public void Chaining_OkPath_AllOperationsExecuteCorrectly()
    {
        var tapValues = new List<int>();

        var result = Result<int, string>.Ok(10)
            .Map(x => x * 2)
            .Tap(x => tapValues.Add(x))
            .Map(x => x + 5)
            .Tap(x => tapValues.Add(x));

        Assert.True(result.IsOk);
        Assert.Equal(25, result.Unwrap());
        Assert.Equal([20, 25], tapValues);
    }

    /// <summary>
    /// 驗證鏈式呼叫中遇到錯誤時後續操作被跳過。
    /// </summary>
    [Fact]
    public void Chaining_ErrPath_SubsequentMapsSkipped()
    {
        var mapCalled = false;

        var result = Result<int, string>.Err("initial error")
            .Map(x =>
            {
                mapCalled = true;
                return x * 2;
            })
            .Map(x => x + 5);

        Assert.True(result.IsErr);
        Assert.Equal("initial error", result.UnwrapErr());
        Assert.False(mapCalled);
    }

    /// <summary>
    /// 驗證 Tap 和 TapErr 在鏈式呼叫中可以正確觀察值和錯誤。
    /// </summary>
    [Fact]
    public void Chaining_TapAndTapErr_ObserveCorrectState()
    {
        var okTapCalled = false;
        var errTapCalled = false;

        Result<int, string>.Err("fail")
            .Tap(_ => okTapCalled = true)
            .TapErr(_ => errTapCalled = true);

        Assert.False(okTapCalled);
        Assert.True(errTapCalled);
    }

    /// <summary>
    /// 驗證 Bind 鏈式呼叫模擬鐵路導向程式設計（ROP）。
    /// </summary>
    [Fact]
    public void Chaining_BindChain_RailwayOrientedProgramming()
    {
        static Result<int, string> Validate(int x) =>
            x > 0 ? Result<int, string>.Ok(x) : Result<int, string>.Err("must be positive");

        static Result<int, string> Double(int x) =>
            Result<int, string>.Ok(x * 2);

        static Result<int, string> CheckMax(int x) =>
            x <= 100 ? Result<int, string>.Ok(x) : Result<int, string>.Err("exceeds maximum");

        // 成功路徑
        var success = Validate(10).Bind(Double).Bind(CheckMax);
        Assert.True(success.IsOk);
        Assert.Equal(20, success.Unwrap());

        // 第一步失敗
        var fail1 = Validate(-1).Bind(Double).Bind(CheckMax);
        Assert.True(fail1.IsErr);
        Assert.Equal("must be positive", fail1.UnwrapErr());

        // 第三步失敗
        var fail3 = Validate(60).Bind(Double).Bind(CheckMax);
        Assert.True(fail3.IsErr);
        Assert.Equal("exceeds maximum", fail3.UnwrapErr());
    }

    // ================================================================
    // Edge Cases
    // ================================================================

    /// <summary>
    /// 驗證 Result 可以使用值型別（int）作為錯誤類型。
    /// </summary>
    [Fact]
    public void EdgeCase_ValueTypeAsError_WorksCorrectly()
    {
        var result = Result<string, int>.Err(404);

        Assert.True(result.IsErr);
        Assert.Equal(404, result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Result 可以使用列舉（enum）作為錯誤類型。
    /// </summary>
    [Fact]
    public void EdgeCase_EnumAsError_WorksCorrectly()
    {
        var result = Result<string, TestError>.Err(TestError.NotFound);

        Assert.True(result.IsErr);
        Assert.Equal(TestError.NotFound, result.UnwrapErr());
    }

    /// <summary>
    /// 驗證 Ok 值為空字串時仍為成功狀態。
    /// </summary>
    [Fact]
    public void EdgeCase_EmptyStringAsOkValue_IsStillOk()
    {
        var result = Result<string, int>.Ok("");

        Assert.True(result.IsOk);
        Assert.Equal("", result.Unwrap());
    }

    /// <summary>
    /// 驗證 Map 可以將型別從一種轉換為完全不同的另一種。
    /// </summary>
    [Fact]
    public void EdgeCase_MapChangesType_WorksCorrectly()
    {
        var result = Result<int, string>.Ok(15);

        var mapped = result.Map(x => new DateTime(2024, 1, x));

        Assert.True(mapped.IsOk);
        Assert.Equal(new DateTime(2024, 1, 15), mapped.Unwrap());
    }

    /// <summary>
    /// 驗證 Ok(0) 和 Ok(default(int)) 應視為相等。
    /// </summary>
    [Fact]
    public void EdgeCase_OkWithDefaultValueType_IsStillOk()
    {
        var result = Result<int, string>.Ok(0);

        Assert.True(result.IsOk);
        Assert.Equal(0, result.Unwrap());
        Assert.True(result.Contains(0));
    }

    /// <summary>
    /// 驗證隱式轉換在方法回傳值情境中能正確運作。
    /// </summary>
    [Fact]
    public void EdgeCase_ImplicitConversionInMethodReturn_WorksCorrectly()
    {
        static Result<int, string> Divide(int a, int b)
        {
            if (b == 0) return "Division by zero";
            return a / b;
        }

        var ok = Divide(10, 2);
        Assert.True(ok.IsOk);
        Assert.Equal(5, ok.Unwrap());

        var err = Divide(10, 0);
        Assert.True(err.IsErr);
        Assert.Equal("Division by zero", err.UnwrapErr());
    }

    /// <summary>
    /// 驗證兩個 default（未初始化）Result 相等。
    /// </summary>
    [Fact]
    public void EdgeCase_TwoDefaultResults_AreEqual()
    {
        var r1 = default(Result<int, string>);
        var r2 = default(Result<int, string>);

        Assert.True(r1 == r2);
        Assert.True(r1.Equals(r2));
    }

    /// <summary>
    /// 驗證兩個未初始化的 Result 產生相同的 HashCode，與 Equals/== 的「未初始化視為彼此相等」行為一致。
    /// 刻意不呼叫 ThrowIfUninitialized：GetHashCode 依 .NET 慣例不應拋出例外（見 <see cref="Result{T,TE}.GetHashCode"/> 的備註）。
    /// </summary>
    [Fact]
    public void EdgeCase_TwoDefaultResults_HaveSameHashCode()
    {
        var r1 = default(Result<int, string>);
        var r2 = default(Result<int, string>);

        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
    }

    /// <summary>
    /// 驗證未初始化的 Result 呼叫 ToString 不會拋出例外，而是回傳 "Uninitialized"。
    /// 與 GetHashCode 相同，這是刻意不套用「未初始化即中毒」規則的成員。
    /// </summary>
    /// <remarks>
    /// 先前版本回傳 "Err()"。改為明確的 "Uninitialized" 是因為當 TE 為值型別時，
    /// 舊輸出會呈現為具誤導性的 "Err(0)"／"Err(None)"，讓未初始化的 struct 在偵錯工具中
    /// 看起來像一個合法的失敗結果——這正是 ResultState 三態設計要消除的混淆。
    /// </remarks>
    [Fact]
    public void EdgeCase_DefaultResult_ToStringReturnsUninitialized()
    {
        var result = default(Result<int, string>);

        Assert.Equal("Uninitialized", result.ToString());
    }

    /// <summary>
    /// 驗證錯誤型別為值型別（enum）時，未初始化的 Result 同樣回傳 "Uninitialized"，
    /// 而不是看起來合法的 "Err(None)"。
    /// </summary>
    [Fact]
    public void EdgeCase_DefaultResultWithEnumError_ToStringReturnsUninitialized()
    {
        var result = default(Result<int, TestError>);

        Assert.Equal("Uninitialized", result.ToString());
        Assert.Equal("Err(NotFound)", Result<int, TestError>.Err(TestError.NotFound).ToString());
    }

    /// <summary>
    /// 驗證 Ok&lt;T&gt; record struct 的 Value 屬性能正確存取。
    /// </summary>
    [Fact]
    public void OkWrapper_ValueProperty_ReturnsCorrectValue()
    {
        var ok = new Ok<int>(42);

        Assert.Equal(42, ok.Value);
    }

    /// <summary>
    /// 驗證 Err&lt;TE&gt; record struct 的 Error 屬性能正確存取。
    /// </summary>
    [Fact]
    public void ErrWrapper_ErrorProperty_ReturnsCorrectError()
    {
        var err = new Err<string>("fail");

        Assert.Equal("fail", err.Error);
    }

    /// <summary>
    /// 驗證 Ok&lt;T&gt; record struct 的值相等性。
    /// </summary>
    [Fact]
    public void OkWrapper_Equality_WorksCorrectly()
    {
        var ok1 = new Ok<int>(42);
        var ok2 = new Ok<int>(42);
        var ok3 = new Ok<int>(100);

        Assert.Equal(ok1, ok2);
        Assert.NotEqual(ok1, ok3);
    }

    /// <summary>
    /// 驗證 Err&lt;TE&gt; record struct 的值相等性。
    /// </summary>
    [Fact]
    public void ErrWrapper_Equality_WorksCorrectly()
    {
        var err1 = new Err<string>("fail");
        var err2 = new Err<string>("fail");
        var err3 = new Err<string>("other");

        Assert.Equal(err1, err2);
        Assert.NotEqual(err1, err3);
    }

    /// <summary>
    /// 驗證 Or 在兩個 Err 情境下回傳第二個 Err。
    /// </summary>
    [Fact]
    public void Or_BothErr_ReturnsSecondErr()
    {
        var first = Result<int, string>.Err("first");
        var second = Result<int, string>.Err("second");

        var actual = first.Or(second);

        Assert.True(actual.IsErr);
        Assert.Equal("second", actual.UnwrapErr());
    }

    /// <summary>
    /// 驗證 OrElse 工廠函數收到正確的錯誤參數。
    /// </summary>
    [Fact]
    public void OrElse_Err_FactoryReceivesOriginalError()
    {
        var result = Result<int, string>.Err("original");
        var receivedError = (string?)null;

        result.OrElse(err =>
        {
            receivedError = err;
            return Result<int, string>.Ok(0);
        });

        Assert.Equal("original", receivedError);
    }

    /// <summary>
    /// 驗證使用複雜物件作為成功值時所有方法正常運作。
    /// </summary>
    [Fact]
    public void EdgeCase_ComplexObjectAsValue_WorksCorrectly()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = Result<List<int>, string>.Ok(list);

        Assert.True(result.IsOk);
        Assert.Same(list, result.Unwrap());

        var mapped = result.Map(l => l.Count);
        Assert.Equal(3, mapped.Unwrap());
    }

    /// <summary>
    /// 驗證 Unwrap 在 Err 狀態下拋出的例外訊息包含錯誤的 ToString 輸出。
    /// </summary>
    [Fact]
    public void Unwrap_Err_ExceptionMessageContainsErrorToString()
    {
        var result = Result<int, string>.Err("detailed error info");

        var ex = Assert.Throws<InvalidOperationException>(() => result.Unwrap());

        Assert.Contains("detailed error info", ex.Message);
    }

    /// <summary>
    /// 驗證 UnwrapErr 在 Ok 狀態下拋出的例外訊息指出 Result 為 Ok。
    /// </summary>
    [Fact]
    public void UnwrapErr_Ok_ExceptionMessageIndicatesOk()
    {
        var result = Result<int, string>.Ok(42);

        var ex = Assert.Throws<InvalidOperationException>(() => result.UnwrapErr());

        Assert.Contains("Ok", ex.Message);
    }

    // ================================================================
    // Uninitialized with VALUE-TYPE error (回歸測試)
    // ================================================================
    //
    // 背景：先前版本以 `!IsOk && _error is null` 判斷未初始化狀態。由於 `where TE : notnull`
    // 不等同 `where TE : struct`，`TE?` 僅是可為 null 的標註而非 Nullable<TE>，因此當 TE 為 enum
    // 或 struct 時 `_error is null` 會被 JIT 常數摺疊為 false，整套防護在該泛型具現化中靜默失效。
    //
    // 影響尤其嚴重的原因是 enum 的預設值通常是有意義的成員（此處 TestError.NotFound = 0），
    // 使得 default(Result<T, TestError>) 會偽裝成一個合法的 Err(NotFound)，讓呼叫端拿到
    // 看似正常的業務錯誤而無從追查來源。
    //
    // 舊有的 UninitializedThrowingOperations 全部使用 Result<int, string>（參考型別 TE），
    // 完全沒有涵蓋這條路徑。以下測試補上該缺口。

    /// <summary>
    /// 列舉所有「未初始化 Result 應拋出 InvalidOperationException」的操作，
    /// 以<b>值型別</b>錯誤（enum）具現化，與 <see cref="UninitializedThrowingOperations"/> 的參考型別版本對應。
    /// </summary>
    public static IEnumerable<object[]> UninitializedThrowingOperationsWithEnumError()
    {
        yield return ["Contains", (Action<Result<int, TestError>>)(r => r.Contains(0))];
        yield return ["ContainsErr", (Action<Result<int, TestError>>)(r => r.ContainsErr(TestError.NotFound))];
        yield return ["IsOkAnd", (Action<Result<int, TestError>>)(r => r.IsOkAnd(_ => true))];
        yield return ["IsErrAnd", (Action<Result<int, TestError>>)(r => r.IsErrAnd(_ => true))];
        yield return ["Match(TResult)", (Action<Result<int, TestError>>)(r => r.Match(v => v, _ => 0))];
        yield return ["Match(Action)", (Action<Result<int, TestError>>)(r => r.Match(_ => { }, _ => { }))];
        yield return ["Map", (Action<Result<int, TestError>>)(r => r.Map(x => x))];
        yield return ["MapErr", (Action<Result<int, TestError>>)(r => r.MapErr(e => e))];
        yield return ["Bind", (Action<Result<int, TestError>>)(r => r.Bind(Result<int, TestError>.Ok))];
        yield return ["Or(self 未初始化)", (Action<Result<int, TestError>>)(r => r.Or(Result<int, TestError>.Ok(1)))];
        yield return ["Or(other 未初始化)", (Action<Result<int, TestError>>)(_ => Result<int, TestError>.Ok(1).Or(default))];
        yield return ["OrElse", (Action<Result<int, TestError>>)(r => r.OrElse(_ => Result<int, TestError>.Ok(1)))];
        yield return ["Tap", (Action<Result<int, TestError>>)(r => r.Tap(_ => { }))];
        yield return ["TapErr", (Action<Result<int, TestError>>)(r => r.TapErr(_ => { }))];
        yield return ["TryGetOk(1 out)", (Action<Result<int, TestError>>)(r => r.TryGetOk(out _))];
        yield return ["TryGetOk(2 out)", (Action<Result<int, TestError>>)(r => r.TryGetOk(out _, out _))];
        yield return ["TryGetErr", (Action<Result<int, TestError>>)(r => r.TryGetErr(out _))];
        yield return ["Unwrap", (Action<Result<int, TestError>>)(r => r.Unwrap())];
        yield return ["UnwrapOr", (Action<Result<int, TestError>>)(r => r.UnwrapOr(0))];
        yield return ["UnwrapOrElse", (Action<Result<int, TestError>>)(r => r.UnwrapOrElse(_ => 0))];
        yield return ["Expect", (Action<Result<int, TestError>>)(r => r.Expect("msg"))];
        yield return ["UnwrapErr", (Action<Result<int, TestError>>)(r => r.UnwrapErr())];
        yield return ["ExpectErr", (Action<Result<int, TestError>>)(r => r.ExpectErr("msg"))];
        yield return ["ToOption", (Action<Result<int, TestError>>)(r => r.ToOption())];
        yield return ["Err", (Action<Result<int, TestError>>)(r => r.Err())];
        yield return ["MapOr", (Action<Result<int, TestError>>)(r => r.MapOr("default", v => $"{v}"))];
        yield return ["MapOrElse", (Action<Result<int, TestError>>)(r => r.MapOrElse(_ => "fallback", v => $"{v}"))];
        yield return ["Select", (Action<Result<int, TestError>>)(r => r.Select(x => x))];
        yield return
        [
            "SelectMany",
            (Action<Result<int, TestError>>)(r => r.SelectMany(Result<int, TestError>.Ok, (a, b) => a + b))
        ];
        yield return ["CompareTo", (Action<Result<int, TestError>>)(r => r.CompareTo(Result<int, TestError>.Ok(1)))];
        yield return
        [
            "Deconstruct", (Action<Result<int, TestError>>)(r =>
            {
                var (_, _, _) = r;
            })
        ];
    }

    /// <summary>
    /// 驗證當錯誤型別為<b>值型別</b>時，未初始化偵測仍然生效——這是先前版本失效的路徑。
    /// </summary>
    [Theory]
    [MemberData(nameof(UninitializedThrowingOperationsWithEnumError))]
    public void Operation_OnUninitializedResultWithEnumError_ThrowsInvalidOperationException(
        string operationName, Action<Result<int, TestError>> operation)
    {
        Assert.False(string.IsNullOrWhiteSpace(operationName));

        var result = default(Result<int, TestError>);

        var ex = Assert.Throws<InvalidOperationException>(() => operation(result));
        Assert.Contains("uninitialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 驗證未初始化的 Result 不等於「錯誤值恰好是 default(TE) 的合法 Err」。
    /// 這兩者在先前版本中完全無法區分。
    /// </summary>
    [Fact]
    public void Uninitialized_WithEnumError_IsNotEqualToErrOfDefaultEnumValue()
    {
        var uninitialized = default(Result<int, TestError>);
        var legitimateErr = Result<int, TestError>.Err(TestError.NotFound); // NotFound == 0 == default(TestError)

        Assert.NotEqual(uninitialized, legitimateErr);
        Assert.False(uninitialized == legitimateErr);
        Assert.True(uninitialized != legitimateErr);
    }

    /// <summary>
    /// 驗證兩個未初始化的 Result（值型別錯誤）仍彼此相等，且 GetHashCode 一致——
    /// 修正後的三態比較不應破壞雜湊容器的不變性。
    /// </summary>
    [Fact]
    public void Uninitialized_WithEnumError_TwoDefaultsAreEqual()
    {
        var r1 = default(Result<int, TestError>);
        var r2 = default(Result<int, TestError>);

        Assert.True(r1 == r2);
        Assert.True(r1.Equals(r2));
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
    }

    /// <summary>
    /// 驗證錯誤值等於 default(TE) 的合法 Err 完全正常運作，不會被誤判為未初始化。
    /// </summary>
    [Fact]
    public void Err_WithDefaultEnumValue_BehavesAsNormalErr()
    {
        var result = Result<int, TestError>.Err(TestError.NotFound);

        Assert.True(result.IsErr);
        Assert.False(result.IsOk);
        Assert.Equal(TestError.NotFound, result.UnwrapErr());
        Assert.True(result.ContainsErr(TestError.NotFound));
        Assert.Equal(-1, result.MapOr(-1, v => v));
        Assert.Equal(Option<TestError>.Some(TestError.NotFound), result.Err());
    }

    /// <summary>
    /// 驗證錯誤型別為自訂 struct 時，未初始化偵測同樣生效（不限於 enum）。
    /// </summary>
    [Fact]
    public void Uninitialized_WithStructError_ThrowsOnValueAccess()
    {
        var uninitialized = default(Result<int, TestErrorStruct>);

        Assert.Throws<InvalidOperationException>(() => uninitialized.Unwrap());
        Assert.Throws<InvalidOperationException>(() => uninitialized.UnwrapErr());
        Assert.NotEqual(uninitialized, Result<int, TestErrorStruct>.Err(default));
    }

    // ================================================================
    // Helper types for tests
    // ================================================================

    /// <summary>
    /// 測試用的列舉錯誤型別。
    /// </summary>
    /// <remarks>
    /// 必須為 <c>public</c> 而非 <c>private</c>：它出現在 <c>[Theory]</c> 測試方法的參數型別中，
    /// 而 xUnit 要求測試方法為 <c>public</c>，參數型別的可存取性不得低於方法本身（CS0051）。
    /// 刻意讓第一個成員 <see cref="NotFound"/> 的基礎值為 0，以覆蓋「default(TE) 是有意義的列舉成員」這個關鍵情境。
    /// </remarks>
    public enum TestError
    {
        NotFound,
        Unauthorized,
        Timeout
    }

    /// <summary>
    /// 值型別的錯誤，用於驗證未初始化偵測不只對 enum 生效。
    /// 刻意讓 <c>default</c> 是一個看似合法的值。
    /// </summary>
    public readonly record struct TestErrorStruct(int Code, string? Reason);
}
