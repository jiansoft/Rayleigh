using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record User(int Id, string Name);
file record Address(string City, string Street);

public static class E02OptionTransformations
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E02 - Option Transformations: Map, Filter, Bind, Flatten");

        MapExamples();
        FilterExamples();
        BindExamples();
        FlattenExamples();
        MapOrExamples();
        Pitfalls();
    }

    private static void MapExamples()
    {
        ConsoleHelper.PrintSubSection("1. Map — transform the inner value");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Basic Map
        var doubled = some.Map(x => x * 2);
        ConsoleHelper.PrintResult("Some(42).Map(x => x * 2)", doubled);

        // Map on None returns None without calling the mapper
        var noneDoubled = none.Map(x => x * 2);
        ConsoleHelper.PrintResult("None.Map(x => x * 2)", noneDoubled);

        // Chained Maps
        var chained = some
            .Map(x => x * 2)
            .Map(x => x + 10)
            .Map(x => $"Result: {x}");
        ConsoleHelper.PrintResult("Some(42) -> *2 -> +10 -> format", chained);

        // Map can change the type
        var asString = some.Map(x => x.ToString());
        ConsoleHelper.PrintResult("Some(42).Map(x => x.ToString())", asString);
    }

    private static void FilterExamples()
    {
        ConsoleHelper.PrintSubSection("2. Filter — keep value only if predicate passes");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // Filter passes
        var passes = some.Filter(x => x > 40);
        ConsoleHelper.PrintResult("Some(42).Filter(x => x > 40)", passes);

        // Filter fails — becomes None
        var fails = some.Filter(x => x > 100);
        ConsoleHelper.PrintResult("Some(42).Filter(x => x > 100)", fails);

        // Filter on None always returns None
        var noneFiltered = none.Filter(x => x > 0);
        ConsoleHelper.PrintResult("None.Filter(x => x > 0)", noneFiltered);

        // Practical: validate a value
        var age = Option<int>.Some(15);
        var adultOnly = age.Filter(x => x >= 18);
        ConsoleHelper.PrintResult("Age(15).Filter(x => x >= 18)", adultOnly);
    }

    private static void BindExamples()
    {
        ConsoleHelper.PrintSubSection("3. Bind — chain operations that may also return None");

        // Helper functions that return Option
        Option<User> FindUser(int id) => id switch
        {
            1 => Option<User>.Some(new User(1, "Alice")),
            2 => Option<User>.Some(new User(2, "Bob")),
            _ => Option<User>.None
        };

        Option<Address> GetAddress(int userId) => userId switch
        {
            1 => Option<Address>.Some(new Address("Taipei", "Xinyi Road")),
            _ => Option<Address>.None
        };

        // Happy path: user found, address found
        var addr1 = FindUser(1).Bind(u => GetAddress(u.Id));
        ConsoleHelper.PrintResult("FindUser(1) -> GetAddress", addr1);

        // User not found — entire chain short-circuits
        var addr2 = FindUser(999).Bind(u => GetAddress(u.Id));
        ConsoleHelper.PrintResult("FindUser(999) -> GetAddress", addr2);

        // User found, but no address — second step returns None
        var addr3 = FindUser(2).Bind(u => GetAddress(u.Id));
        ConsoleHelper.PrintResult("FindUser(2) -> GetAddress (no address)", addr3);

        // Multi-step Bind chain
        var city = FindUser(1)
            .Bind(u => GetAddress(u.Id))
            .Map(a => a.City);
        ConsoleHelper.PrintResult("FindUser(1) -> GetAddress -> City", city);
    }

    private static void FlattenExamples()
    {
        ConsoleHelper.PrintSubSection("4. Flatten — unwrap nested Option<Option<T>>");

        // Some(Some(42)) -> Some(42)
        var nestedSome = Option<Option<int>>.Some(Option<int>.Some(42));
        ConsoleHelper.PrintResult("Some(Some(42)).Flatten()", nestedSome.Flatten());

        // Some(None) -> None
        var nestedNoneInner = Option<Option<int>>.Some(Option<int>.None);
        ConsoleHelper.PrintResult("Some(None).Flatten()", nestedNoneInner.Flatten());

        // None (outer) -> None
        var nestedNoneOuter = Option<Option<int>>.None;
        ConsoleHelper.PrintResult("None.Flatten()", nestedNoneOuter.Flatten());

        // Flatten is equivalent to Bind(x => x)
        ConsoleHelper.PrintResult("Flatten() == Bind(x => x)",
            nestedSome.Flatten() == nestedSome.Bind(x => x));
    }

    private static void MapOrExamples()
    {
        ConsoleHelper.PrintSubSection("5. MapOr / MapOrElse — map with a fallback");

        var some = Option<int>.Some(42);
        var none = Option<int>.None;

        // MapOr: eagerly evaluated default
        var mapped1 = some.MapOr("N/A", x => $"Value is {x}");
        ConsoleHelper.PrintResult("Some(42).MapOr(\"N/A\", format)", mapped1);

        var mapped2 = none.MapOr("N/A", x => $"Value is {x}");
        ConsoleHelper.PrintResult("None.MapOr(\"N/A\", format)", mapped2);

        // MapOrElse: lazily evaluated default
        var mapped3 = none.MapOrElse(
            () => $"Computed default at {DateTime.UtcNow:HH:mm:ss}",
            x => $"Value is {x}");
        ConsoleHelper.PrintResult("None.MapOrElse(compute, format)", mapped3);
        ConsoleHelper.PrintCorrect("MapOrElse only calls the default factory when the option is None");
    }

    private static void Pitfalls()
    {
        ConsoleHelper.PrintSubSection("6. Pitfalls");

        // Pitfall 1: Arrow-shaped nested code vs fluent chain
        ConsoleHelper.PrintWrong("Nested if/else to extract values (arrow-shaped code):");
        Console.WriteLine(@"
    // Deeply nested — hard to read
    if (FindUser(id).TryGetValue(out var user))
    {
        if (GetAddress(user.Id).TryGetValue(out var addr))
        {
            if (GetZipCode(addr).TryGetValue(out var zip))
            {
                return zip;
            }
        }
    }
    return ""unknown"";");

        ConsoleHelper.PrintCorrect("Fluent Bind chain — flat and readable:");
        Console.WriteLine(@"
    // Clean pipeline
    var zip = FindUser(id)
        .Bind(u => GetAddress(u.Id))
        .Bind(a => GetZipCode(a))
        .UnwrapOr(""unknown"");");

        // Pitfall 2: Map producing Option<Option<T>> when Bind should be used
        ConsoleHelper.PrintWrong("Using Map when the mapper returns Option<T> creates nesting:");

        Option<int> TryParse(string s) =>
            int.TryParse(s, out var result)
                ? Option<int>.Some(result)
                : Option<int>.None;

        var input = Option<string>.Some("42");

        // Map creates Option<Option<int>> — probably not what you want
        var nested = input.Map(TryParse);
        ConsoleHelper.PrintResult("input.Map(TryParse)  [nested!]", nested);

        // Bind flattens automatically
        var flat = input.Bind(TryParse);
        ConsoleHelper.PrintResult("input.Bind(TryParse) [flat]", flat);

        ConsoleHelper.PrintCorrect("Use Bind when the mapping function itself returns Option<T>");
        ConsoleHelper.PrintCorrect("Use Map when the mapping function returns a plain value T");
    }
}
