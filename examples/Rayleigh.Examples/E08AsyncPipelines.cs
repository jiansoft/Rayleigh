using jIAnSoft.Rayleigh;
using jIAnSoft.Rayleigh.Extensions;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record User(int Id, string Name);
file record Order(int Id, int UserId, decimal Total);

public static class E08AsyncPipelines
{
    public static async Task RunAsync()
    {
        ConsoleHelper.PrintHeader("E08 - Async Pipelines: BindAsync, MapAsync, TapAsync, and More");

        await ResultBindAsyncDemo();
        await ResultMapAsyncDemo();
        await ResultMapErrAsyncDemo();
        await ResultTapAndTapErrAsyncDemo();
        await ResultOrElseAsyncDemo();
        await FullResultAsyncPipelineDemo();
        await OptionAsyncDemo();
        await ValueTaskVariantDemo();
    }

    private static async Task ResultBindAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("1. Result BindAsync — chaining async operations");

        static Task<Result<User, string>> GetUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Result<User, string>.Ok(new User(1, "Alice"))
                : Result<User, string>.Err("User not found"));

        static Task<Result<Order, string>> GetOrdersAsync(int userId) =>
            Task.FromResult(userId == 1
                ? Result<Order, string>.Ok(new Order(101, 1, 249.99m))
                : Result<Order, string>.Err("No orders found"));

        // Success path: GetUser -> GetOrders
        var result1 = await GetUserAsync(1)
            .BindAsync(u => GetOrdersAsync(u.Id));
        ConsoleHelper.PrintResult("GetUserAsync(1).BindAsync(GetOrdersAsync)", result1);

        // Failure at first step
        var result2 = await GetUserAsync(999)
            .BindAsync(u => GetOrdersAsync(u.Id));
        ConsoleHelper.PrintResult("GetUserAsync(999).BindAsync(GetOrdersAsync)", result2);

        ConsoleHelper.PrintCorrect("BindAsync short-circuits on the first error — subsequent steps are skipped");
    }

    private static async Task ResultMapAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("2. Result MapAsync — async transformation of Ok value");

        static Task<Result<User, string>> GetUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Result<User, string>.Ok(new User(1, "Alice"))
                : Result<User, string>.Err("User not found"));

        static Task<string> FormatAsync(User user) =>
            Task.FromResult($"[{user.Id}] {user.Name}");

        var result1 = await GetUserAsync(1).MapAsync(FormatAsync);
        ConsoleHelper.PrintResult("GetUserAsync(1).MapAsync(FormatAsync)", result1);

        // MapAsync on Err: mapper is never called, error passes through
        var result2 = await GetUserAsync(999).MapAsync(FormatAsync);
        ConsoleHelper.PrintResult("GetUserAsync(999).MapAsync(FormatAsync)", result2);

        ConsoleHelper.PrintCorrect("MapAsync transforms the Ok value; errors pass through untouched");
    }

    private static async Task ResultMapErrAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("3. Result MapErrAsync — async transformation of error");

        static Task<Result<User, string>> GetUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Result<User, string>.Ok(new User(1, "Alice"))
                : Result<User, string>.Err("User not found"));

        static Task<string> TranslateErrorAsync(string error) =>
            Task.FromResult($"Translated: {error}");

        var result = await GetUserAsync(999)
            .MapErrAsync(TranslateErrorAsync);
        ConsoleHelper.PrintResult("GetUserAsync(999).MapErrAsync(TranslateErrorAsync)", result);

        // MapErrAsync on Ok: mapper is never called
        var okResult = await GetUserAsync(1)
            .MapErrAsync(TranslateErrorAsync);
        ConsoleHelper.PrintResult("GetUserAsync(1).MapErrAsync(TranslateErrorAsync)", okResult);

        ConsoleHelper.PrintCorrect("MapErrAsync transforms only errors — useful for translating domain errors to API errors");
    }

    private static async Task ResultTapAndTapErrAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("4. Result TapAsync / TapErrAsync — async side effects");

        static Task<Result<User, string>> GetUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Result<User, string>.Ok(new User(1, "Alice"))
                : Result<User, string>.Err("User not found"));

        // TapAsync on success — e.g., logging, auditing
        ConsoleHelper.PrintCorrect("TapAsync on Ok — side effect fires:");
        var result1 = await GetUserAsync(1)
            .TapAsync(u =>
            {
                ConsoleHelper.PrintResult("  TapAsync saw user", u.Name);
                return Task.CompletedTask;
            });
        ConsoleHelper.PrintResult("Result after TapAsync", result1);

        // TapAsync on Err — side effect is skipped
        ConsoleHelper.PrintCorrect("TapAsync on Err — side effect is skipped:");
        var result2 = await GetUserAsync(999)
            .TapAsync(u =>
            {
                ConsoleHelper.PrintResult("  TapAsync should NOT appear", u.Name);
                return Task.CompletedTask;
            });
        ConsoleHelper.PrintResult("Result after TapAsync on Err", result2);

        // TapErrAsync on Err — e.g., error logging
        ConsoleHelper.PrintCorrect("TapErrAsync on Err — side effect fires:");
        var result3 = await GetUserAsync(999)
            .TapErrAsync(e =>
            {
                ConsoleHelper.PrintResult("  TapErrAsync saw error", e);
                return Task.CompletedTask;
            });
        ConsoleHelper.PrintResult("Result after TapErrAsync", result3);

        ConsoleHelper.PrintCorrect("Tap/TapErr never change the Result — they are for side effects only");
    }

    private static async Task ResultOrElseAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("5. Result OrElseAsync — async fallback on error");

        static Task<Result<string, string>> LoadFromCacheAsync(string key) =>
            Task.FromResult(Result<string, string>.Err($"Cache miss for '{key}'"));

        static Task<Result<string, string>> LoadFromDbAsync(string key) =>
            Task.FromResult(Result<string, string>.Ok($"DB value for '{key}'"));

        // Cache miss -> fall back to database
        var result = await LoadFromCacheAsync("config")
            .OrElseAsync(err =>
            {
                ConsoleHelper.PrintResult("  OrElseAsync received error", err);
                return LoadFromDbAsync("config");
            });
        ConsoleHelper.PrintResult("LoadFromCacheAsync.OrElseAsync(LoadFromDbAsync)", result);

        // OrElseAsync is not called when the result is already Ok
        static Task<Result<string, string>> GetOkAsync() =>
            Task.FromResult(Result<string, string>.Ok("cached value"));

        var okResult = await GetOkAsync()
            .OrElseAsync(err =>
            {
                ConsoleHelper.PrintResult("  OrElseAsync should NOT appear", err);
                return LoadFromDbAsync("config");
            });
        ConsoleHelper.PrintResult("Ok result.OrElseAsync — factory skipped", okResult);

        ConsoleHelper.PrintCorrect("OrElseAsync is ideal for fallback strategies: cache -> DB -> default");
    }

    private static async Task FullResultAsyncPipelineDemo()
    {
        ConsoleHelper.PrintSubSection("6. Full async Result pipeline — combining everything");

        static Task<Result<User, string>> GetUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Result<User, string>.Ok(new User(1, "Alice"))
                : Result<User, string>.Err("User not found"));

        static Task<Result<Order, string>> GetOrdersAsync(int userId) =>
            Task.FromResult(userId == 1
                ? Result<Order, string>.Ok(new Order(101, 1, 249.99m))
                : Result<Order, string>.Err("No orders found"));

        // Success path: get user -> log -> get orders -> format
        var result1 = await GetUserAsync(1)
            .TapAsync(u =>
            {
                ConsoleHelper.PrintResult("  [log] Found user", u.Name);
                return Task.CompletedTask;
            })
            .BindAsync(u => GetOrdersAsync(u.Id))
            .MapAsync(o => Task.FromResult($"Order #{o.Id}: ${o.Total}"))
            .TapErrAsync(e =>
            {
                ConsoleHelper.PrintResult("  [log] Pipeline error", e);
                return Task.CompletedTask;
            });
        ConsoleHelper.PrintResult("Full pipeline (success)", result1);

        // Failure path: user not found -> error logged
        var result2 = await GetUserAsync(999)
            .TapAsync(u =>
            {
                ConsoleHelper.PrintResult("  [log] Found user", u.Name);
                return Task.CompletedTask;
            })
            .BindAsync(u => GetOrdersAsync(u.Id))
            .MapAsync(o => Task.FromResult($"Order #{o.Id}: ${o.Total}"))
            .TapErrAsync(e =>
            {
                ConsoleHelper.PrintResult("  [log] Pipeline error", e);
                return Task.CompletedTask;
            });
        ConsoleHelper.PrintResult("Full pipeline (failure)", result2);

        ConsoleHelper.PrintCorrect("Compose async pipelines by chaining BindAsync, MapAsync, TapAsync, TapErrAsync");
    }

    private static async Task OptionAsyncDemo()
    {
        ConsoleHelper.PrintSubSection("7. Option async: BindAsync / MapAsync / OrElseAsync");

        static Task<Option<User>> FindUserAsync(int id) =>
            Task.FromResult(id == 1
                ? Option<User>.Some(new User(1, "Alice"))
                : Option<User>.None);

        static Task<Option<string>> GetNicknameAsync(User user) =>
            Task.FromResult(user.Id == 1
                ? Option<string>.Some("Ally")
                : Option<string>.None);

        static Task<string> FormatNicknameAsync(string nickname) =>
            Task.FromResult($"~{nickname}~");

        static Task<Option<User>> LoadFallbackUserAsync() =>
            Task.FromResult(Option<User>.Some(new User(0, "Guest")));

        // BindAsync: chain async Option-returning operations
        var result1 = await FindUserAsync(1)
            .BindAsync(u => GetNicknameAsync(u));
        ConsoleHelper.PrintResult("FindUserAsync(1).BindAsync(GetNicknameAsync)", result1);

        var result2 = await FindUserAsync(999)
            .BindAsync(u => GetNicknameAsync(u));
        ConsoleHelper.PrintResult("FindUserAsync(999).BindAsync(GetNicknameAsync)", result2);

        // MapAsync: transform the Some value asynchronously
        var result3 = await FindUserAsync(1)
            .BindAsync(u => GetNicknameAsync(u))
            .MapAsync(FormatNicknameAsync);
        ConsoleHelper.PrintResult("FindUser(1) -> GetNickname -> FormatNickname", result3);

        // OrElseAsync: provide a fallback when None
        var result4 = await FindUserAsync(999)
            .OrElseAsync(LoadFallbackUserAsync);
        ConsoleHelper.PrintResult("FindUserAsync(999).OrElseAsync(LoadFallbackUserAsync)", result4);

        // OrElseAsync is not called when the option is Some
        var result5 = await FindUserAsync(1)
            .OrElseAsync(LoadFallbackUserAsync);
        ConsoleHelper.PrintResult("FindUserAsync(1).OrElseAsync(LoadFallbackUserAsync)", result5);

        ConsoleHelper.PrintCorrect("Option async extensions mirror the Result ones — BindAsync, MapAsync, OrElseAsync");
    }

    private static async Task ValueTaskVariantDemo()
    {
        ConsoleHelper.PrintSubSection("8. ValueTask variant — API parity with Task");

        // Define a ValueTask-based service method
        static ValueTask<Result<int, string>> ComputeAsync(int x) =>
            ValueTask.FromResult(x > 0
                ? Result<int, string>.Ok(x * 10)
                : Result<int, string>.Err("must be positive"));

        // MapAsync with ValueTask
        var result1 = await ComputeAsync(5)
            .MapAsync(v => ValueTask.FromResult($"Computed: {v}"));
        ConsoleHelper.PrintResult("ComputeAsync(5).MapAsync(format)", result1);

        var result2 = await ComputeAsync(-1)
            .MapAsync(v => ValueTask.FromResult($"Computed: {v}"));
        ConsoleHelper.PrintResult("ComputeAsync(-1).MapAsync(format)", result2);

        // BindAsync with ValueTask
        static ValueTask<Result<string, string>> ValidateAndFormatAsync(int value) =>
            ValueTask.FromResult(value > 20
                ? Result<string, string>.Ok($"Valid({value})")
                : Result<string, string>.Err($"{value} is too small"));

        var result3 = await ComputeAsync(5)
            .BindAsync(ValidateAndFormatAsync);
        ConsoleHelper.PrintResult("ComputeAsync(5).BindAsync(ValidateAndFormatAsync)", result3);

        var result4 = await ComputeAsync(1)
            .BindAsync(ValidateAndFormatAsync);
        ConsoleHelper.PrintResult("ComputeAsync(1).BindAsync(ValidateAndFormatAsync)", result4);

        ConsoleHelper.PrintCorrect("ValueTask variants have identical API — use them for high-frequency async paths to avoid heap allocations");
    }
}
