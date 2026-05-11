using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

public static class E11CommonPitfalls
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E11 - Common Pitfalls: 12 Mistakes to Avoid with Option & Result");

        Pitfall01_UnwrapWithoutChecking();
        Pitfall02_DefaultOptionConfusion();
        Pitfall03_DefaultResultIsPoisoned();
        Pitfall04_VerboseFactoryWhenImplicitWorks();
        Pitfall05_SameTAndTEWithImplicit();
        Pitfall06_NestedIfElseInsteadOfChains();
        Pitfall07_MapProducingNestedWrappers();
        Pitfall08_ExceptionBasedControlFlow();
        Pitfall09_OptionWhenErrorInfoNeeded();
        Pitfall10_SilentlySwallowingErrors();
        Pitfall11_DiscardingErrorInfoTooEarly();
        Pitfall12_NotUsingTwoOutputTryGetOk();
    }

    // -------------------------------------------------------
    // Pitfall 1: Unwrap on None/Err without checking
    // -------------------------------------------------------

    private static void Pitfall01_UnwrapWithoutChecking()
    {
        ConsoleHelper.PrintSubSection("Pitfall 1: Unwrap on None/Err without checking");

        var none = Option<int>.None;
        var err = Result<int, string>.Err("not found");

        // WRONG: Unwrap on None throws InvalidOperationException
        ConsoleHelper.PrintWrong("Calling Unwrap() on None or Err throws at runtime:");
        ConsoleHelper.CatchAndPrint("none.Unwrap()", () => none.Unwrap());
        ConsoleHelper.CatchAndPrint("err.Unwrap()", () => err.Unwrap());

        // RIGHT: Use safe extraction methods
        ConsoleHelper.PrintCorrect("Use TryGetValue / Match / UnwrapOr instead:");

        if (none.TryGetValue(out var val))
        {
            ConsoleHelper.PrintResult("  TryGetValue", val);
        }
        else
        {
            ConsoleHelper.PrintResult("  TryGetValue returned false", "safely handled");
        }

        var safe = none.Match(
            some: v => $"Got {v}",
            none: () => "No value -- handled gracefully");
        ConsoleHelper.PrintResult("  Match result", safe);

        var fallback = err.UnwrapOr(-1);
        ConsoleHelper.PrintResult("  err.UnwrapOr(-1)", fallback);
    }

    // -------------------------------------------------------
    // Pitfall 2: default(Option<T>) confusion
    // -------------------------------------------------------

    private static void Pitfall02_DefaultOptionConfusion()
    {
        ConsoleHelper.PrintSubSection("Pitfall 2: default(Option<T>) confusion");

        // WRONG: using default(Option<T>) -- it works, but intent is unclear
        ConsoleHelper.PrintWrong("default(Option<T>) is a valid None, but the intent is ambiguous:");
        var fromDefault = default(Option<int>);
        var fromNone = Option<int>.None;
        ConsoleHelper.PrintResult("  default(Option<int>).IsNone", fromDefault.IsNone);
        ConsoleHelper.PrintResult("  default(Option<int>) == Option<int>.None", fromDefault == fromNone);
        ConsoleHelper.PrintResult("  Readers may wonder: was this intentional?", "ambiguous");

        // RIGHT: use Option<T>.None explicitly
        ConsoleHelper.PrintCorrect("Always use Option<T>.None to express intent clearly:");
        var explicit_ = Option<int>.None;
        ConsoleHelper.PrintResult("  Option<int>.None.IsNone", explicit_.IsNone);
        ConsoleHelper.PrintResult("  The code clearly communicates: no value", "clear intent");
    }

    // -------------------------------------------------------
    // Pitfall 3: default(Result<T,E>) is poisoned
    // -------------------------------------------------------

    private static void Pitfall03_DefaultResultIsPoisoned()
    {
        ConsoleHelper.PrintSubSection("Pitfall 3: default(Result<T,E>) is poisoned");

        // WRONG: default(Result<T,E>) is an uninitialized struct
        ConsoleHelper.PrintWrong("default(Result<T,E>) is uninitialized -- any property access throws:");
        var poisoned = default(Result<int, string>);
        ConsoleHelper.CatchAndPrint("poisoned.IsOk", () => _ = poisoned.IsOk);
        ConsoleHelper.CatchAndPrint("poisoned.Unwrap()", () => poisoned.Unwrap());
        ConsoleHelper.CatchAndPrint("poisoned.Map(x => x)", () => poisoned.Map(x => x));

        // RIGHT: always construct via Ok() or Err()
        ConsoleHelper.PrintCorrect("Always construct via .Ok(), .Err(), or implicit conversion:");
        var ok = Result<int, string>.Ok(42);
        Result<int, string> implicitOk = 42;
        Result<int, string> implicitErr = "error";
        ConsoleHelper.PrintResult("  Result<int, string>.Ok(42)", ok);
        ConsoleHelper.PrintResult("  Result<int, string> x = 42", implicitOk);
        ConsoleHelper.PrintResult("  Result<int, string> x = \"error\"", implicitErr);
    }

    // -------------------------------------------------------
    // Pitfall 4: Verbose factory when implicit works
    // -------------------------------------------------------

    private static void Pitfall04_VerboseFactoryWhenImplicitWorks()
    {
        ConsoleHelper.PrintSubSection("Pitfall 4: Verbose factory when implicit conversion works");

        // WRONG: overly verbose return statements
        ConsoleHelper.PrintWrong("Verbose: return Result<int, string>.Ok(value);");
        ConsoleHelper.PrintResult("  DivideVerbose(10, 2)", DivideVerbose(10, 2));
        ConsoleHelper.PrintResult("  DivideVerbose(10, 0)", DivideVerbose(10, 0));

        // RIGHT: use implicit conversions -- just return the value or error
        ConsoleHelper.PrintCorrect("Concise: return value; or return \"error\";");
        ConsoleHelper.PrintResult("  DivideClean(10, 2)", DivideClean(10, 2));
        ConsoleHelper.PrintResult("  DivideClean(10, 0)", DivideClean(10, 0));
    }

    // Verbose version
    private static Result<int, string> DivideVerbose(int a, int b)
    {
        if (b == 0) return Result<int, string>.Err("Division by zero");
        return Result<int, string>.Ok(a / b);
    }

    // Clean version using implicit conversions
    private static Result<int, string> DivideClean(int a, int b)
    {
        if (b == 0) return "Division by zero";
        return a / b;
    }

    // -------------------------------------------------------
    // Pitfall 5: Same T and TE with implicit conversion
    // -------------------------------------------------------

    private static void Pitfall05_SameTAndTEWithImplicit()
    {
        ConsoleHelper.PrintSubSection("Pitfall 5: Same T and TE type causes implicit conversion ambiguity");

        // WRONG: When T == TE (e.g., Result<string, string>), implicit conversion is ambiguous
        ConsoleHelper.PrintWrong("Result<string, string> r = \"hello\"; -- compiler error (ambiguous)");
        ConsoleHelper.PrintResult("  This would not compile", "CS0457: ambiguous implicit conversion");

        // RIGHT: Use Ok<T> / Err<TE> wrapper records to disambiguate
        ConsoleHelper.PrintCorrect("Use new Ok<string>(...) / new Err<string>(...) to be explicit:");
        Result<string, string> explicitOk = new Ok<string>("success value");
        Result<string, string> explicitErr = new Err<string>("error message");
        ConsoleHelper.PrintResult("  new Ok<string>(\"success value\")", explicitOk);
        ConsoleHelper.PrintResult("  new Err<string>(\"error message\")", explicitErr);

        // Practical example: a method returning Result<string, string>
        ConsoleHelper.PrintResult("  Greet(\"Alice\")", Greet("Alice"));
        ConsoleHelper.PrintResult("  Greet(\"\")", Greet(""));
    }

    private static Result<string, string> Greet(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Err<string>("Name cannot be empty");
        return new Ok<string>($"Hello, {name}!");
    }

    // -------------------------------------------------------
    // Pitfall 6: Nested if/else instead of chains
    // -------------------------------------------------------

    private static void Pitfall06_NestedIfElseInsteadOfChains()
    {
        ConsoleHelper.PrintSubSection("Pitfall 6: Nested if/else instead of fluent chains");

        // WRONG: arrow-shaped code with nested if/else
        ConsoleHelper.PrintWrong("Arrow-shaped imperative code:");
        ConsoleHelper.PrintResult("  ProcessArrowShaped(\"42\")", ProcessArrowShaped("42"));
        ConsoleHelper.PrintResult("  ProcessArrowShaped(\"abc\")", ProcessArrowShaped("abc"));
        ConsoleHelper.PrintResult("  ProcessArrowShaped(\"-5\")", ProcessArrowShaped("-5"));

        // RIGHT: flat Map/Bind chain
        ConsoleHelper.PrintCorrect("Flat fluent chain with Map/Bind:");
        ConsoleHelper.PrintResult("  ProcessFluent(\"42\")", ProcessFluent("42"));
        ConsoleHelper.PrintResult("  ProcessFluent(\"abc\")", ProcessFluent("abc"));
        ConsoleHelper.PrintResult("  ProcessFluent(\"-5\")", ProcessFluent("-5"));
    }

    // Arrow-shaped: nested ifs
    private static string ProcessArrowShaped(string input)
    {
        if (int.TryParse(input, out var n))
        {
            if (n > 0)
            {
                if (n <= 100)
                {
                    return $"Ok: {n * 2}";
                }
                else
                {
                    return "Error: exceeds 100";
                }
            }
            else
            {
                return "Error: not positive";
            }
        }
        else
        {
            return "Error: not a number";
        }
    }

    // Fluent: Map/Bind chain
    private static string ProcessFluent(string input)
    {
        static Result<int, string> Parse(string s) =>
            int.TryParse(s, out var n) ? n : $"'{s}' is not a number";

        static Result<int, string> Positive(int n) =>
            n > 0 ? n : "not positive";

        static Result<int, string> InRange(int n) =>
            n <= 100 ? n : "exceeds 100";

        return Parse(input)
            .Bind(Positive)
            .Bind(InRange)
            .Map(n => n * 2)
            .Match(
                ok: v => $"Ok: {v}",
                err: e => $"Error: {e}");
    }

    // -------------------------------------------------------
    // Pitfall 7: Map producing nested wrappers
    // -------------------------------------------------------

    private static void Pitfall07_MapProducingNestedWrappers()
    {
        ConsoleHelper.PrintSubSection("Pitfall 7: Map producing nested wrappers (use Bind instead)");

        // Helper that returns Option
        static Option<int> TryParseOption(string s) =>
            int.TryParse(s, out var n) ? Option<int>.Some(n) : Option<int>.None;

        var input = Option<string>.Some("42");

        // WRONG: Map with a function that returns Option -> nested Option<Option<int>>
        ConsoleHelper.PrintWrong("option.Map(TryParse) produces Option<Option<int>> -- nested:");
        var nested = input.Map(TryParseOption);
        ConsoleHelper.PrintResult("  input.Map(TryParse)", nested);
        ConsoleHelper.PrintResult("  Type is Option<Option<int>>", "not what you want");

        // RIGHT: Use Bind to keep it flat
        ConsoleHelper.PrintCorrect("Use Bind instead -- it flattens automatically:");
        var flat = input.Bind(TryParseOption);
        ConsoleHelper.PrintResult("  input.Bind(TryParse)", flat);

        // Also correct: Map + Flatten
        ConsoleHelper.PrintCorrect("Or use Map + Flatten (equivalent to Bind):");
        var flattened = input.Map(TryParseOption).Flatten();
        ConsoleHelper.PrintResult("  input.Map(TryParse).Flatten()", flattened);
    }

    // -------------------------------------------------------
    // Pitfall 8: Exception-based control flow
    // -------------------------------------------------------

    private static void Pitfall08_ExceptionBasedControlFlow()
    {
        ConsoleHelper.PrintSubSection("Pitfall 8: Exception-based control flow for expected errors");

        // WRONG: using try/catch for expected business errors
        ConsoleHelper.PrintWrong("try/catch for business validation -- expensive and unclear:");
        ConsoleHelper.PrintResult("  ValidateWithException(\"abc\")", ValidateWithException("abc"));
        ConsoleHelper.PrintResult("  ValidateWithException(\"-5\")", ValidateWithException("-5"));
        ConsoleHelper.PrintResult("  ValidateWithException(\"42\")", ValidateWithException("42"));

        // RIGHT: use Result chains
        ConsoleHelper.PrintCorrect("Result chain -- zero-cost, composable, and explicit:");
        ConsoleHelper.PrintResult("  ValidateWithResult(\"abc\")", ValidateWithResult("abc"));
        ConsoleHelper.PrintResult("  ValidateWithResult(\"-5\")", ValidateWithResult("-5"));
        ConsoleHelper.PrintResult("  ValidateWithResult(\"42\")", ValidateWithResult("42"));
    }

    private static string ValidateWithException(string input)
    {
        try
        {
            var n = int.Parse(input);
            if (n <= 0) throw new ArgumentException("Must be positive");
            return $"Ok({n})";
        }
        catch (FormatException)
        {
            return "Caught FormatException";
        }
        catch (ArgumentException ex)
        {
            return $"Caught ArgumentException: {ex.Message}";
        }
    }

    private static string ValidateWithResult(string input)
    {
        static Result<int, string> Parse(string s) =>
            int.TryParse(s, out var n) ? n : $"'{s}' is not a number";

        static Result<int, string> Positive(int n) =>
            n > 0 ? n : "must be positive";

        return Parse(input)
            .Bind(Positive)
            .Match(ok: v => $"Ok({v})", err: e => $"Error: {e}");
    }

    // -------------------------------------------------------
    // Pitfall 9: Option when error info is needed
    // -------------------------------------------------------

    private static void Pitfall09_OptionWhenErrorInfoNeeded()
    {
        ConsoleHelper.PrintSubSection("Pitfall 9: Using Option when error information is needed");

        // WRONG: Option hides the reason for failure
        ConsoleHelper.PrintWrong("Option<User> -- was it 'not found'? 'permission denied'? 'timeout'?");

        var notFound = Option<string>.None;
        ConsoleHelper.PrintResult("  FindUserOption(999)", notFound);
        ConsoleHelper.PrintResult("  Caller only sees None", "no idea why it failed");

        // RIGHT: Result carries the error context
        ConsoleHelper.PrintCorrect("Result<User, string> -- the error explains what went wrong:");

        var err1 = Result<string, string>.Err("User not found");
        var err2 = Result<string, string>.Err("Permission denied");
        var err3 = Result<string, string>.Err("Database connection timeout");
        ConsoleHelper.PrintResult("  FindUserResult(999) scenario 1", err1);
        ConsoleHelper.PrintResult("  FindUserResult(999) scenario 2", err2);
        ConsoleHelper.PrintResult("  FindUserResult(999) scenario 3", err3);
        ConsoleHelper.PrintCorrect("Use Option for 'present or absent'; use Result when the caller needs to know WHY");
    }

    // -------------------------------------------------------
    // Pitfall 10: Silently swallowing errors with UnwrapOr
    // -------------------------------------------------------

    private static void Pitfall10_SilentlySwallowingErrors()
    {
        ConsoleHelper.PrintSubSection("Pitfall 10: Silently swallowing errors with UnwrapOr");

        var err = Result<int, string>.Err("database connection failed");

        // WRONG: UnwrapOr with no logging -- error is silently lost
        ConsoleHelper.PrintWrong(".UnwrapOr(0) silently hides the error -- debugging nightmare:");
        var silent = err.UnwrapOr(0);
        ConsoleHelper.PrintResult("  err.UnwrapOr(0)", silent);
        ConsoleHelper.PrintResult("  Error was", "silently discarded -- nobody knows what happened");

        // RIGHT: TapErr to log before providing a fallback
        ConsoleHelper.PrintCorrect("Use TapErr to log, then UnwrapOr for the fallback:");
        var logged = err
            .TapErr(e => ConsoleHelper.PrintResult("  [LOG] TapErr caught", e))
            .UnwrapOr(0);
        ConsoleHelper.PrintResult("  err.TapErr(log).UnwrapOr(0)", logged);
        ConsoleHelper.PrintCorrect("The error is logged for observability, then a safe default is used");
    }

    // -------------------------------------------------------
    // Pitfall 11: Discarding error info too early
    // -------------------------------------------------------

    private static void Pitfall11_DiscardingErrorInfoTooEarly()
    {
        ConsoleHelper.PrintSubSection("Pitfall 11: Discarding error info too early with .ToOption()");

        static Result<int, string> Validate(string input)
        {
            if (!int.TryParse(input, out var n)) return $"'{input}' is not a number";
            if (n <= 0) return $"{n} must be positive";
            return n;
        }

        // WRONG: ToOption mid-pipeline loses the error
        ConsoleHelper.PrintWrong(".ToOption() mid-pipeline discards the error reason:");
        var lostError = Validate("abc")
            .ToOption()           // Error info is gone!
            .Map(n => n * 2);
        ConsoleHelper.PrintResult("  Validate(\"abc\").ToOption().Map(n => n * 2)", lostError);
        ConsoleHelper.PrintResult("  We got None, but why?", "error was discarded by ToOption()");

        // RIGHT: keep as Result through the pipeline, convert at the end
        ConsoleHelper.PrintCorrect("Keep Result through the pipeline -- convert only at the boundary:");
        var preserved = Validate("abc")
            .Map(n => n * 2);
        ConsoleHelper.PrintResult("  Validate(\"abc\").Map(n => n * 2)", preserved);
        ConsoleHelper.PrintResult("  Error is preserved", "Err(\"'abc' is not a number\")");

        // Only convert to Option at the very end, when error info is no longer needed
        ConsoleHelper.PrintCorrect("Convert to Option only at the final boundary if needed:");
        var final_ = Validate("abc").Map(n => n * 2).ToOption();
        ConsoleHelper.PrintResult("  ...pipeline complete, then .ToOption()", final_);
    }

    // -------------------------------------------------------
    // Pitfall 12: Not using 2-output TryGetOk
    // -------------------------------------------------------

    private static void Pitfall12_NotUsingTwoOutputTryGetOk()
    {
        ConsoleHelper.PrintSubSection("Pitfall 12: Not using 2-output TryGetOk for guard clauses");

        var okResult = Result<int, string>.Ok(42);
        var errResult = Result<int, string>.Err("validation failed");

        // WRONG: Separate IsErr + UnwrapErr -- verbose and fragile
        ConsoleHelper.PrintWrong("Separate checks: IsErr then TryGetErr -- verbose:");
        ConsoleHelper.PrintResult("  HandleVerbose(Ok(42))", HandleVerbose(okResult));
        ConsoleHelper.PrintResult("  HandleVerbose(Err(...))", HandleVerbose(errResult));

        // RIGHT: 2-param TryGetOk -- one call, both outputs
        ConsoleHelper.PrintCorrect("2-param TryGetOk -- clean guard clause, one call:");
        ConsoleHelper.PrintResult("  HandleClean(Ok(42))", HandleClean(okResult));
        ConsoleHelper.PrintResult("  HandleClean(Err(...))", HandleClean(errResult));
    }

    // Verbose: separate checks
    private static string HandleVerbose(Result<int, string> result)
    {
        if (result.IsErr)
        {
            if (result.TryGetErr(out var e))
            {
                return $"Error: {e}";
            }
        }

        return $"Value: {result.Unwrap()}";
    }

    // Clean: 2-param TryGetOk
    private static string HandleClean(Result<int, string> result)
    {
        if (!result.TryGetOk(out var value, out var error))
        {
            return $"Error: {error}";
        }

        return $"Value: {value}";
    }
}
