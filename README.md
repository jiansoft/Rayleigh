**English** | [繁體中文](README.zh-TW.md)

# Rayleigh

Option and Result types for C#, inspired by Rust.

A zero-allocation, high-performance functional primitives library for .NET 8.

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

## Usage

### Creating an Option

```csharp
using jIAnSoft.Rayleigh;

// From a value
var some = Option<int>.Some(42);
var none = Option<int>.None;

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
| `Select` / `SelectMany` / `Where` | LINQ support |
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
| `Select` / `SelectMany` | LINQ support |
| `Flatten()` | Unwrap nested `Result<Result<T,E>,E>` (extension) |

### Extension Methods

| Class | Method | Description |
|---|---|---|
| `NullableExtensions` | `ToOption()` | Convert `T?` (reference or value type) to `Option<T>` |
| `EnumerableExtensions` | `Values()` | Filter `IEnumerable<Option<T>>` to extract all `Some` values |
| `OptionAsyncExtensions` | `BindAsync` / `MapAsync` / `OrElseAsync` | Async Option chaining (`Task` & `ValueTask`) |
| `ResultAsyncExtensions` | `BindAsync` / `MapAsync` / `MapErrAsync` / `OrElseAsync` / `TapAsync` / `TapErrAsync` | Async Result chaining (`Task` & `ValueTask`) |

## Project Structure

```
Rayleigh/
├── src/
│   └── Rayleigh/
│       ├── Option.cs          # Option<T> type
│       ├── Result.cs          # Result<T, E>, Ok<T>, Err<E> types
│       ├── Unit.cs            # Unit type
│       └── Extensions.cs      # All extension methods
├── tests/
│   └── Rayleigh.Tests/        # Unit tests (xUnit)
├── examples/
│   └── Rayleigh.Examples/     # Runnable examples & common pitfalls
├── LICENSE
└── README.md
```

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

## License

This project is licensed under the [MIT License](LICENSE).
