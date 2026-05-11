using Xunit;
using jIAnSoft.Rayleigh;

namespace Rayleigh.Tests;

public class ComparisonTests
{
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
