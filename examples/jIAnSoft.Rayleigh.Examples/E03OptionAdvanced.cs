using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record Product(int Id, string Name, decimal Price);

public static class E03OptionAdvanced
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E03 - Option Advanced: Zip, Or, Tap, TryGetValue, Unwrap, Deconstruct, Match");

        ZipExamples();
        OrExamples();
        TapExamples();
        TryGetValueExamples();
        UnwrapFamilyExamples();
        DeconstructExamples();
        MatchExamples();
        Pitfalls();
    }

    private static void ZipExamples()
    {
        ConsoleHelper.PrintSubSection("1. Zip / ZipWith — combine two Options");

        var firstName = Option<string>.Some("John");
        var lastName = Option<string>.Some("Doe");
        var noName = Option<string>.None;

        // Zip: combines into a tuple
        var fullName = firstName.Zip(lastName);
        ConsoleHelper.PrintResult("firstName.Zip(lastName)", fullName);

        var missing = firstName.Zip(noName);
        ConsoleHelper.PrintResult("firstName.Zip(None)", missing);

        // ZipWith: combines using a function
        var width = Option<int>.Some(10);
        var height = Option<int>.Some(20);
        var noHeight = Option<int>.None;

        var area = width.ZipWith(height, (w, h) => w * h);
        ConsoleHelper.PrintResult("width.ZipWith(height, (w,h) => w*h)", area);

        var noArea = width.ZipWith(noHeight, (w, h) => w * h);
        ConsoleHelper.PrintResult("width.ZipWith(None, (w,h) => w*h)", noArea);
        ConsoleHelper.PrintCorrect("Zip returns None if either side is None");
    }

    private static void OrExamples()
    {
        ConsoleHelper.PrintSubSection("2. Or / OrElse — provide fallback Options");

        var primary = Option<string>.None;
        var fallback = Option<string>.Some("fallback-value");

        // Or: eagerly evaluated alternative
        var result1 = primary.Or(fallback);
        ConsoleHelper.PrintResult("None.Or(Some(\"fallback-value\"))", result1);

        var existing = Option<string>.Some("primary-value");
        var result2 = existing.Or(fallback);
        ConsoleHelper.PrintResult("Some(\"primary-value\").Or(fallback)", result2);

        // OrElse: lazily computed alternative
        var result3 = primary.OrElse(() =>
        {
            // This runs only when primary is None
            ConsoleHelper.PrintResult("  (OrElse factory invoked)", true);
            return Option<string>.Some("computed-fallback");
        });
        ConsoleHelper.PrintResult("None.OrElse(() => compute)", result3);

        // OrElse is NOT called when the option already has a value
        var callCount = 0;
        var result4 = existing.OrElse(() =>
        {
            callCount++;
            return Option<string>.Some("should-not-appear");
        });
        ConsoleHelper.PrintResult("Some.OrElse(factory) — factory call count", callCount);
        ConsoleHelper.PrintCorrect("OrElse skips the factory when the option is Some");
    }

    private static void TapExamples()
    {
        ConsoleHelper.PrintSubSection("3. Tap — peek at the value without changing it");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Tap on Some: action executes
        var tapped = some.Tap(v => ConsoleHelper.PrintResult("  Tap saw", v));
        ConsoleHelper.PrintResult("After Tap, option unchanged", tapped);

        // Tap on None: action does NOT execute
        var noneExecuted = false;
        none.Tap(_ => noneExecuted = true);
        ConsoleHelper.PrintResult("Tap on None — action executed?", noneExecuted);

        // Tap is useful for logging in a pipeline
        var result = Option<string>.Some("data")
            .Tap(v => ConsoleHelper.PrintResult("  [log] Processing", v))
            .Map(s => s.ToUpperInvariant())
            .Tap(v => ConsoleHelper.PrintResult("  [log] After Map", v));
        ConsoleHelper.PrintResult("Pipeline result", result);
    }

    private static void TryGetValueExamples()
    {
        ConsoleHelper.PrintSubSection("4. TryGetValue — guard-clause pattern");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Basic TryGetValue
        if (some.TryGetValue(out var value))
        {
            ConsoleHelper.PrintResult("TryGetValue succeeded with", value);
        }

        if (!none.TryGetValue(out _))
        {
            ConsoleHelper.PrintResult("TryGetValue on None returned", false);
        }

        // Guard clause: early return when None
        static string ProcessOrder(Option<Product> productOpt)
        {
            if (!productOpt.TryGetValue(out var product))
            {
                return "No product found";
            }

            // Here, product is guaranteed to be non-null
            return $"Processing: {product.Name} (${product.Price})";
        }

        var found = Option<Product>.Some(new Product(1, "Widget", 9.99m));
        var notFound = Option<Product>.None;

        ConsoleHelper.PrintResult("ProcessOrder(Some)", ProcessOrder(found));
        ConsoleHelper.PrintResult("ProcessOrder(None)", ProcessOrder(notFound));
        ConsoleHelper.PrintCorrect("TryGetValue is ideal for guard clauses — flat, no nesting");
    }

    private static void UnwrapFamilyExamples()
    {
        ConsoleHelper.PrintSubSection("5. Unwrap family: UnwrapOr, UnwrapOrElse, Expect");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // UnwrapOr: provide a static default
        ConsoleHelper.PrintResult("Some(42).UnwrapOr(0)", some.UnwrapOr(0));
        ConsoleHelper.PrintResult("None.UnwrapOr(0)", none.UnwrapOr(0));

        // UnwrapOrElse: compute the default lazily
        var result = none.UnwrapOrElse(() =>
        {
            // Only called when None
            return -1;
        });
        ConsoleHelper.PrintResult("None.UnwrapOrElse(() => -1)", result);

        // Expect: unwrap with a custom error message
        ConsoleHelper.PrintResult("Some(42).Expect(\"must exist\")", some.Expect("must exist"));
        ConsoleHelper.CatchAndPrint(
            "None.Expect(\"Config value is required\")",
            () => none.Expect("Config value is required"));
    }

    private static void DeconstructExamples()
    {
        ConsoleHelper.PrintSubSection("6. Deconstruct — tuple decomposition and pattern matching");

        var some = Option<string>.Some("hello");
        var none = Option<string>.None;

        // Tuple deconstruction
        var (isSome1, val1) = some;
        ConsoleHelper.PrintResult("var (isSome, val) = Some(\"hello\")", $"isSome={isSome1}, val={val1}");

        var (isSome2, val2) = none;
        ConsoleHelper.PrintResult("var (isSome, val) = None", $"isSome={isSome2}, val={val2}");

        // Switch expression with deconstruct
        var message1 = some switch
        {
            (true, var v) => $"Got value: {v}",
            (false, _) => "No value"
        };
        ConsoleHelper.PrintResult("some switch { (true, v) => ..., ... }", message1);

        var message2 = none switch
        {
            (true, var v) => $"Got value: {v}",
            (false, _) => "No value"
        };
        ConsoleHelper.PrintResult("none switch { (true, v) => ..., ... }", message2);

        // is pattern matching
        if (some is (true, var matched))
        {
            ConsoleHelper.PrintResult("some is (true, var matched)", matched);
        }
    }

    private static void MatchExamples()
    {
        ConsoleHelper.PrintSubSection("7. Match — exhaustive handling of both states");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Match with return value
        var greeting = some.Match(
            some: v => $"The answer is {v}",
            none: () => "No answer available"
        );
        ConsoleHelper.PrintResult("Some(42).Match(format, fallback)", greeting);

        var noneGreeting = none.Match(
            some: v => $"The answer is {v}",
            none: () => "No answer available"
        );
        ConsoleHelper.PrintResult("None.Match(format, fallback)", noneGreeting);

        // Match with void overload (side effects)
        Console.Write("  Some(42).Match (void): ");
        some.Match(
            some: v => Console.WriteLine($"Processing value {v}"),
            none: () => Console.WriteLine("Nothing to process")
        );

        Console.Write("  None.Match (void):     ");
        none.Match(
            some: v => Console.WriteLine($"Processing value {v}"),
            none: () => Console.WriteLine("Nothing to process")
        );

        ConsoleHelper.PrintCorrect("Match forces you to handle both cases — no forgotten None checks");
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("8. Pitfalls");

        // Pitfall 1: Unwrap on None throws
        ConsoleHelper.PrintWrong("Calling Unwrap() on None throws InvalidOperationException:");
        var none = Option<int>.None;
        ConsoleHelper.CatchAndPrint("None.Unwrap()", () => none.Unwrap());
        ConsoleHelper.PrintCorrect("Use TryGetValue, UnwrapOr, UnwrapOrElse, or Match instead");

        // Pitfall 2: Option when you need error information — use Result
        ConsoleHelper.PrintWrong("Option only tells you 'no value' — it cannot tell you WHY:");
        Console.WriteLine(@"
    // Option<User> — was it 'not found'? 'permission denied'? 'timeout'?
    var user = FindUser(id);  // None... but why?

    // Result<User, Error> — the error carries context
    var user = FindUser(id);  // Err(""User not found"") or Err(""Timeout"")");
        ConsoleHelper.PrintCorrect("When the caller needs to know the reason for failure, use Result<T, E> instead");

        // Pitfall 3: Deep nesting vs TryGetValue guard clause
        ConsoleHelper.PrintWrong("Deeply nested Match/IsSome checks:");
        Console.WriteLine(@"
    // Arrow-shaped nesting
    option.Match(
        some: v =>
        {
            inner.Match(
                some: v2 =>
                {
                    // ... deeper and deeper
                },
                none: () => { }
            );
        },
        none: () => { }
    );");

        ConsoleHelper.PrintCorrect("Flat guard clauses with TryGetValue:");
        Console.WriteLine(@"
    if (!option.TryGetValue(out var v)) return;
    if (!inner.TryGetValue(out var v2)) return;
    // Both v and v2 are guaranteed non-null here");
    }
}
