using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

/// <summary>
/// Contains comparison-focused tests for Option and Result ordering operators and CompareTo implementations.
/// This test class exists to document and guard the library contract that absence or error states sort before successful value states, while equal values compare as equal.
/// Test methods take no input, return no values, and use xUnit assertions as their only observable result.
/// </summary>
public class ComparisonTests
{
    /// <summary>
    /// Verifies that Option comparison treats None as less than Some and compares two Some values by their contained values.
    /// Use this test to confirm the ordering contract behind CompareTo and relational operators; it has no parameters, returns void, and fails through xUnit assertions if the contract changes.
    /// </summary>
    [Fact]
    public void Option_CompareTo_ShouldWorkCorrectly()
    {
        var none = Option<int>.None;
        var some1 = Option<int>.Some(1);
        var some2 = Option<int>.Some(2);
        var some1Duplicate = Option<int>.Some(1);

        // None < Some
        Assert.True(none.CompareTo(some1) < 0);
        Assert.True(some1.CompareTo(none) > 0);
        Assert.True(none < some1);
        Assert.True(some1 > none);

        // Some comparisons
        Assert.True(some1.CompareTo(some2) < 0);
        Assert.True(some2.CompareTo(some1) > 0);
        Assert.True(some1 < some2);
        Assert.True(some2 > some1);

        // Equality via CompareTo
        Assert.True(some1.CompareTo(some1Duplicate) == 0);
        Assert.True(some1 <= some1Duplicate);
        Assert.True(some1 >= some1Duplicate);

        // None == None
        Assert.True(none.CompareTo(Option<int>.None) == 0);
    }

    /// <summary>
    /// Verifies that Result comparison treats Err as less than Ok and compares same-state values by their contained error or success value.
    /// Use this test to guard sorting and relational operator behavior for Result; it has no input, returns no value, and reports regressions through assertions.
    /// </summary>
    [Fact]
    public void Result_CompareTo_ShouldWorkCorrectly()
    {
        var err1 = Result<int, string>.Err("A");
        var err2 = Result<int, string>.Err("B");
        var ok1 = Result<int, string>.Ok(1);
        var ok2 = Result<int, string>.Ok(2);

        // Err < Ok
        Assert.True(err1.CompareTo(ok1) < 0);
        Assert.True(ok1.CompareTo(err1) > 0);
        Assert.True(err1 < ok1);
        Assert.True(ok1 > err1);

        // Err comparisons
        Assert.True(err1.CompareTo(err2) < 0);
        Assert.True(err2.CompareTo(err1) > 0);
        Assert.True(err1 < err2);

        // Ok comparisons
        Assert.True(ok1.CompareTo(ok2) < 0);
        Assert.True(ok2.CompareTo(ok1) > 0);
        Assert.True(ok1 < ok2);
    }

    /// <summary>
    /// Verifies that comparing initialized Result values with a default, uninitialized Result throws InvalidOperationException instead of producing a misleading ordering.
    /// Use this test to preserve the safety boundary around poisoned default structs; it accepts no input, returns void, and expects explicit exceptions as the test output.
    /// </summary>
    [Fact]
    public void Result_CompareTo_Uninitialized_Throws()
    {
        var uninit = new Result<int, string>(); // Default struct (IsOk=false, _error=null)
        var ok = Result<int, string>.Ok(1);

        // Comparing with uninitialized should throw
        Assert.Throws<InvalidOperationException>(() => ok.CompareTo(uninit));
        Assert.Throws<InvalidOperationException>(() => uninit.CompareTo(ok));
    }
}
