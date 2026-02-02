using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

public static class E01OptionBasics
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E01 - Option Basics: Creating, Checking, and Comparing");

        CreatingOptions();
        StateChecks();
        ContainsAndIsSomeAnd();
        EqualityComparisons();
        Pitfalls();
    }

    private static void CreatingOptions()
    {
        ConsoleHelper.PrintSubSection("1. Creating Options");

        // Some: wraps a non-null value
        var someInt = Option<int>.Some(42);
        ConsoleHelper.PrintResult("Option<int>.Some(42)", someInt);

        var someString = Option<string>.Some("hello");
        ConsoleHelper.PrintResult("Option<string>.Some(\"hello\")", someString);

        // None: represents the absence of a value
        var noneInt = Option<int>.None;
        ConsoleHelper.PrintResult("Option<int>.None", noneInt);

        var noneString = Option<string>.None;
        ConsoleHelper.PrintResult("Option<string>.None", noneString);

        // ToOption() from nullable reference types
        string? name = "Alice";
        string? nullName = null;
        ConsoleHelper.PrintResult("\"Alice\".ToOption()", name.ToOption());
        ConsoleHelper.PrintResult("null (string?).ToOption()", nullName.ToOption());

        // ToOption() from nullable value types
        int? age = 30;
        int? nullAge = null;
        ConsoleHelper.PrintResult("((int?)30).ToOption()", age.ToOption());
        ConsoleHelper.PrintResult("((int?)null).ToOption()", nullAge.ToOption());

        // Some(0) and Some("") are valid — they are NOT None
        var zero = Option<int>.Some(0);
        var empty = Option<string>.Some("");
        ConsoleHelper.PrintResult("Option<int>.Some(0)", zero);
        ConsoleHelper.PrintResult("Option<string>.Some(\"\")", empty);
        ConsoleHelper.PrintCorrect("Zero and empty string are valid Some values, not None");
    }

    private static void StateChecks()
    {
        ConsoleHelper.PrintSubSection("2. State Checks: IsSome / IsNone");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        ConsoleHelper.PrintResult("some.IsSome", some.IsSome);
        ConsoleHelper.PrintResult("some.IsNone", some.IsNone);
        ConsoleHelper.PrintResult("none.IsSome", none.IsSome);
        ConsoleHelper.PrintResult("none.IsNone", none.IsNone);
    }

    private static void ContainsAndIsSomeAnd()
    {
        ConsoleHelper.PrintSubSection("3. Contains / IsSomeAnd");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Contains: checks if the option holds a specific value
        ConsoleHelper.PrintResult("some.Contains(42)", some.Contains(42));
        ConsoleHelper.PrintResult("some.Contains(99)", some.Contains(99));
        ConsoleHelper.PrintResult("none.Contains(42)", none.Contains(42));

        // IsSomeAnd: checks if the option has a value AND the predicate is true
        ConsoleHelper.PrintResult("some.IsSomeAnd(x => x > 40)", some.IsSomeAnd(x => x > 40));
        ConsoleHelper.PrintResult("some.IsSomeAnd(x => x > 100)", some.IsSomeAnd(x => x > 100));
        ConsoleHelper.PrintResult("none.IsSomeAnd(x => x > 0)", none.IsSomeAnd(x => x > 0));
        ConsoleHelper.PrintCorrect("IsSomeAnd on None always returns false — the predicate is never called");
    }

    private static void EqualityComparisons()
    {
        ConsoleHelper.PrintSubSection("4. Equality");

        var a = Option<int>.Some(42);
        var b = Option<int>.Some(42);
        var c = Option<int>.Some(99);
        var n1 = Option<int>.None;
        var n2 = Option<int>.None;

        ConsoleHelper.PrintResult("Some(42) == Some(42)", a == b);
        ConsoleHelper.PrintResult("Some(42) == Some(99)", a == c);
        ConsoleHelper.PrintResult("Some(42) == None", a == n1);
        ConsoleHelper.PrintResult("None == None", n1 == n2);
        ConsoleHelper.PrintResult("Some(42) != None", a != n1);

        // HashCode consistency
        ConsoleHelper.PrintResult("Some(42).GetHashCode() == Some(42).GetHashCode()",
            a.GetHashCode() == b.GetHashCode());

        // ToString for debugging
        ConsoleHelper.PrintResult("Some(42).ToString()", a.ToString());
        ConsoleHelper.PrintResult("None.ToString()", n1.ToString());
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("5. Pitfalls");

        // Pitfall 1: default(Option<T>) vs Option<T>.None
        var fromDefault = default(Option<int>);
        var fromNone = Option<int>.None;
        ConsoleHelper.PrintResult("default(Option<int>) == Option<int>.None", fromDefault == fromNone);
        ConsoleHelper.PrintResult("default(Option<int>).IsNone", fromDefault.IsNone);
        ConsoleHelper.PrintCorrect("Both are equivalent, but Option<T>.None expresses intent clearly");
        ConsoleHelper.PrintWrong("default(Option<T>) works but is ambiguous — readers may wonder if it was intentional");

        // Pitfall 2: Option<string>.Some(null!) throws ArgumentNullException
        ConsoleHelper.CatchAndPrint(
            "Option<string>.Some(null!)",
            () => Option<string>.Some(null!));
        ConsoleHelper.PrintCorrect("Use ToOption() to safely convert nullable values instead:");

        string? maybeNull = null;
        var safe = maybeNull.ToOption();
        ConsoleHelper.PrintResult("((string?)null).ToOption()", safe);
    }
}
