using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

public class NullContractTests
{
    [Fact]
    public void Ok_Null_RejectsAtConstruction()
    {
        var error = Assert.Throws<ArgumentNullException>(() => Result<string, int>.Ok(null!));
        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void DefaultOkMarker_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string, int> result = default(Ok<string>);
            return result;
        });
    }

    [Fact]
    public void ImplicitSuccess_Null_RejectsAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string, int> result = (string)null!;
            return result;
        });
    }

    [Fact]
    public void Map_Null_RejectsAtMappingBoundary()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Ok(1).Map(static _ => (string)null!));
    }

    [Fact]
    public async Task MapAsync_Null_FaultsReturnedValueTask()
    {
        var pending = Result<int, string>.Ok(1).MapAsync(static _ => ValueTask.FromResult<string>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await pending);
    }

    [Fact]
    public void ValueTypeDefault_RemainsValidSuccess()
    {
        Result<int, string> result = default(Ok<int>);
        Assert.True(result.TryGetOk(out var value));
        Assert.Equal(0, value);
        Assert.Equal(0, result.ToOption().Unwrap());
    }
}
