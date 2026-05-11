using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record AgeRecord(int Age, string Label);

public static class E05ResultTransformations
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E05 - Result Transformations: Map, Bind, Flatten, and Pipelines");

        MapDemo();
        MapErrDemo();
        BindRailwayDemo();
        FlattenDemo();
        MapOrAndMapOrElseDemo();
        PitfallsDemo();
    }

    private static void MapDemo()
    {
        ConsoleHelper.PrintSubSection("1. Map — transform the Ok value, preserve Err");

        var ok = Result<int, string>.Ok(21);
        var err = Result<int, string>.Err("bad input");

        // Map on Ok: applies the function
        var doubled = ok.Map(x => x * 2);
        ConsoleHelper.PrintResult("Ok(21).Map(x => x * 2)", doubled);

        // Map on Err: the function is NOT called; the error passes through
        var errMapped = err.Map(x => x * 2);
        ConsoleHelper.PrintResult("Err(\"bad input\").Map(x => x * 2)", errMapped);

        // Chaining multiple Maps
        var chained = ok
            .Map(x => x * 2)
            .Map(x => x + 10)
            .Map(x => $"final = {x}");
        ConsoleHelper.PrintResult("Ok(21) -> *2 -> +10 -> format", chained);
    }

    private static void MapErrDemo()
    {
        ConsoleHelper.PrintSubSection("2. MapErr — transform the Err value, preserve Ok");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("not found");

        // MapErr on Err: transforms the error
        var tagged = err.MapErr(e => $"[ERROR] {e}");
        ConsoleHelper.PrintResult("Err(\"not found\").MapErr(e => \"[ERROR] \" + e)", tagged);

        // MapErr on Ok: the function is NOT called; the value passes through
        var okPassThrough = ok.MapErr(e => $"[ERROR] {e}");
        ConsoleHelper.PrintResult("Ok(42).MapErr(...)", okPassThrough);

        // Practical: convert error types between layers
        var apiError = err.MapErr(e => new { Code = 404, Message = e });
        ConsoleHelper.PrintResult("MapErr to anonymous object", apiError);
    }

    private static void BindRailwayDemo()
    {
        ConsoleHelper.PrintSubSection("3. Bind — Railway-Oriented Programming");

        // Local helper functions for the pipeline
        static Result<int, string> ParseAge(string input)
        {
            if (int.TryParse(input, out var age)) return age;
            return $"'{input}' is not a valid integer";
        }

        static Result<int, string> ValidateRange(int age)
        {
            if (age is < 0 or > 150) return $"Age {age} is out of valid range (0-150)";
            return age;
        }

        static Result<AgeRecord, string> CreateRecord(int age)
        {
            var label = age switch
            {
                < 13 => "child",
                < 18 => "teenager",
                < 65 => "adult",
                _ => "senior"
            };
            return new AgeRecord(age, label);
        }

        // Define a pipeline of validation steps, each returning Result
        ConsoleHelper.PrintCorrect("Chain: ParseAge -> ValidateRange -> CreateRecord");

        // Success path: all steps pass
        var success = ParseAge("25")
            .Bind(ValidateRange)
            .Bind(CreateRecord);
        ConsoleHelper.PrintResult("\"25\" through the pipeline", success);

        // Failure at step 1: parse fails
        var failParse = ParseAge("abc")
            .Bind(ValidateRange)
            .Bind(CreateRecord);
        ConsoleHelper.PrintResult("\"abc\" through the pipeline", failParse);

        // Failure at step 2: out of range
        var failRange = ParseAge("200")
            .Bind(ValidateRange)
            .Bind(CreateRecord);
        ConsoleHelper.PrintResult("\"200\" through the pipeline", failRange);

        ConsoleHelper.PrintCorrect("Each Bind short-circuits on error — no unnecessary work is done");
    }

    private static void FlattenDemo()
    {
        ConsoleHelper.PrintSubSection("4. Flatten — unwrap nested Result<Result<T,E>,E>");

        static Result<int, string> ValidatePositive(int n)
        {
            if (n > 0) return n;
            return "must be positive";
        }

        // Ok(Ok(42)) -> Ok(42)
        var nestedOk = Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(42));
        ConsoleHelper.PrintResult("Ok(Ok(42)).Flatten()", nestedOk.Flatten());

        // Ok(Err("inner")) -> Err("inner")
        var nestedInnerErr = Result<Result<int, string>, string>.Ok(Result<int, string>.Err("inner error"));
        ConsoleHelper.PrintResult("Ok(Err(\"inner error\")).Flatten()", nestedInnerErr.Flatten());

        // Err("outer") -> Err("outer")
        var nestedOuterErr = Result<Result<int, string>, string>.Err("outer error");
        ConsoleHelper.PrintResult("Err(\"outer error\").Flatten()", nestedOuterErr.Flatten());

        // Practical: Map produces nested Result, Flatten removes one layer
        var source = Result<int, string>.Ok(10);
        var nested = source.Map(ValidatePositive);
        var flat = nested.Flatten();
        ConsoleHelper.PrintResult("Ok(10).Map(ValidatePositive).Flatten()", flat);
        ConsoleHelper.PrintCorrect("Flatten() is equivalent to Bind(x => x). Use Bind directly when possible.");
    }

    private static void MapOrAndMapOrElseDemo()
    {
        ConsoleHelper.PrintSubSection("5. MapOr / MapOrElse — transform with fallback");

        var ok = Result<int, string>.Ok(5);
        var err = Result<int, string>.Err("missing");

        // MapOr: apply mapper on Ok, return default on Err
        var okMapped = ok.MapOr("N/A", v => $"count = {v}");
        var errMapped = err.MapOr("N/A", v => $"count = {v}");
        ConsoleHelper.PrintResult("Ok(5).MapOr(\"N/A\", v => ...)", okMapped);
        ConsoleHelper.PrintResult("Err.MapOr(\"N/A\", ...)", errMapped);

        // MapOrElse: apply mapper on Ok, apply fallback on Err (lazy)
        var okElse = ok.MapOrElse(
            e => $"Error: {e}",
            v => $"Value: {v}");
        var errElse = err.MapOrElse(
            e => $"Error: {e}",
            v => $"Value: {v}");
        ConsoleHelper.PrintResult("Ok(5).MapOrElse(errFn, okFn)", okElse);
        ConsoleHelper.PrintResult("Err.MapOrElse(errFn, okFn)", errElse);

        ConsoleHelper.PrintCorrect("MapOrElse is similar to Match but may feel more natural in data pipelines");
    }

    private static void PitfallsDemo()
    {
        ConsoleHelper.PrintSubSection("6. Pitfalls");

        // Local helpers for demonstration
        static Result<int, string> ParseAge(string input)
        {
            if (int.TryParse(input, out var age)) return age;
            return $"'{input}' is not a valid integer";
        }

        static Result<int, string> ValidateRange(int age)
        {
            if (age is < 0 or > 150) return $"Age {age} is out of valid range (0-150)";
            return age;
        }

        static Result<AgeRecord, string> CreateRecord(int age)
        {
            var label = age switch
            {
                < 13 => "child",
                < 18 => "teenager",
                < 65 => "adult",
                _ => "senior"
            };
            return new AgeRecord(age, label);
        }

        static string TryParseAndValidateWithExceptions(string input)
        {
            try
            {
                var age = int.Parse(input);
                if (age is < 0 or > 150) throw new ArgumentOutOfRangeException(nameof(input));
                return $"Ok({age})";
            }
            catch (FormatException)
            {
                return "Caught FormatException";
            }
            catch (ArgumentOutOfRangeException)
            {
                return "Caught ArgumentOutOfRangeException";
            }
        }

        // Pitfall 1: Exception-based control flow vs Result chains
        ConsoleHelper.PrintWrong("Exception-based control flow — uses try/catch for expected business errors:");
        ConsoleHelper.PrintResult("try { ParseAndValidate(\"abc\") }",
            TryParseAndValidateWithExceptions("abc"));

        ConsoleHelper.PrintCorrect("Result chain — composable, no exception overhead:");
        var resultChain = ParseAge("abc")
            .Bind(ValidateRange)
            .Bind(CreateRecord);
        ConsoleHelper.PrintResult("ParseAge(\"abc\").Bind(ValidateRange).Bind(CreateRecord)", resultChain);

        // Pitfall 2: Silently ignoring errors with UnwrapOr
        ConsoleHelper.PrintWrong("Silently swallowing errors: err.UnwrapOr(defaultVal) hides what went wrong");

        var err = Result<int, string>.Err("database connection failed");
        var silent = err.UnwrapOr(0);
        ConsoleHelper.PrintResult("err.UnwrapOr(0)", silent);

        ConsoleHelper.PrintCorrect("Use TapErr to log before providing a fallback:");
        var logged = err
            .TapErr(e => ConsoleHelper.PrintResult("  TapErr logged", e))
            .UnwrapOr(0);
        ConsoleHelper.PrintResult("err.TapErr(log).UnwrapOr(0)", logged);
    }
}
