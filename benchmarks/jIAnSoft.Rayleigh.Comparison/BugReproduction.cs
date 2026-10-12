extern alias original;

using jIAnSoft.Rayleigh;
using Old = original::jIAnSoft.Rayleigh;

namespace jIAnSoft.Rayleigh.Comparison;

public readonly record struct LargeValue(long A, long B, long C, long D, long E, long F, long G, long H);

internal static class BugReproduction
{
    internal static async Task Run()
    {
        Check("Ok(null)", () => Old.Result<string, int>.Ok(null!), () => Result<string, int>.Ok(null!));
        Check("implicit null success", () => (string)null!, () => (string)null!);
        Check("default Ok marker", () => default(Old.Ok<string>), () => default(Ok<string>));
        Check("Map returns null", () => Old.Result<int, int>.Ok(1).Map(static _ => (string)null!),
            () => Result<int, int>.Ok(1).Map(static _ => (string)null!));

        var oldAsync = await Old.ResultAsyncExtensions.MapAsync(Old.Result<int, int>.Ok(1), static _ => ValueTask.FromResult<string>(null!));
        VerifyOldViolation("MapAsync returns null", oldAsync);
        try
        {
            await Result<int, int>.Ok(1).MapAsync(static _ => ValueTask.FromResult<string>(null!));
            throw new InvalidOperationException("Current async implementation accepted null.");
        }
        catch (ArgumentNullException error) when (error.ParamName == "value")
        {
            Console.WriteLine("PASS Current MapAsync: ArgumentNullException(value)");
        }

        Result<int, string> currentDefault = default(Ok<int>);
        Old.Result<int, string> oldDefault = default(Old.Ok<int>);
        if (currentDefault.Unwrap() != 0 || oldDefault.Unwrap() != 0)
        {
            throw new InvalidOperationException("Valid default value type was changed.");
        }

        Console.WriteLine("PASS Valid value-type default remains Ok(0) in both versions.");
    }

    private static void Check(string name, Func<Old.Result<string, int>> before, Func<Result<string, int>> after)
    {
        VerifyOldViolation(name, before());
        try
        {
            after();
            throw new InvalidOperationException($"Current {name} accepted null.");
        }
        catch (ArgumentNullException error) when (error.ParamName == "value")
        {
            Console.WriteLine($"PASS Current {name}: ArgumentNullException(value)");
        }
    }

    private static void VerifyOldViolation(string name, Old.Result<string, int> value)
    {
        if (!value.TryGetOk(out var success) || success is not null)
        {
            throw new InvalidOperationException($"Failed to reproduce original {name} null contract violation.");
        }

        Console.WriteLine($"REPRO Original {name}: TryGetOk returns true with null (violates MaybeNullWhen(false)).");
        try
        {
            value.ToOption();
            throw new InvalidOperationException("Original ToOption unexpectedly accepted the inconsistent value.");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("REPRO Original ToOption then throws for this successful Result.");
        }
    }
}
