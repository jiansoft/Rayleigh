using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record Customer(int Id, string Name);
file record Address(int CustomerId, string City);
file record Invoice(int Id, int CustomerId, decimal Total);

public static class E09LinqIntegration
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E09 - LINQ Integration: Query Syntax with Option and Result");

        OptionLinqSelect();
        OptionLinqWhereSelect();
        OptionLinqMultipleFrom();
        ResultLinqSelect();
        ResultLinqMultipleFrom();
        EnumerableValues();
        LinqVsFluentComparison();
    }

    private static void OptionLinqSelect()
    {
        ConsoleHelper.PrintSubSection("1. Option LINQ: from/select (Select = Map)");

        var some = Option<int>.Some(14);
        var none = Option<int>.None;

        // from x in option select expression
        var tripled = from x in some select x * 3;
        var tripledNone = from x in none select x * 3;

        ConsoleHelper.PrintResult("from x in Some(14) select x * 3", tripled);
        ConsoleHelper.PrintResult("from x in None select x * 3", tripledNone);

        // String transformation
        var name = Option<string>.Some("alice");
        var upper = from n in name select n.ToUpperInvariant();
        ConsoleHelper.PrintResult("from n in Some(\"alice\") select n.ToUpper()", upper);

        ConsoleHelper.PrintCorrect("'select' in LINQ maps to Option.Select (which delegates to Map)");
    }

    private static void OptionLinqWhereSelect()
    {
        ConsoleHelper.PrintSubSection("2. Option LINQ: from/where/select (Where = Filter)");

        var some = Option<int>.Some(42);
        var low = Option<int>.Some(10);
        var none = Option<int>.None;

        // where filters: if predicate fails, result becomes None
        var passed = from x in some where x > 40 select x;
        var filtered = from x in low where x > 40 select x;
        var fromNone = from x in none where x > 40 select x;

        ConsoleHelper.PrintResult("from x in Some(42) where x > 40 select x", passed);
        ConsoleHelper.PrintResult("from x in Some(10) where x > 40 select x", filtered);
        ConsoleHelper.PrintResult("from x in None where x > 40 select x", fromNone);

        // Practical example: validate and transform
        var age = Option<int>.Some(25);
        var validAge = from a in age
                       where a >= 18 && a <= 120
                       select $"Valid age: {a}";
        ConsoleHelper.PrintResult("Age validation via LINQ where", validAge);

        ConsoleHelper.PrintCorrect("'where' in LINQ maps to Option.Where (which delegates to Filter)");
    }

    private static void OptionLinqMultipleFrom()
    {
        ConsoleHelper.PrintSubSection("3. Option LINQ: multiple from (SelectMany = Bind)");

        static Option<Customer> FindCustomer(int id) => id switch
        {
            1 => Option<Customer>.Some(new Customer(1, "Alice")),
            2 => Option<Customer>.Some(new Customer(2, "Bob")),
            _ => Option<Customer>.None
        };

        static Option<Address> GetAddress(int customerId) => customerId switch
        {
            1 => Option<Address>.Some(new Address(1, "Tokyo")),
            _ => Option<Address>.None
        };

        // Chain dependent Option lookups using LINQ query syntax
        var city1 = from customer in FindCustomer(1)
                    from address in GetAddress(customer.Id)
                    select address.City;
        ConsoleHelper.PrintResult("FindCustomer(1) -> GetAddress -> City", city1);

        // First lookup fails (customer not found)
        var city2 = from customer in FindCustomer(999)
                    from address in GetAddress(customer.Id)
                    select address.City;
        ConsoleHelper.PrintResult("FindCustomer(999) -> GetAddress -> City", city2);

        // Second lookup fails (no address for Bob)
        var city3 = from customer in FindCustomer(2)
                    from address in GetAddress(customer.Id)
                    select address.City;
        ConsoleHelper.PrintResult("FindCustomer(2) -> GetAddress -> City", city3);

        // Three-level chain with combined projection
        var greeting = from customer in FindCustomer(1)
                       from address in GetAddress(customer.Id)
                       select $"Hello {customer.Name} from {address.City}";
        ConsoleHelper.PrintResult("Combined projection", greeting);

        ConsoleHelper.PrintCorrect("Multiple 'from' clauses chain Bind operations — None short-circuits the whole query");
    }

    private static void ResultLinqSelect()
    {
        ConsoleHelper.PrintSubSection("4. Result LINQ: from/select (Select = Map)");

        var ok = Result<int, string>.Ok(14);
        var err = Result<int, string>.Err("bad input");

        var tripled = from x in ok select x * 3;
        var tripledErr = from x in err select x * 3;

        ConsoleHelper.PrintResult("from x in Ok(14) select x * 3", tripled);
        ConsoleHelper.PrintResult("from x in Err(\"bad input\") select x * 3", tripledErr);

        // Transform to a different type
        var formatted = from x in ok select $"Value is {x}";
        ConsoleHelper.PrintResult("from x in Ok(14) select $\"Value is {x}\"", formatted);

        ConsoleHelper.PrintCorrect("Result LINQ 'select' works just like Option — errors pass through unchanged");
    }

    private static void ResultLinqMultipleFrom()
    {
        ConsoleHelper.PrintSubSection("5. Result LINQ: multiple from (SelectMany = Bind)");

        static Result<Customer, string> GetCustomer(int id) => id switch
        {
            1 => new Customer(1, "Alice"),
            2 => new Customer(2, "Bob"),
            _ => "Customer not found"
        };

        static Result<Invoice, string> GetInvoice(int customerId) => customerId switch
        {
            1 => new Invoice(501, 1, 1299.99m),
            _ => $"No invoice for customer {customerId}"
        };

        // Success path: get customer, then get invoice
        var total1 = from customer in GetCustomer(1)
                     from invoice in GetInvoice(customer.Id)
                     select invoice.Total;
        ConsoleHelper.PrintResult("GetCustomer(1) -> GetInvoice -> Total", total1);

        // Failure at first step
        var total2 = from customer in GetCustomer(999)
                     from invoice in GetInvoice(customer.Id)
                     select invoice.Total;
        ConsoleHelper.PrintResult("GetCustomer(999) -> GetInvoice -> Total", total2);

        // Failure at second step
        var total3 = from customer in GetCustomer(2)
                     from invoice in GetInvoice(customer.Id)
                     select invoice.Total;
        ConsoleHelper.PrintResult("GetCustomer(2) -> GetInvoice -> Total", total3);

        // Complex projection using both intermediate values
        var summary = from customer in GetCustomer(1)
                      from invoice in GetInvoice(customer.Id)
                      select $"{customer.Name}: Invoice #{invoice.Id} = ${invoice.Total}";
        ConsoleHelper.PrintResult("LINQ projection with both values", summary);

        ConsoleHelper.PrintCorrect("Multiple 'from' clauses chain Bind operations — first error short-circuits");
    }

    private static void EnumerableValues()
    {
        ConsoleHelper.PrintSubSection("6. EnumerableExtensions.Values() — filtering Some values from collections");

        // Basic: filter Some values from an array of Options
        Option<int>[] options =
        [
            Option<int>.Some(10),
            Option<int>.None,
            Option<int>.Some(20),
            Option<int>.None,
            Option<int>.Some(30),
        ];

        var values = options.Values().ToList();
        ConsoleHelper.PrintResult("options.Values()", $"[{string.Join(", ", values)}]");

        // Practical: safe batch lookup
        var inventory = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["apple"] = 5,
            ["banana"] = 12,
            ["cherry"] = 0,
        };

        Option<int> SafeLookup(string key) =>
            inventory.TryGetValue(key, out var qty)
                ? Option<int>.Some(qty)
                : Option<int>.None;

        string[] requestedItems = ["apple", "dragonfruit", "banana", "elderberry", "cherry"];

        var found = requestedItems.Select(SafeLookup).Values().ToList();
        ConsoleHelper.PrintResult("Requested items", $"[{string.Join(", ", requestedItems)}]");
        ConsoleHelper.PrintResult("Found quantities", $"[{string.Join(", ", found)}]");

        // Combine with LINQ for richer processing
        var summary = requestedItems
            .Select(item => SafeLookup(item).Map(qty => $"{item}={qty}"))
            .Values()
            .ToList();
        ConsoleHelper.PrintResult("Items with quantities", $"[{string.Join(", ", summary)}]");

        ConsoleHelper.PrintCorrect(".Values() extracts all Some values — None entries are silently skipped");
    }

    private static void LinqVsFluentComparison()
    {
        ConsoleHelper.PrintSubSection("7. LINQ style vs fluent style — they are equivalent");

        static Option<Customer> FindCustomer(int id) => id switch
        {
            1 => Option<Customer>.Some(new Customer(1, "Alice")),
            2 => Option<Customer>.Some(new Customer(2, "Bob")),
            _ => Option<Customer>.None
        };

        static Option<Address> GetAddress(int customerId) => customerId switch
        {
            1 => Option<Address>.Some(new Address(1, "Tokyo")),
            _ => Option<Address>.None
        };

        static Result<Customer, string> GetCustomer(int id) => id switch
        {
            1 => new Customer(1, "Alice"),
            2 => new Customer(2, "Bob"),
            _ => "Customer not found"
        };

        static Result<Invoice, string> GetInvoice(int customerId) => customerId switch
        {
            1 => new Invoice(501, 1, 1299.99m),
            _ => $"No invoice for customer {customerId}"
        };

        // === Option example ===
        ConsoleHelper.PrintResult("--- Option ---", "");

        // LINQ style
        var linqOption = from customer in FindCustomer(1)
                         from address in GetAddress(customer.Id)
                         select $"{customer.Name} lives in {address.City}";

        // Fluent style (same logic)
        var fluentOption = FindCustomer(1)
            .Bind(customer => GetAddress(customer.Id)
                .Map(address => $"{customer.Name} lives in {address.City}"));

        ConsoleHelper.PrintResult("LINQ style", linqOption);
        ConsoleHelper.PrintResult("Fluent style", fluentOption);
        ConsoleHelper.PrintResult("Are they equal?", linqOption == fluentOption);

        // === Result example ===
        ConsoleHelper.PrintResult("--- Result ---", "");

        // LINQ style
        var linqResult = from customer in GetCustomer(1)
                         from invoice in GetInvoice(customer.Id)
                         select $"{customer.Name}: ${invoice.Total}";

        // Fluent style (same logic)
        var fluentResult = GetCustomer(1)
            .Bind(customer => GetInvoice(customer.Id)
                .Map(invoice => $"{customer.Name}: ${invoice.Total}"));

        ConsoleHelper.PrintResult("LINQ style", linqResult);
        ConsoleHelper.PrintResult("Fluent style", fluentResult);
        ConsoleHelper.PrintResult("Are they equal?", linqResult == fluentResult);

        ConsoleHelper.PrintCorrect("Choose whichever reads better — LINQ shines with multiple 'from' clauses");
        ConsoleHelper.PrintCorrect("Fluent style is more natural for simple Map/Filter chains");
    }
}
