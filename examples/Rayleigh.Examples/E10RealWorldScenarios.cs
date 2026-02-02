using jIAnSoft.Rayleigh;
using Rayleigh.Examples.Helpers;

namespace Rayleigh.Examples;

file record CreateUserRequest(string Name, string Email);

file record UserDto(int Id, string Name, string Email);

public static class E10RealWorldScenarios
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("E10 - Real-World Scenarios: Practical Patterns with Option & Result");

        SafeDictionaryLookup();
        FormValidationPipeline();
        UserRegistrationPipeline();
        ConfigurationLoadingWithFallbacks();
        BatchProcessingWithErrorCollection();
    }

    // -------------------------------------------------------
    // Scenario 1: Safe Dictionary Lookup
    // -------------------------------------------------------

    private static void SafeDictionaryLookup()
    {
        ConsoleHelper.PrintSubSection("1. Safe Dictionary Lookup");

        var inventory = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["apple"] = 50,
            ["banana"] = 120,
            ["cherry"] = 30
        };

        // Helper: wraps Dictionary.TryGetValue into Option<TValue>
        static Option<TValue> TryGet<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
            where TKey : notnull
            where TValue : notnull
        {
            return dict.TryGetValue(key, out var value)
                ? Option<TValue>.Some(value)
                : Option<TValue>.None;
        }

        // Look up existing key
        var appleStock = TryGet(inventory, "apple");
        ConsoleHelper.PrintResult("TryGet(inventory, \"apple\")", appleStock);

        // Look up non-existing key
        var mangoStock = TryGet(inventory, "mango");
        ConsoleHelper.PrintResult("TryGet(inventory, \"mango\")", mangoStock);

        // Chain with Map to transform the value
        var appleMessage = TryGet(inventory, "apple")
            .Map(qty => qty > 100 ? "Well stocked" : "Running low")
            .UnwrapOr("Item not found");
        ConsoleHelper.PrintResult("apple stock status", appleMessage);

        var bananaMessage = TryGet(inventory, "banana")
            .Map(qty => qty > 100 ? "Well stocked" : "Running low")
            .UnwrapOr("Item not found");
        ConsoleHelper.PrintResult("banana stock status", bananaMessage);

        var mangoMessage = TryGet(inventory, "mango")
            .Map(qty => qty > 100 ? "Well stocked" : "Running low")
            .UnwrapOr("Item not found");
        ConsoleHelper.PrintResult("mango stock status", mangoMessage);

        ConsoleHelper.PrintCorrect("TryGet wraps TryGetValue into Option, enabling safe fluent chains");
    }

    // -------------------------------------------------------
    // Scenario 2: Form Validation Pipeline
    // -------------------------------------------------------

    private static void FormValidationPipeline()
    {
        ConsoleHelper.PrintSubSection("2. Form Validation Pipeline");

        // Step 1: Parse input string to int
        static Result<int, string> ParseInt(string input)
        {
            if (int.TryParse(input, out var n)) return n;
            return $"'{input}' is not a valid integer";
        }

        // Step 2: Validate positive (> 0)
        static Result<int, string> ValidatePositive(int n)
        {
            if (n > 0) return n;
            return $"{n} is not positive";
        }

        // Step 3: Validate range
        static Result<int, string> ValidateRange(int n, int min, int max)
        {
            if (n >= min && n <= max) return n;
            return $"{n} is out of range [{min}..{max}]";
        }

        // Build the pipeline: ParseInt -> ValidatePositive -> ValidateRange(1, 120)
        static Result<int, string> ValidateAge(string input) =>
            ParseInt(input)
                .Bind(ValidatePositive)
                .Bind(n => ValidateRange(n, 1, 120));

        // Test cases
        string[] testInputs = ["25", "abc", "-5", "200"];

        foreach (var input in testInputs)
        {
            var result = ValidateAge(input);
            ConsoleHelper.PrintResult($"ValidateAge(\"{input}\")", result);
        }

        ConsoleHelper.PrintCorrect("Each Bind short-circuits on the first error -- no wasted work");
    }

    // -------------------------------------------------------
    // Scenario 3: User Registration Pipeline (full ROP)
    // -------------------------------------------------------

    private static void UserRegistrationPipeline()
    {
        ConsoleHelper.PrintSubSection("3. User Registration Pipeline (Railway-Oriented Programming)");

        // Simulated database of existing emails
        HashSet<string> existingEmails = ["alice@example.com", "bob@example.com"];
        var nextId = 1;

        // Step 1: Validate name
        static Result<CreateUserRequest, string> ValidateName(CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return "Name cannot be empty";
            if (req.Name.Length < 2)
                return "Name must be at least 2 characters";
            return req;
        }

        // Step 2: Validate email
        static Result<CreateUserRequest, string> ValidateEmail(CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return "Email cannot be empty";
            if (!req.Email.Contains('@'))
                return "Email must contain '@'";
            return req;
        }

        // Step 3: Check uniqueness (captures existingEmails)
        Result<CreateUserRequest, string> CheckUniqueness(CreateUserRequest req)
        {
            if (existingEmails.Contains(req.Email))
                return $"Email '{req.Email}' is already registered";
            return req;
        }

        // Step 4: Save user (captures nextId, existingEmails)
        Result<CreateUserRequest, string> SaveUser(CreateUserRequest req)
        {
            // Simulate persisting to database
            existingEmails.Add(req.Email);
            nextId++;
            return req;
        }

        // Step 5: Map to DTO
        Result<UserDto, string> RegisterUser(CreateUserRequest req) =>
            ValidateName(req)
                .Bind(ValidateEmail)
                .Bind(CheckUniqueness)
                .Bind(SaveUser)
                .Map(r => new UserDto(nextId, r.Name, r.Email));

        // Test cases
        var validRequest = new CreateUserRequest("Charlie", "charlie@example.com");
        var emptyName = new CreateUserRequest("", "test@example.com");
        var badEmail = new CreateUserRequest("Dave", "not-an-email");
        var duplicateEmail = new CreateUserRequest("Eve", "alice@example.com");

        ConsoleHelper.PrintResult("Register(Charlie, charlie@...)", RegisterUser(validRequest));
        ConsoleHelper.PrintResult("Register(\"\", test@...)", RegisterUser(emptyName));
        ConsoleHelper.PrintResult("Register(Dave, not-an-email)", RegisterUser(badEmail));
        ConsoleHelper.PrintResult("Register(Eve, alice@...)", RegisterUser(duplicateEmail));

        ConsoleHelper.PrintCorrect("Full ROP: each step returns Result, errors short-circuit the pipeline");
    }

    // -------------------------------------------------------
    // Scenario 4: Configuration Loading with Fallbacks
    // -------------------------------------------------------

    private static void ConfigurationLoadingWithFallbacks()
    {
        ConsoleHelper.PrintSubSection("4. Configuration Loading with Fallbacks");

        // Simulated file load -- fails for non-existent paths
        // Note: When T and TE are the same type, implicit conversions are ambiguous.
        // Use Ok<T>/Err<TE> wrappers to disambiguate (see also Pitfall #5 in E11).
        static Result<string, string> LoadFromFile(string path)
        {
            if (path == "config.json")
                return new Ok<string>("file:timeout=5000");
            return new Err<string>($"File '{path}' not found");
        }

        // Simulated environment variable -- only some keys exist
        static Result<string, string> LoadFromEnv(string key)
        {
            if (key == "APP_TIMEOUT")
                return new Ok<string>("env:timeout=3000");
            return new Err<string>($"Environment variable '{key}' not set");
        }

        // Default value -- always succeeds
        static Result<string, string> DefaultValue()
        {
            return new Ok<string>("default:timeout=1000");
        }

        // Chain: file -> env -> default
        static Result<string, string> LoadConfig(string filePath, string envKey) =>
            LoadFromFile(filePath)
                .OrElse(_ => LoadFromEnv(envKey))
                .OrElse(_ => DefaultValue());

        // Case 1: File exists -- uses file value
        var config1 = LoadConfig("config.json", "APP_TIMEOUT");
        ConsoleHelper.PrintResult("LoadConfig(\"config.json\", ...)", config1);

        // Case 2: File missing, env exists -- falls through to env
        var config2 = LoadConfig("missing.json", "APP_TIMEOUT");
        ConsoleHelper.PrintResult("LoadConfig(\"missing.json\", \"APP_TIMEOUT\")", config2);

        // Case 3: Both missing -- falls through to default
        var config3 = LoadConfig("missing.json", "MISSING_KEY");
        ConsoleHelper.PrintResult("LoadConfig(\"missing.json\", \"MISSING_KEY\")", config3);

        ConsoleHelper.PrintCorrect("OrElse chains provide graceful fallback without nested if/else");
    }

    // -------------------------------------------------------
    // Scenario 5: Batch Processing with Error Collection
    // -------------------------------------------------------

    private static void BatchProcessingWithErrorCollection()
    {
        ConsoleHelper.PrintSubSection("5. Batch Processing with Error Collection");

        // Reusable validation pipeline for a single input
        static Result<int, string> ProcessOne(string input)
        {
            if (!int.TryParse(input, out var n))
                return $"'{input}' is not a number";
            if (n <= 0)
                return $"{n} must be positive";
            if (n > 100)
                return $"{n} exceeds maximum of 100";
            return n;
        }

        // Batch of mixed inputs
        string[] inputs = ["42", "abc", "7", "-3", "100", "xyz", "200"];

        // Process all inputs
        var results = inputs.Select(ProcessOne).ToList();

        // Collect successes: convert each Result to Option<int> via ToOption, then extract values
        var successes = results.Select(r => r.ToOption()).Values().ToList();

        // Collect errors: convert each Result to Option<string> via .Err(), then extract values
        var errors = results.Select(r => r.Err()).Values().ToList();

        // Print individual results
        for (var i = 0; i < inputs.Length; i++)
        {
            ConsoleHelper.PrintResult($"ProcessOne(\"{inputs[i]}\")", results[i]);
        }

        // Print summary
        ConsoleHelper.PrintResult("Successes", $"[{string.Join(", ", successes)}]");
        ConsoleHelper.PrintResult("Errors", $"[{string.Join(", ", errors)}]");
        ConsoleHelper.PrintResult("Summary", $"{successes.Count} succeeded, {errors.Count} failed");

        ConsoleHelper.PrintCorrect("ToOption().Values() collects successes; .Err().Values() collects errors");
    }
}
