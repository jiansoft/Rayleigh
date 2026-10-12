using Xunit;

namespace jIAnSoft.Rayleigh.Tests;

public class StateOverloadTests
{
    [Fact]
    public void Option_State_IsPassedToMapAndBind()
    {
        var result = Option<int>.Some(7)
            .Map(5, static (value, state) => value + state)
            .Bind("items", static (value, state) => Option<string>.Some($"{value} {state}"));
        Assert.Equal("12 items", result.Unwrap());
    }

    [Fact]
    public void Result_State_IsPassedToMap()
    {
        var result = Result<int, string>.Ok(7)
            .Map(5, static (value, state) => value + state)
            .Map("items", static (value, state) => $"{value} {state}");
        Assert.Equal("12 items", result.Unwrap());
    }

    [Fact]
    public void ShortCircuit_DoesNotInvokeStatefulDelegates()
    {
        Assert.True(Option<int>.None.Map<int, int>(1, static (_, _) => throw new InvalidOperationException()).IsNone);
        Assert.True(Option<int>.None.Bind<int, int>(1, static (_, _) => throw new InvalidOperationException()).IsNone);
        var error = Result<int, string>.Err("error");
        Assert.Equal("error", error.Map<int, int>(1, static (_, _) => throw new InvalidOperationException()).UnwrapErr());
    }

    [Fact]
    public void StatefulOperations_PreserveInitializationAndNullContracts()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result<int, string>).Map(1, static (x, y) => x + y));
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Ok(1).Map(0, static (_, _) => (string)null!));
        Assert.Throws<ArgumentNullException>(() => Option<int>.Some(1).Map(0, static (_, _) => (string)null!));
    }
}
