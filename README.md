**English** | [繁體中文](README.zh-TW.md)

# Rayleigh

Option and Result types for C#, inspired by Rust.

A zero-allocation, high-performance functional primitives library for .NET 8 and .NET 10.

## Why Rayleigh?

The C# [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references) feature is useful,
but it's entirely optional and easily ignored. `Result<T, E>` goes further by making error handling explicit in the type system.

Rayleigh uses the type system to:

- **Prevent null-reference errors** — Make it impossible to access a possibly-missing value without first checking if the value is present.
- **Express intent clearly** — If a method might not return a value, or might return an error, the return type makes this obvious and impossible to miss.
- **Chain operations safely** — Use `Map`, `Bind`, and other combinators to build pipelines that short-circuit on `None` or `Err`, eliminating nested `if`/`else` blocks.
- **Avoid exception-driven control flow** — Use `Result<T, E>` for expected business errors (validation, not-found, etc.), reserving exceptions for truly unexpected failures.

## Installation

```bash
dotnet add package jIAnSoft.Rayleigh
```

Or via the NuGet Package Manager:

```
Install-Package jIAnSoft.Rayleigh
```

## When to Use Option vs Result

Use `Option<T>` when absence is a normal outcome and the caller does not need to know why the value is missing.
Use `Result<T, E>` when failure needs an explanation that the caller may log, display, return from an API, retry, or branch on.

| Situation | Use |
|---|---|
| A lookup may or may not find a value | `Option<T>` |
| A nullable value should become explicit | `Option<T>` |
| Missing value is expected and not an error | `Option<T>` |
| Validation, authorization, parsing, or I/O can fail | `Result<T, E>` |
| The caller needs an error reason | `Result<T, E>` |
| The failure should be logged, displayed, returned, retried, or handled differently by type | `Result<T, E>` |

In short: `Option<T>` means maybe value; `Result<T, E>` means success or explained failure.

## Usage

### Creating an Option

```csharp
using jIAnSoft.Rayleigh;

// From a value
var some = Option<int>.Some(42);
var none = Option<int>.None;

// Using implicit conversions (recommended)
Option<int> implicitSome = 42;          // implicitly Some(42)
Option<int> implicitNone = Option.None; // universal None marker

// From nullable reference types
string? name = GetName();
var option = name.ToOption();  // Some("Alice") or None

// From nullable value types
int? maybeAge = GetAge();
var ageOption = maybeAge.ToOption();  // Some(25) or None
```

### Getting Values from an Option

```csharp
// TryGetValue — guard-clause style
if (!option.TryGetValue(out var value))
{
    return; // early exit, no value
}
// value is guaranteed non-null here

// Match — exhaustive handling of both cases
var message = option.Match(
    some: v => $"Hello, {v}!",
    none: () => "Hello, Guest!"
);

// UnwrapOr — provide a default
var name = option.UnwrapOr("Unknown");

// Deconstruct — pattern matching in switch
var result = option switch
{
    (true, var v)  => $"Value: {v}",
    (false, _)     => "No value"
};
```

### Creating a Result

```csharp
using jIAnSoft.Rayleigh;

// Using factory methods
var ok  = Result<int, string>.Ok(42);
var err = Result<int, string>.Err("Something went wrong");

// Using implicit conversions (recommended)
Result<int, string> Divide(int a, int b)
{
    if (b == 0) return "Division by zero";  // implicitly Err
    return a / b;                            // implicitly Ok
}

// Using Ok<T> / Err<E> wrapper records
Result<int, string> fromWrapper = new Ok<int>(42);
Result<int, string> fromErrWrapper = new Err<string>("Oops");
```

### Getting Values from a Result

```csharp
// TryGetOk — guard-clause style (recommended)
if (!result.TryGetOk(out var value, out var error))
{
    return BadRequest(error); // early exit with error
}
// value is guaranteed non-null here

// Match — exhaustive handling
var response = result.Match(
    ok:  user  => $"Welcome, {user.Name}!",
    err: error => $"Error: {error}"
);

// UnwrapOr — provide a default
var timeout = GetConfig("Timeout").UnwrapOr(30);
```

### Safely Chain Together Fallible Methods

Rayleigh supports safely chaining together multiple methods that return `Option` or `Result`,
and converting between `Option` and `Result` when needed.

```csharp
var output = ValidateInput(userInput)
    .Bind(validated => FindUser(validated.UserId))
    .Filter(user => user.IsActive)
    .Map(user => user.Name)
    .UnwrapOr("Guest");
```

The example above does the following:

1. Validates the user input. If validation fails, returns `None` / `Err`.
2. If validation succeeds, looks up the user. If the user is not found, returns `None`.
3. If found, checks whether the user is active. If not, becomes `None`.
4. If the user is active, extracts the name.
5. If at the end we have a value, returns it. Otherwise, returns `"Guest"`.

At no point is a `null` reference possible, and there are no nested `if` / `else` blocks.

### Railway-Oriented Programming with Result

```csharp
// Define methods that return Result
Result<UserDto, AppError> CreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Bind(name => ValidateEmail(request.Email).Map(email => (name, email)))
        .Bind(pair => SaveToDatabase(pair.name, pair.email))
        .Map(entity => entity.ToDto())
        .Tap(dto => logger.LogInformation("Created user {Id}", dto.Id))
        .TapErr(err => logger.LogError("Failed: {Error}", err));
}

// LINQ query syntax — equivalent to chained Bind calls
var total = from user  in GetUser(userId)
            from order in GetLatestOrder(user.Id)
            select order.Total;
```

### Async Pipelines

When your operations involve I/O (database queries, HTTP calls, file access, etc.),
you need async versions of the combinators. Rayleigh provides `Task` and `ValueTask`
extension methods that let you chain async operations with the same railway-oriented style —
errors propagate automatically, and you never need nested `try`/`catch` or `if` blocks.

#### BindAsync — Chain async operations that may fail

`BindAsync` takes a function that returns `Task<Result<TU, TE>>` (or `Task<Option<TU>>`).
If the source is `Ok`/`Some`, the function is called; if it's `Err`/`None`, the chain short-circuits.

```csharp
// Each step returns Task<Result<T, E>>, errors propagate automatically
var user = await ValidateTokenAsync(token)        // Task<Result<UserId, ApiError>>
    .BindAsync(id => FindUserAsync(id))           // -> Task<Result<User, ApiError>>
    .BindAsync(user => LoadPermissionsAsync(user)) // -> Task<Result<UserWithPerms, ApiError>>
```

#### MapAsync — Transform the success value asynchronously

`MapAsync` transforms the inner value without changing the Result/Option structure.
Unlike `BindAsync`, the mapper returns a plain `Task<TU>`, not a wrapped type.

```csharp
var dto = await GetUserAsync(userId)              // Task<Result<User, string>>
    .MapAsync(user => EnrichWithAvatarAsync(user)) // -> Task<Result<UserDto, string>>
```

#### TapAsync / TapErrAsync — Async side effects

Execute async side effects (logging, notifications, metrics) without changing the value.
`TapAsync` fires on success, `TapErrAsync` fires on error.

```csharp
var result = await CreateOrderAsync(request)
    .TapAsync(order => SendConfirmationEmailAsync(order))
    .TapErrAsync(err => AlertOpsChannelAsync(err));
// result is unchanged — side effects run but don't alter the value
```

#### OrElseAsync — Async fallback on error

Provide an alternative when the original operation fails.

```csharp
var data = await LoadFromCacheAsync(key)
    .OrElseAsync(err => LoadFromDatabaseAsync(key));
// If cache misses (Err), falls back to the database
```

#### MapErrAsync — Transform the error asynchronously

Convert one error type to another (e.g., enrich with context from an async source).

```csharp
var result = await CallExternalApiAsync(request)
    .MapErrAsync(err => EnrichErrorWithTraceAsync(err));
```

#### Full example — Combining everything

```csharp
Result<OrderConfirmation, AppError> confirmation = await ValidateOrderAsync(request)
    .BindAsync(order => CheckInventoryAsync(order))
    .BindAsync(order => ProcessPaymentAsync(order))
    .MapAsync(receipt => BuildConfirmationAsync(receipt))
    .TapAsync(conf => SendEmailAsync(conf.Email, conf))
    .TapErrAsync(err => logger.LogErrorAsync("Order failed: {Error}", err))
    .OrElseAsync(err => CreatePendingOrderAsync(request, err));
```

The pipeline above:

1. **ValidateOrderAsync** — Validates the incoming request. Returns `Err` if invalid.
2. **CheckInventoryAsync** — Checks stock availability. Returns `Err` if out of stock.
3. **ProcessPaymentAsync** — Charges the customer. Returns `Err` if payment fails.
4. **BuildConfirmationAsync** — Transforms the payment receipt into a confirmation DTO.
5. **SendEmailAsync** — Sends a confirmation email (side effect, doesn't alter the value).
6. **LogErrorAsync** — Logs the error if any step failed (side effect on the error path).
7. **CreatePendingOrderAsync** — Fallback: if anything failed, create a pending order instead.

Each step only runs if the previous one succeeded. Errors propagate automatically without any `try`/`catch` or `if`/`else` nesting.

> **`Task` vs `ValueTask`**: Every method above has a `ValueTask` overload as well.
> Use `ValueTask` in hot paths to avoid heap allocation when the result is often available synchronously.

> **`CancellationToken`**: Every async combinator also has an overload whose delegate accepts a
> `CancellationToken` as its last parameter, plus a trailing `CancellationToken` argument on the combinator itself:
> ```csharp
> var result = await GetUserAsync(userId)
>     .BindAsync((user, ct) => GetOrdersAsync(user.Id, ct), cancellationToken);
> ```
> The token is only checked (and forwarded) when the delegate is actually about to run — a short-circuited
> `Err`/`None` branch never observes cancellation, matching the no-op semantics of the rest of the library.

### Working with Collections

#### Entering the Option world — safely

The BCL's `FirstOrDefault` and `GetValueOrDefault` cannot distinguish *"not found"* from
*"found a value that happens to equal the default"*. For value types this silently loses information:

```csharp
var scores = new[] { 0, 5, 10 };

scores.FirstOrDefault();          // 0  ─┬─ indistinguishable
Array.Empty<int>().FirstOrDefault(); // 0  ─┘

scores.FirstOrNone();             // Some(0)
Array.Empty<int>().FirstOrNone(); // None
```

The same applies to dictionaries:

```csharp
var counts = new Dictionary<string, int> { ["a"] = 0 };

counts.GetValueOrDefault("a");     // 0  ─┬─ indistinguishable
counts.GetValueOrDefault("miss");  // 0  ─┘

counts.GetValueOrNone("a");        // Some(0)
counts.GetValueOrNone("miss");     // None
```

Full set of entry points:

```csharp
source.FirstOrNone();               // Option<T>
source.FirstOrNone(x => x > 10);    // Option<T>, short-circuits on first match
source.SingleOrNone();              // None if empty OR more than one (never throws)
source.ElementAtOrNone(3);          // None if out of range (negative index included)
dictionary.GetValueOrNone(key);     // Option<TValue>
```

#### `Sequence()` — all-or-nothing

Turns a collection of `Option`/`Result` inside out. Short-circuits on the first `None`/`Err`:

```csharp
Option<int>[] all  = [Option<int>.Some(1), Option<int>.Some(2)];
Option<int>[] some = [Option<int>.Some(1), Option<int>.None];

all.Sequence();   // Some([1, 2])
some.Sequence();  // None

// With Result: returns the FIRST error encountered, and stops enumerating there
var validated = inputs.Select(Validate).Sequence();   // Result<List<Valid>, Error>
```

#### `Partition()` — collect every error

Where `Sequence()` short-circuits, `Partition()` walks the entire sequence and gathers everything.
This is what you want for form validation, where the user should see all problems at once:

```csharp
var (users, errors) = dtos.Select(Validate).Partition();

if (errors.Count > 0)
{
    return BadRequest(errors);   // report ALL validation failures, not just the first
}

return Ok(users);
```

#### `Values()` — keep the `Some`s, drop the `None`s

```csharp
Option<int>[] options = [Option<int>.Some(1), Option<int>.None, Option<int>.Some(3)];
options.Values();   // [1, 3]
```

> **`Values()` vs `Sequence()`**
> `Values()` ignores `None` and keeps what it can. `Sequence()` treats any `None` as total failure.

### Starting an Async Pipeline from a Sync Value

The `Task`/`ValueTask` async overloads cover the case where the pipeline *already* is async. When the
**start** of the chain is a synchronously obtained `Result`/`Option`, use the overloads that take the
value itself:

```csharp
// Before — an extra Task allocation just to satisfy the type
await Task.FromResult(Validate(input)).BindAsync(v => SaveAsync(v));

// After
await Validate(input).BindAsync(v => SaveAsync(v));
```

These overloads are deliberately **not** declared `async`. On the short-circuit path (`Err`/`None`) they
return an already-completed `ValueTask` — no state machine, no allocation:

```csharp
var pending = Result<int, string>.Err("boom")
    .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)));

pending.IsCompletedSuccessfully;   // true — never touched the thread pool
```

### Unit Type

Use `Unit` as a success type in `Result` when there is no meaningful return value:

```csharp
Result<Unit, string> Save(Entity entity)
{
    if (!IsValid(entity)) return "Validation failed";
    repository.Save(entity);
    return Unit.Value;
}
```

### Converting Between Option and Result

```csharp
// Option -> Result (None becomes Err with provided error)
var result = option.ToResult("Value not found");
var result2 = option.ToResult(() => new AppError("Not found"));

// Result -> Option (discards error info)
var option = result.ToOption();

// Result -> Option<Error> (discards success value)
var maybeError = result.Err();
```

## Uses Modern .NET Features

- **Zero heap allocation** — All core types (`Option<T>`, `Result<T, E>`, `Unit`) are `readonly struct`, living entirely on the stack.
- **AggressiveInlining** — All critical-path methods are JIT-inlined for minimal overhead.
- **`IEquatable<T>`** and **`IComparable<T>`** — `Option` and `Unit` can be compared, sorted, and used as dictionary keys.
- **Nullable annotations** — Full support for C# nullable reference type analysis.
- **LINQ query syntax** — `Select`, `SelectMany`, `Where` enable `from`/`where`/`select` syntax.
- **Pattern matching** — `Deconstruct` enables `switch` expressions and `is` patterns.
- **Async support** — `Task<T>` and `ValueTask<T>` extension methods for `BindAsync`, `MapAsync`, `OrElseAsync`, `TapAsync`, and more.
- **Implicit conversions** — `Result<T, E>` can be created directly from `T`, `E`, `Ok<T>`, or `Err<E>` for concise method returns.
- **Poisoned default state** — `default(Result<T, E>)` is *not* a valid result. Because C# lets any struct be
  zero-initialized (array slots, unassigned fields), `Result` tracks a distinct uninitialized state and throws
  `InvalidOperationException` on any member that would read a value. This holds for **every** `E`, including
  `enum` and `struct` error types where the default value is an ordinary member.
- **AOT & trimming ready** — `IsAotCompatible` and `IsTrimmable`; no reflection, no dynamic code generation.

> **Known limitation — `T` and `E` must differ**
> `Result<T, E>` declares implicit conversions from both `T` and `E`. When they resolve to the same type
> (e.g. `Result<string, string>`), those two operators collide and implicit conversion fails to compile with
> `CS0457`. Use the explicit factories or the wrapper records instead:
>
> ```csharp
> Result<string, string> r = ok ? new Ok<string>("value") : new Err<string>("error");
> ```
>
> Better still, give errors a dedicated type (`enum`, `record`, or `readonly record struct`) — it sidesteps the
> limitation and gives failures real meaning in the type system.

## API Reference

### Option\<T\>

| Method | Description |
|---|---|
| `Some(T)` / `None` | Construction |
| `IsSome` / `IsNone` | State check |
| `Contains(T)` | Value equality check |
| `IsSomeAnd(predicate)` | Conditional check |
| `Match(some, none)` | Pattern match (with or without return value) |
| `Map(mapper)` | Transform inner value |
| `Filter(predicate)` | Conditional passthrough |
| `Bind(binder)` | Monadic bind (flatMap) |
| `Zip(other)` / `ZipWith(other, zipper)` | Combine two Options |
| `Or(other)` / `OrElse(factory)` | Fallback |
| `Tap(action)` | Side effect without changing value |
| `Unwrap()` / `UnwrapOr(default)` / `UnwrapOrElse(factory)` / `Expect(msg)` | Extract value |
| `TryGetValue(out value)` | TryParse-style extraction |
| `ToResult(error)` / `ToResult(factory)` | Convert to Result |
| `MapOr(default, mapper)` / `MapOrElse(factory, mapper)` | Map with fallback |
| `Deconstruct(out isSome, out value)` | Deconstruct for pattern matching and switch expressions |
| `Select` / `SelectMany` / `Where` | LINQ support |
| `Equals` / `CompareTo` / comparison operators | Equality, ordering, and sorting support; `None` sorts before `Some` |
| `ToString()` | Debug-friendly `Some(value)` or `None` text |
| `Flatten()` | Unwrap nested `Option<Option<T>>` (extension) |

### Result\<T, E\>

| Method | Description |
|---|---|
| `Ok(T)` / `Err(E)` | Construction |
| Implicit from `T` / `E` / `Ok<T>` / `Err<E>` | Implicit conversions |
| `IsOk` / `IsErr` | State check |
| `Contains(T)` / `ContainsErr(E)` | Value / error equality check |
| `IsOkAnd(predicate)` / `IsErrAnd(predicate)` | Conditional check |
| `Match(ok, err)` | Pattern match |
| `Map(mapper)` / `MapErr(mapper)` | Transform value or error |
| `Bind(binder)` | Monadic bind |
| `Or(other)` / `OrElse(factory)` | Fallback |
| `Tap(action)` / `TapErr(action)` | Side effects |
| `Unwrap()` / `UnwrapOr(default)` / `UnwrapOrElse(factory)` / `Expect(msg)` | Extract value |
| `UnwrapErr()` / `ExpectErr(msg)` | Extract error |
| `TryGetOk(out value)` / `TryGetOk(out value, out error)` / `TryGetErr(out error)` | TryParse-style |
| `ToOption()` / `Err()` | Convert to Option |
| `MapOr` / `MapOrElse` | Map with fallback |
| `Deconstruct(out isOk, out value, out error)` | Deconstruct for pattern matching and switch expressions |
| `Select` / `SelectMany` | LINQ support |
| `Equals` / `CompareTo` / comparison operators | Equality, ordering, and sorting support; `Err` sorts before `Ok` |
| `ToString()` | Debug-friendly `Ok(value)` or `Err(error)` text |
| `Flatten()` | Unwrap nested `Result<Result<T,E>,E>` (extension) |

### Supporting Types

| Type | API | Description |
|---|---|---|
| `Unit` | `Unit.Value` | Represents a successful result with no meaningful value, useful as `Result<Unit, E>` |
| `Unit` | `Equals` / `CompareTo` / `==` / `!=` / `ToString()` | All Unit values are equal, compare as equal, and render as `()` |
| `OptionNone` | `Option.None` marker | Universal None marker that can implicitly convert to any `Option<T>` |
| `Ok<T>` / `Err<E>` | Wrapper records | Explicit wrappers for creating `Result<T, E>` when implicit conversion is ambiguous or clarity matters |

### Extension Methods

| Class | Method | Description |
|---|---|---|
| `OptionExtensions` | `OrNull()` | Convert `Option<T>` to `T?` (supports both struct and class types) |
| `NullableExtensions` | `ToOption()` | Convert `T?` (reference or value type) to `Option<T>` |
| `EnumerableExtensions` | `Values()` | Filter `IEnumerable<Option<T>>` to extract all `Some` values |
| `EnumerableExtensions` | `FirstOrNone()` / `FirstOrNone(predicate)` | First element as `Option<T>`; `None` if empty or no match |
| `EnumerableExtensions` | `SingleOrNone()` | The single element as `Option<T>`; `None` if empty **or** more than one |
| `EnumerableExtensions` | `ElementAtOrNone(index)` | Element at index as `Option<T>`; `None` if out of range |
| `EnumerableExtensions` | `GetValueOrNone(key)` | Dictionary lookup as `Option<TValue>`; `None` if the key is absent |
| `EnumerableExtensions` | `Sequence()` | `IEnumerable<Option<T>>` → `Option<List<T>>`, `IEnumerable<Result<T,E>>` → `Result<List<T>,E>` (short-circuits) |
| `EnumerableExtensions` | `Partition()` | `IEnumerable<Result<T,E>>` → `(List<T> Values, List<E> Errors)` (collects **all** errors) |
| `OptionAsyncExtensions` | `BindAsync` / `MapAsync` / `OrElseAsync` | Async Option chaining (`Task` & `ValueTask`) |
| `OptionAsyncExtensions` | `BindAsync` / `MapAsync` on `Option<T>` | Start an async pipeline from a **synchronous** `Option<T>` — no `Task.FromResult` wrapper needed |
| `ResultAsyncExtensions` | `BindAsync` / `MapAsync` / `MapErrAsync` / `OrElseAsync` / `TapAsync` / `TapErrAsync` | Async Result chaining (`Task` & `ValueTask`) |
| `ResultAsyncExtensions` | `BindAsync` / `MapAsync` on `Result<T,E>` | Start an async pipeline from a **synchronous** `Result<T,E>` — short-circuits with zero allocation |

## Security Considerations

`Unwrap()` and `UnwrapErr()` throw an `InvalidOperationException` whose default message embeds the
`ToString()` output of the value you didn't ask for (`Unwrap()` on `Err` includes the error; `UnwrapErr()`
on `Ok` includes the value):

```csharp
var result = Result<int, string>.Err(secretValidationDetails);
result.Unwrap(); // message: "Result is Err: {secretValidationDetails}"
```

If `T` or `E` may carry sensitive data (raw user input, tokens, connection strings, PII), that content can
flow into logs, telemetry, or error responses through an uncaught exception. The same applies to `ToString()`,
which always renders `Ok(value)` / `Err(error)` for debugging purposes.

**Guidance:**
- Prefer `Expect(message)` / `ExpectErr(message)` with a static, non-sensitive message when the value/error
  might be sensitive — the message is entirely caller-controlled.
- Prefer `Match` / `TryGetOk` / `TryGetErr` when you need to branch on success/failure without ever risking
  the default exception path.
- Avoid putting secrets directly in `E` (or `T`); wrap them in an error/DTO type whose `ToString()` you control,
  or scrub sensitive fields before they reach a `Result`/`Option`.

## Project Structure

```
Rayleigh/
├── src/
│   └── jIAnSoft.Rayleigh/     # Core library
├── tests/
│   └── jIAnSoft.Rayleigh.Tests/ # Unit tests (xUnit)
├── examples/
│   └── jIAnSoft.Rayleigh.Examples/ # Runnable tutorial (E01–E12, heavily commented)
├── benchmarks/
│   └── jIAnSoft.Rayleigh.Benchmarks/ # BenchmarkDotNet allocation/throughput benchmarks
├── CHANGELOG.md                      # Release notes
├── LICENSE
└── README.md
```

## Learn by Running

The `examples/` project is a runnable tutorial, not a code dump. Each of the twelve modules explains
**why** an API exists before showing how to use it, then prints every expression next to its actual result:

```bash
# Walk through all twelve modules
dotnet run --project examples/jIAnSoft.Rayleigh.Examples

# Or jump to one — e.g. module 8, collection operations
dotnet run --project examples/jIAnSoft.Rayleigh.Examples -- 8
```

| # | Module | Covers |
|---|---|---|
| E01 | Option basics | What `Option` is, four ways to create one, `IsSome` / `Contains` |
| E02 | Option transformations | `Map` / `Filter` / `Bind` / `Flatten` — and how to pick between them |
| E03 | Getting values out | `Match`, `TryGetValue`, the `Unwrap` family, `Or`, `Tap`, `Zip` |
| E04 | Result basics | Carrying a failure reason; choosing an error type; the poisoned `default` |
| E05 | Result transformations | `Map` / `MapErr` / `Bind` and railway-oriented programming |
| E06 | Result extraction | `Match`, `TryGetOk`, fallbacks, logging, the `Unit` type |
| E07 | Option ↔ Result | Which one to reach for, and how to convert between them |
| E08 | Collections | `FirstOrNone`, `GetValueOrNone`, `Sequence`, `Partition`, `Values` |
| E09 | Async pipelines | `BindAsync` / `MapAsync`, zero-allocation short-circuit, cancellation |
| E10 | LINQ query syntax | `from` / `where` / `select` over `Option` and `Result` |
| E11 | Real-world scenarios | Config, form validation, sign-up, batch import, layered cache |
| E12 | Common pitfalls | Ten mistakes worth seeing once, each with the fix |

> Modules are written for someone meeting `Option` / `Result` for the first time. If you already know
> the concepts, E08 and E12 are the ones with material you likely haven't seen.

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

## Benchmarks

The `benchmarks/` project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure allocation and
throughput for `Option<T>` and `Result<T, E>`, substantiating the "zero-allocation" claim above and guarding
against regressions. Run it in Release mode (BenchmarkDotNet refuses to run under a Debug build):

```bash
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks
```

Run a single suite, or a quick pass with fewer iterations:

```bash
# One suite
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks -- --filter '*EnumerableBenchmarks*'

# Quick pass (lower precision, much faster)
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks -- --filter '*' --job short
```

| Suite | What it establishes |
|---|---|
| `OptionBenchmarks` | `Option<T>` allocates nothing on its own; the contrast group shows closure cost is the *caller's*, not the library's |
| `ResultBenchmarks` | Same for `Result<T,E>`; the uninitialized-state guard costs nothing on the success path |
| `EnumerableBenchmarks` | The collection combinators' stronger semantics don't cost throughput vs. their LINQ counterparts |
| `AsyncPipelineBenchmarks` | Sync-source overloads short-circuit with **0 B** allocated, vs. the `Task.FromResult` wrapper they replace |
| `ThrowPathBenchmarks` | Quantifies why `Result` beats exception-driven control flow on the error path |

Representative numbers (BenchmarkDotNet 0.15.8, AMD Ryzen 9 5950X, .NET 10.0.9):

| | Mean | Allocated |
|---|---:|---:|
| `Result.Ok(v)` | 0.13 ns | 0 B |
| `result.UnwrapOr(-1)` on `Err` | 0.16 ns | 0 B |
| `try { result.Unwrap(); } catch { }` — same error, via exception | 2,100 ns | 384 B |
| `err.BindAsync(...)` — sync-source, short-circuit | 13.9 ns | **0 B** |
| `Task.FromResult(err).BindAsync(...)` — the wrapper it replaces | 38.4 ns | 240 B |

> **On precision:** these come from a `--job short` run (3 iterations), so the `Mean` column has wide
> error bars and is directional only. The `Allocated` column is measured deterministically via GC
> counters and is exact. Re-run without `--job short` for citable timings.

## License

This project is licensed under the [MIT License](LICENSE).
