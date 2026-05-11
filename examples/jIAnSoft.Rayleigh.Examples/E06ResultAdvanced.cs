using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record Config(string Key, string Value);

public static class E06ResultAdvanced
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E06 - Result Advanced: Recovery, Side Effects, Unwrapping, and More");

        OrAndOrElseDemo();
        TapAndTapErrDemo();
        TryGetOkAndTryGetErrDemo();
        UnwrapFamilyDemo();
        ToOptionAndErrDemo();
        DeconstructDemo();
        UnitTypeDemo();
        Pitfalls();
    }

    private static void OrAndOrElseDemo()
    {
        ConsoleHelper.PrintSubSection("1. Or / OrElse — fallback on error");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("primary failed");
        var fallback = Result<int, string>.Ok(999);

        // Or: if Err, return the alternative (eagerly evaluated)
        ConsoleHelper.PrintResult("Ok(42).Or(fallback)", ok.Or(fallback));
        ConsoleHelper.PrintResult("Err.Or(Ok(999))", err.Or(fallback));

        // OrElse: if Err, run a factory that receives the error (lazy evaluation)
        var recovered = err.OrElse(e =>
        {
            ConsoleHelper.PrintResult("  OrElse received error", e);
            return Result<int, string>.Ok(-1);
        });
        ConsoleHelper.PrintResult("Err.OrElse(e => Ok(-1))", recovered);

        // OrElse is NOT called when the result is already Ok
        var skipped = ok.OrElse(e =>
        {
            ConsoleHelper.PrintResult("  OrElse should NOT appear", e);
            return Result<int, string>.Ok(-1);
        });
        ConsoleHelper.PrintResult("Ok(42).OrElse(...) — factory not called", skipped);
    }

    private static void TapAndTapErrDemo()
    {
        ConsoleHelper.PrintSubSection("2. Tap / TapErr — side effects without changing the value");

        // Local helper that returns a file-scoped record
        static Result<Config, string> FetchConfig(string key)
        {
            if (key == "timeout_ms") return new Config("timeout_ms", "3000");
            return $"Config key '{key}' not found";
        }

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("timeout");

        // Tap: executes action on Ok, passes the Result through unchanged
        ConsoleHelper.PrintCorrect("Tap on Ok triggers the action:");
        ok.Tap(v => ConsoleHelper.PrintResult("  Tap saw value", v));

        ConsoleHelper.PrintCorrect("Tap on Err does nothing:");
        err.Tap(v => ConsoleHelper.PrintResult("  Tap should NOT appear", v));

        // TapErr: executes action on Err, passes the Result through unchanged
        ConsoleHelper.PrintCorrect("TapErr on Err triggers the action:");
        err.TapErr(e => ConsoleHelper.PrintResult("  TapErr saw error", e));

        ConsoleHelper.PrintCorrect("TapErr on Ok does nothing:");
        ok.TapErr(e => ConsoleHelper.PrintResult("  TapErr should NOT appear", e));

        // Chaining Tap and TapErr for logging in a pipeline
        ConsoleHelper.PrintCorrect("Chained pipeline with logging:");
        var result = FetchConfig("timeout_ms")
            .Tap(c => ConsoleHelper.PrintResult("  Loaded config", $"{c.Key}={c.Value}"))
            .TapErr(e => ConsoleHelper.PrintResult("  Config error", e))
            .Map(c => int.Parse(c.Value));
        ConsoleHelper.PrintResult("Pipeline result", result);
    }

    private static void TryGetOkAndTryGetErrDemo()
    {
        ConsoleHelper.PrintSubSection("3. TryGetOk / TryGetErr — guard-clause style");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("invalid");

        // TryGetOk (1-param): simple bool + out value
        if (ok.TryGetOk(out var value))
        {
            ConsoleHelper.PrintResult("TryGetOk(out value) on Ok", value);
        }

        if (!err.TryGetOk(out _))
        {
            ConsoleHelper.PrintResult("TryGetOk on Err", "returned false");
        }

        // TryGetOk (2-param): bool + out value + out error — ideal for guard clauses
        ConsoleHelper.PrintCorrect("2-param TryGetOk — clean guard clause pattern:");
        ConsoleHelper.PrintResult("ProcessOrder(Ok(42))", ProcessOrder(ok));
        ConsoleHelper.PrintResult("ProcessOrder(Err(\"invalid\"))", ProcessOrder(err));

        // TryGetErr: extract error value
        if (err.TryGetErr(out var error))
        {
            ConsoleHelper.PrintResult("TryGetErr(out error) on Err", error);
        }

        if (!ok.TryGetErr(out _))
        {
            ConsoleHelper.PrintResult("TryGetErr on Ok", "returned false");
        }
    }

    private static void UnwrapFamilyDemo()
    {
        ConsoleHelper.PrintSubSection("4. Unwrap Family — extracting values");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("failure");

        // Unwrap: get the Ok value or throw
        ConsoleHelper.PrintResult("Ok(42).Unwrap()", ok.Unwrap());

        // UnwrapOr: get the Ok value or a default
        ConsoleHelper.PrintResult("Ok(42).UnwrapOr(0)", ok.UnwrapOr(0));
        ConsoleHelper.PrintResult("Err.UnwrapOr(0)", err.UnwrapOr(0));

        // UnwrapOrElse: get the Ok value or compute a default from the error (lazy)
        var computed = err.UnwrapOrElse(e => e.Length);
        ConsoleHelper.PrintResult("Err(\"failure\").UnwrapOrElse(e => e.Length)", computed);

        // Expect: like Unwrap but with a custom error message
        ConsoleHelper.PrintResult("Ok(42).Expect(\"must exist\")", ok.Expect("must exist"));

        // UnwrapErr: get the Err value or throw (useful in tests)
        ConsoleHelper.PrintResult("Err(\"failure\").UnwrapErr()", err.UnwrapErr());

        // ExpectErr: like UnwrapErr but with a custom message
        ConsoleHelper.PrintResult("Err(\"failure\").ExpectErr(\"should fail\")", err.ExpectErr("should fail"));
    }

    private static void ToOptionAndErrDemo()
    {
        ConsoleHelper.PrintSubSection("5. ToOption / Err() — conversion to Option<T>");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("oops");

        // ToOption: Ok -> Some(value), Err -> None (error is discarded)
        ConsoleHelper.PrintResult("Ok(42).ToOption()", ok.ToOption());
        ConsoleHelper.PrintResult("Err.ToOption()", err.ToOption());

        // Err(): Ok -> None, Err -> Some(error) (value is discarded)
        ConsoleHelper.PrintResult("Ok(42).Err()", ok.Err());
        ConsoleHelper.PrintResult("Err(\"oops\").Err()", err.Err());

        ConsoleHelper.PrintCorrect("ToOption keeps only the success path; .Err() keeps only the error path");
    }

    private static void DeconstructDemo()
    {
        ConsoleHelper.PrintSubSection("6. Deconstruct — pattern matching with var (isOk, value, error)");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("not found");

        // Tuple deconstruction
        var (isOk1, val1, err1) = ok;
        ConsoleHelper.PrintResult("var (isOk, val, err) = Ok(42)",
            $"isOk={isOk1}, val={val1}, err={err1}");

        var (isOk2, val2, err2) = err;
        ConsoleHelper.PrintResult("var (isOk, val, err) = Err(\"not found\")",
            $"isOk={isOk2}, val={val2}, err={err2}");

        // Switch expression with deconstruction
        string[] testInputs = ["42", "abc", "-1"];
        foreach (var input in testInputs)
        {
            var result = ParsePositive(input);
            var message = result switch
            {
                (true, var v, _) => $"Success: {v}",
                (false, _, var e) => $"Failed: {e}"
            };
            ConsoleHelper.PrintResult($"ParsePositive(\"{input}\") switch", message);
        }

        // is pattern matching
        if (ok is (true, var matched, _))
        {
            ConsoleHelper.PrintResult("ok is (true, var matched, _)", matched);
        }
    }

    private static void UnitTypeDemo()
    {
        ConsoleHelper.PrintSubSection("7. Unit Type — Result<Unit, TE> for void-like success");

        // Some operations succeed without a meaningful return value.
        // Use Result<Unit, string> instead of returning bool or throwing.

        var success = SaveToDatabase("important data");
        ConsoleHelper.PrintResult("SaveToDatabase(\"important data\")", success);

        var failure = SaveToDatabase("");
        ConsoleHelper.PrintResult("SaveToDatabase(\"\")", failure);

        // You can still chain operations on Unit results
        var chained = SaveToDatabase("data")
            .Tap(_ => ConsoleHelper.PrintResult("  Tap on Unit", "save succeeded"))
            .Map(_ => "operation complete");
        ConsoleHelper.PrintResult("SaveToDatabase.Tap.Map", chained);

        // Match works normally
        var message = failure.Match(
            ok: _ => "Saved successfully",
            err: e => $"Save failed: {e}");
        ConsoleHelper.PrintResult("failure.Match", message);

        ConsoleHelper.PrintCorrect("Unit.Value is the single instance — it means 'success with no data'");
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("8. Pitfalls");

        // Pitfall 1: err.Unwrap() throws
        var err = Result<int, string>.Err("something broke");
        ConsoleHelper.CatchAndPrint(
            "Err(\"something broke\").Unwrap()",
            () => err.Unwrap());
        ConsoleHelper.PrintWrong("Unwrap on Err throws InvalidOperationException");
        ConsoleHelper.PrintCorrect("Use UnwrapOr, UnwrapOrElse, Match, or TryGetOk instead");

        // Pitfall 2: ok.UnwrapErr() throws
        var ok = Result<int, string>.Ok(42);
        ConsoleHelper.CatchAndPrint(
            "Ok(42).UnwrapErr()",
            () => ok.UnwrapErr());
        ConsoleHelper.PrintWrong("UnwrapErr on Ok throws — it is meant for testing error paths");

        // Pitfall 3: Verbose guard clause vs clean 2-param TryGetOk
        ConsoleHelper.PrintWrong("Verbose approach — separate checks for value and error:");
        ConsoleHelper.PrintResult("  VerboseGuard(Err)", VerboseGuard(err));

        ConsoleHelper.PrintCorrect("Clean approach — 2-param TryGetOk in a single guard clause:");
        ConsoleHelper.PrintResult("  CleanGuard(Err)", CleanGuard(err));
    }

    // --- Helper methods ---

    private static string ProcessOrder(Result<int, string> orderResult)
    {
        if (!orderResult.TryGetOk(out var orderId, out var error))
        {
            return $"Rejected: {error}";
        }

        return $"Processing order #{orderId}";
    }

    private static Result<int, string> ParsePositive(string input)
    {
        if (!int.TryParse(input, out var n)) return $"'{input}' is not a number";
        if (n < 0) return $"{n} is negative";
        return n;
    }

    private static Result<Unit, string> SaveToDatabase(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return "Data cannot be empty";

        // Simulate a successful save
        return Unit.Value;
    }

    private static string VerboseGuard(Result<int, string> result)
    {
        // Verbose: check IsErr first, then extract error separately
        if (result.IsErr)
        {
            if (result.TryGetErr(out var e))
            {
                return $"Error: {e}";
            }
        }

        return $"Value: {result.Unwrap()}";
    }

    private static string CleanGuard(Result<int, string> result)
    {
        // Clean: one call, one branch, both outputs available
        if (!result.TryGetOk(out var value, out var error))
        {
            return $"Error: {error}";
        }

        return $"Value: {value}";
    }
}
