using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

public static class E04ResultBasics
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E04 - Result Basics: Creating, Checking, and Comparing");

        CreatingResults();
        StateChecks();
        ContainsAndContainsErr();
        IsOkAndIsErrAnd();
        EqualityComparisons();
        Pitfalls();
    }

    private static void CreatingResults()
    {
        ConsoleHelper.PrintSubSection("1. Creating Results");

        // Factory methods: Result<T, TE>.Ok(value) and Result<T, TE>.Err(error)
        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("something went wrong");
        ConsoleHelper.PrintResult("Result<int, string>.Ok(42)", ok);
        ConsoleHelper.PrintResult("Result<int, string>.Err(\"something went wrong\")", err);

        // Implicit conversion from T -> Ok
        Result<int, string> implicitOk = 100;
        ConsoleHelper.PrintResult("Result<int, string> implicitOk = 100", implicitOk);

        // Implicit conversion from TE -> Err
        Result<int, string> implicitErr = "network timeout";
        ConsoleHelper.PrintResult("Result<int, string> implicitErr = \"network timeout\"", implicitErr);

        // Ok<T> / Err<TE> wrapper records (useful when T and TE are the same type)
        Result<int, string> wrapperOk = new Ok<int>(200);
        Result<int, string> wrapperErr = new Err<string>("wrapper error");
        ConsoleHelper.PrintResult("new Ok<int>(200)", wrapperOk);
        ConsoleHelper.PrintResult("new Err<string>(\"wrapper error\")", wrapperErr);

        // Implicit conversion in a method return — concise and readable
        ConsoleHelper.PrintResult("Divide(10, 2)", Divide(10, 2));
        ConsoleHelper.PrintResult("Divide(10, 0)", Divide(10, 0));
    }

    private static void StateChecks()
    {
        ConsoleHelper.PrintSubSection("2. State Checks: IsOk / IsErr");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("fail");

        ConsoleHelper.PrintResult("ok.IsOk", ok.IsOk);
        ConsoleHelper.PrintResult("ok.IsErr", ok.IsErr);
        ConsoleHelper.PrintResult("err.IsOk", err.IsOk);
        ConsoleHelper.PrintResult("err.IsErr", err.IsErr);
    }

    private static void ContainsAndContainsErr()
    {
        ConsoleHelper.PrintSubSection("3. Contains / ContainsErr");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("not found");

        // Contains: checks if the result is Ok AND holds a specific value
        ConsoleHelper.PrintResult("ok.Contains(42)", ok.Contains(42));
        ConsoleHelper.PrintResult("ok.Contains(99)", ok.Contains(99));
        ConsoleHelper.PrintResult("err.Contains(42)", err.Contains(42));

        // ContainsErr: checks if the result is Err AND holds a specific error
        ConsoleHelper.PrintResult("err.ContainsErr(\"not found\")", err.ContainsErr("not found"));
        ConsoleHelper.PrintResult("err.ContainsErr(\"timeout\")", err.ContainsErr("timeout"));
        ConsoleHelper.PrintResult("ok.ContainsErr(\"not found\")", ok.ContainsErr("not found"));
    }

    private static void IsOkAndIsErrAnd()
    {
        ConsoleHelper.PrintSubSection("4. IsOkAnd / IsErrAnd");

        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("file not found");

        // IsOkAnd: Ok AND the predicate on the value is true
        ConsoleHelper.PrintResult("ok.IsOkAnd(x => x > 40)", ok.IsOkAnd(x => x > 40));
        ConsoleHelper.PrintResult("ok.IsOkAnd(x => x > 100)", ok.IsOkAnd(x => x > 100));
        ConsoleHelper.PrintResult("err.IsOkAnd(x => x > 0)", err.IsOkAnd(x => x > 0));
        ConsoleHelper.PrintCorrect("IsOkAnd on Err always returns false — the predicate is never called");

        // IsErrAnd: Err AND the predicate on the error is true
        ConsoleHelper.PrintResult("err.IsErrAnd(e => e.Contains(\"not found\"))",
            err.IsErrAnd(e => e.Contains("not found")));
        ConsoleHelper.PrintResult("err.IsErrAnd(e => e.Contains(\"timeout\"))",
            err.IsErrAnd(e => e.Contains("timeout")));
        ConsoleHelper.PrintResult("ok.IsErrAnd(e => e.Contains(\"not found\"))",
            ok.IsErrAnd(e => e.Contains("not found")));
        ConsoleHelper.PrintCorrect("IsErrAnd on Ok always returns false — the predicate is never called");
    }

    private static void EqualityComparisons()
    {
        ConsoleHelper.PrintSubSection("5. Equality");

        var a = Result<int, string>.Ok(42);
        var b = Result<int, string>.Ok(42);
        var c = Result<int, string>.Ok(99);
        var e1 = Result<int, string>.Err("error");
        var e2 = Result<int, string>.Err("error");
        var e3 = Result<int, string>.Err("different");

        ConsoleHelper.PrintResult("Ok(42) == Ok(42)", a == b);
        ConsoleHelper.PrintResult("Ok(42) == Ok(99)", a == c);
        ConsoleHelper.PrintResult("Ok(42) == Err(\"error\")", a == e1);
        ConsoleHelper.PrintResult("Err(\"error\") == Err(\"error\")", e1 == e2);
        ConsoleHelper.PrintResult("Err(\"error\") == Err(\"different\")", e1 == e3);

        // HashCode consistency
        ConsoleHelper.PrintResult("Ok(42).GetHashCode() == Ok(42).GetHashCode()",
            a.GetHashCode() == b.GetHashCode());

        // ToString for debugging
        ConsoleHelper.PrintResult("Ok(42).ToString()", a.ToString());
        ConsoleHelper.PrintResult("Err(\"error\").ToString()", e1.ToString());
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("6. Pitfalls");

        // Pitfall 1: Verbose factory calls in return statements
        ConsoleHelper.PrintWrong("Verbose: return Result<int, string>.Ok(value);");
        ConsoleHelper.PrintCorrect("Use implicit conversion: return value;  (the compiler infers Ok)");
        ConsoleHelper.PrintResult("Divide(10, 3) using implicit returns", Divide(10, 3));

        // Pitfall 2: default(Result<T,TE>) is poisoned
        var poisoned = default(Result<int, string>);
        ConsoleHelper.CatchAndPrint(
            "default(Result<int, string>).IsOk",
            () => _ = poisoned.IsOk);
        ConsoleHelper.PrintWrong("default(Result<T,TE>) is an uninitialized struct — any property access throws");
        ConsoleHelper.PrintCorrect("Always construct via .Ok() / .Err() / implicit conversion");

        // Pitfall 3: When T and TE are the same type, implicit conversion is ambiguous
        ConsoleHelper.PrintWrong("Result<string, string> r = \"hello\"; — compiler cannot decide Ok or Err");
        ConsoleHelper.PrintCorrect("Use explicit wrappers: new Ok<string>(\"hello\") / new Err<string>(\"oops\")");

        Result<string, string> explicitOk = new Ok<string>("success value");
        Result<string, string> explicitErr = new Err<string>("error message");
        ConsoleHelper.PrintResult("new Ok<string>(\"success value\")", explicitOk);
        ConsoleHelper.PrintResult("new Err<string>(\"error message\")", explicitErr);
    }

    /// <summary>
    /// Example method demonstrating implicit conversions in return statements.
    /// No need for Result&lt;int, string&gt;.Ok(value) — just return value or error directly.
    /// </summary>
    private static Result<int, string> Divide(int a, int b)
    {
        if (b == 0) return "Division by zero";
        return a / b;
    }
}
