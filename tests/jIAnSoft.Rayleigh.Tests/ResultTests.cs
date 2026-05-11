using jIAnSoft.Rayleigh;
using Xunit;

namespace Rayleigh.Tests;

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
    /// 驗證 Contains 在未初始化（default struct）狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Contains_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Contains(42));
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

    /// <summary>
    /// 驗證 ContainsErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ContainsErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ContainsErr("error"));
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
    /// 驗證 IsOkAnd 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void IsOkAnd_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.IsOkAnd(_ => true));
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

    /// <summary>
    /// 驗證 IsErrAnd 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void IsErrAnd_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.IsErrAnd(_ => true));
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

    /// <summary>
    /// 驗證有回傳值的 Match 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Match_WithReturn_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Match(
            ok: v => v,
            err: _ => 0
        ));
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

    /// <summary>
    /// 驗證無回傳值的 Match 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void MatchAction_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Match(
            ok: _ => { },
            err: _ => { }
        ));
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

    /// <summary>
    /// 驗證 Map 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Map_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Map(x => x * 2));
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

    /// <summary>
    /// 驗證 MapErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void MapErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.MapErr(e => e.Length));
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

    /// <summary>
    /// 驗證 Bind 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Bind_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Bind(x => Result<string, string>.Ok($"{x}"))
        );
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

    /// <summary>
    /// 驗證 Or 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Or_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Or(Result<int, string>.Ok(1))
        );
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

    /// <summary>
    /// 驗證 OrElse 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void OrElse_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.OrElse(_ => Result<int, string>.Ok(1))
        );
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

    /// <summary>
    /// 驗證 Tap 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Tap_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Tap(_ => { }));
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

    /// <summary>
    /// 驗證 TapErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void TapErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TapErr(_ => { }));
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

    /// <summary>
    /// 驗證 TryGetOk（單一 out 參數）在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void TryGetOk_SingleOut_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetOk(out _));
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

    /// <summary>
    /// 驗證 TryGetOk（雙 out 參數）在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void TryGetOk_DoubleOut_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetOk(out _, out _));
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

    /// <summary>
    /// 驗證 TryGetErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void TryGetErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetErr(out _));
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

    /// <summary>
    /// 驗證 Unwrap 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Unwrap_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Unwrap());
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

    /// <summary>
    /// 驗證 UnwrapOr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void UnwrapOr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapOr(0));
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

    /// <summary>
    /// 驗證 UnwrapOrElse 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void UnwrapOrElse_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapOrElse(_ => 0));
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

    /// <summary>
    /// 驗證 Expect 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Expect_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Expect("msg"));
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

    /// <summary>
    /// 驗證 UnwrapErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void UnwrapErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

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

    /// <summary>
    /// 驗證 ExpectErr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ExpectErr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ExpectErr("msg"));
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

    /// <summary>
    /// 驗證 ToOption 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ToOption_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ToOption());
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

    /// <summary>
    /// 驗證 Err() 方法在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ErrMethod_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Err());
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

    /// <summary>
    /// 驗證 SelectMany 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void SelectMany_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() =>
            result.SelectMany(
                x => Result<int, string>.Ok(x),
                (a, b) => a + b
            )
        );
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

    /// <summary>
    /// 驗證 MapOr 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void MapOr_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.MapOr("default", v => $"{v}")
        );
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

    /// <summary>
    /// 驗證 MapOrElse 在未初始化狀態下拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void MapOrElse_Uninitialized_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.MapOrElse(_ => "fallback", v => $"{v}")
        );
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
    /// 驗證 Deconstruct 在未初始化狀態下解構為 (false, default, null)，不會拋出例外。
    /// </summary>
    [Fact]
    public void Deconstruct_Uninitialized_ReturnsDefaultValues()
    {
        var result = default(Result<int, string>);

        var (isOk, value, error) = result;

        Assert.False(isOk);
        Assert.Equal(default, value);
        Assert.Null(error);
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
    /// 驗證未初始化的 Result（default struct）的 IsOk 為 false、IsErr 為 true。
    /// </summary>
    [Fact]
    public void Uninitialized_IsOkIsFalse_IsErrIsTrue()
    {
        var result = default(Result<int, string>);

        Assert.False(result.IsOk);
        Assert.True(result.IsErr);
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Match（有回傳值）時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_MatchWithReturn_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Match(v => v, _ => 0)
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Match（無回傳值）時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_MatchAction_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Match(_ => { }, _ => { })
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Map 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Map_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Map(x => x));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 MapErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_MapErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.MapErr(e => e));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Bind 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Bind_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Bind(x => Result<int, string>.Ok(x))
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Or 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Or_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.Or(Result<int, string>.Ok(1))
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 OrElse 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_OrElse_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.OrElse(_ => Result<int, string>.Ok(1))
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Tap 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Tap_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Tap(_ => { }));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 TapErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_TapErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TapErr(_ => { }));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Unwrap 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Unwrap_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Unwrap());
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 UnwrapOr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_UnwrapOr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapOr(0));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 UnwrapOrElse 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_UnwrapOrElse_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapOrElse(_ => 0));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 UnwrapErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_UnwrapErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapErr());
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Expect 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Expect_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Expect("msg"));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 ExpectErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_ExpectErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ExpectErr("msg"));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 ToOption 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_ToOption_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ToOption());
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Err() 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_ErrMethod_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Err());
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 MapOr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_MapOr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.MapOr("default", v => $"{v}")
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 MapOrElse 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_MapOrElse_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(
            () => result.MapOrElse(_ => "fallback", v => $"{v}")
        );
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Contains 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Contains_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Contains(0));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 ContainsErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_ContainsErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.ContainsErr("e"));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 IsOkAnd 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_IsOkAnd_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.IsOkAnd(_ => true));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 IsErrAnd 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_IsErrAnd_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.IsErrAnd(_ => true));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 TryGetOk（單一 out）時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_TryGetOk_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetOk(out _));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 TryGetOk（雙 out）時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_TryGetOkDouble_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetOk(out _, out _));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 TryGetErr 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_TryGetErr_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.TryGetErr(out _));
    }

    /// <summary>
    /// 驗證未初始化的 Result 在呼叫 Select 時拋出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void Uninitialized_Select_ThrowsInvalidOperationException()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Select(x => x));
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
    // Helper types for tests
    // ================================================================

    private enum TestError
    {
        NotFound,
        Unauthorized,
        Timeout
    }
}
