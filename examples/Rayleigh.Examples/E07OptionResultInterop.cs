using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record CustomError(string Code, string Message);

public static class E07OptionResultInterop
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E07 - Option / Result Interop: Converting Between Types");

        OptionToResult();
        ResultToOption();
        ResultToOptionError();
        NullableToOptionToResultPipeline();
        CollectingErrors();
        Pitfalls();
    }

    private static void OptionToResult()
    {
        ConsoleHelper.PrintSubSection("1. Option -> Result: ToResult");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // ToResult with a static error value
        var okResult = some.ToResult("not found");
        var errResult = none.ToResult("not found");
        ConsoleHelper.PrintResult("Some(42).ToResult(\"not found\")", okResult);
        ConsoleHelper.PrintResult("None.ToResult(\"not found\")", errResult);

        // ToResult with a lazy error factory — useful when building the error is expensive
        var lazyErr = none.ToResult(() => new CustomError("E404", "Resource not found"));
        ConsoleHelper.PrintResult("None.ToResult(() => new CustomError(...))", lazyErr);

        // Factory is NOT called when the option is Some
        var callCount = 0;
        var lazyOk = some.ToResult(() =>
        {
            callCount++;
            return new CustomError("NEVER", "Should not be created");
        });
        ConsoleHelper.PrintResult("Some(42).ToResult(factory) — factory call count", callCount);
        ConsoleHelper.PrintResult("Some(42).ToResult(factory)", lazyOk);

        ConsoleHelper.PrintCorrect("ToResult bridges the gap from 'maybe absent' to 'maybe failed'");
    }

    private static void ResultToOption()
    {
        ConsoleHelper.PrintSubSection("2. Result -> Option: ToOption (discards error)");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("something went wrong");

        // ToOption: Ok -> Some, Err -> None (error info is lost)
        ConsoleHelper.PrintResult("Ok(42).ToOption()", ok.ToOption());
        ConsoleHelper.PrintResult("Err(\"something went wrong\").ToOption()", err.ToOption());

        ConsoleHelper.PrintCorrect("ToOption keeps only the success value — the error is discarded");
    }

    private static void ResultToOptionError()
    {
        ConsoleHelper.PrintSubSection("3. Result -> Option<Error>: .Err() method");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("validation failed");

        // .Err() extracts the error as an Option
        // Ok -> None (no error to extract)
        // Err -> Some(error)
        ConsoleHelper.PrintResult("Ok(42).Err()", ok.Err());
        ConsoleHelper.PrintResult("Err(\"validation failed\").Err()", err.Err());

        ConsoleHelper.PrintCorrect(".Err() is the mirror of .ToOption() — it keeps only the error path");
    }

    private static void NullableToOptionToResultPipeline()
    {
        ConsoleHelper.PrintSubSection("4. Nullable -> Option -> Result pipeline");

        // Simulating nullable input from external sources (e.g., user input, API response)
        string? validInput = "hello";
        string? nullInput = null;

        // Pipeline: nullable -> ToOption -> ToResult
        var result1 = validInput.ToOption().ToResult("input is required");
        var result2 = nullInput.ToOption().ToResult("input is required");

        ConsoleHelper.PrintResult("\"hello\".ToOption().ToResult(\"input is required\")", result1);
        ConsoleHelper.PrintResult("null.ToOption().ToResult(\"input is required\")", result2);

        // Extended pipeline: nullable -> Option -> Result -> Map
        var processed = validInput
            .ToOption()
            .ToResult("input is required")
            .Map(s => s.ToUpperInvariant());
        ConsoleHelper.PrintResult("Full pipeline: null -> Option -> Result -> Map", processed);

        var failed = nullInput
            .ToOption()
            .ToResult("input is required")
            .Map(s => s.ToUpperInvariant());
        ConsoleHelper.PrintResult("Full pipeline with null input", failed);

        ConsoleHelper.PrintCorrect("ToOption().ToResult() is a clean pattern for validating nullable inputs");
    }

    private static void CollectingErrors()
    {
        ConsoleHelper.PrintSubSection("5. Collecting errors with .Err() and .Values()");

        // Simulate a batch of validation results
        Result<int, string>[] validations =
        [
            Result<int, string>.Ok(1),
            Result<int, string>.Err("name is required"),
            Result<int, string>.Ok(2),
            Result<int, string>.Err("email is invalid"),
            Result<int, string>.Ok(3),
            Result<int, string>.Err("age must be positive"),
        ];

        // Extract all errors: map each result to .Err() (Option<string>), then .Values()
        var errors = validations.Select(r => r.Err()).Values().ToList();
        ConsoleHelper.PrintResult("Total validations", validations.Length);
        ConsoleHelper.PrintResult("Error count", errors.Count);
        foreach (var error in errors)
        {
            ConsoleHelper.PrintResult("  Error", error);
        }

        // Extract all successes: map each result to .ToOption(), then .Values()
        var successes = validations.Select(r => r.ToOption()).Values().ToList();
        ConsoleHelper.PrintResult("Success count", successes.Count);
        ConsoleHelper.PrintResult("Success values", $"[{string.Join(", ", successes)}]");

        ConsoleHelper.PrintCorrect(".Err().Values() collects all error messages from a batch of Results");
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("6. Pitfall: Discarding error info too early with .ToOption()");

        // Scenario: a pipeline that processes user input with specific error messages
        var detailedResult = ValidateAge("abc");
        ConsoleHelper.PrintResult("ValidateAge(\"abc\")", detailedResult);

        // BAD: Converting to Option mid-pipeline loses the specific error reason
        var lostInfo = ValidateAge("abc")
            .ToOption()              // "abc is not a number" is now gone
            .ToResult("unknown error"); // replaced with a generic error
        ConsoleHelper.PrintResult("ValidateAge(\"abc\").ToOption().ToResult(\"unknown error\")", lostInfo);
        ConsoleHelper.PrintWrong("ToOption() mid-pipeline discards the specific error — you only get 'unknown error'");

        // GOOD: Keep the Result flowing through the pipeline
        var preserved = ValidateAge("abc")
            .Map(age => age * 2);  // error is preserved untouched
        ConsoleHelper.PrintResult("ValidateAge(\"abc\").Map(age => age * 2)", preserved);
        ConsoleHelper.PrintCorrect("Keep errors in the Result pipeline — only call ToOption() at the very end if needed");
    }

    // --- Helper methods ---

    private static Result<int, string> ValidateAge(string input)
    {
        if (!int.TryParse(input, out var age)) return $"'{input}' is not a number";
        if (age < 0) return $"{age} is negative";
        if (age > 150) return $"{age} is unrealistically large";
        return age;
    }
}
