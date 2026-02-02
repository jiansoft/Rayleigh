using Rayleigh.Examples;

var examples = new (string Name, Func<Task> Run)[]
{
    ("E01 - Option Basics", () => { E01OptionBasics.Run(); return Task.CompletedTask; }),
    ("E02 - Option Transformations", () => { E02OptionTransformations.Run(); return Task.CompletedTask; }),
    ("E03 - Option Advanced", () => { E03OptionAdvanced.Run(); return Task.CompletedTask; }),
    ("E04 - Result Basics", () => { E04ResultBasics.Run(); return Task.CompletedTask; }),
    ("E05 - Result Transformations", () => { E05ResultTransformations.Run(); return Task.CompletedTask; }),
    ("E06 - Result Advanced", () => { E06ResultAdvanced.Run(); return Task.CompletedTask; }),
    ("E07 - Option / Result Interop", () => { E07OptionResultInterop.Run(); return Task.CompletedTask; }),
    ("E08 - Async Pipelines", E08AsyncPipelines.RunAsync),
    ("E09 - LINQ Integration", () => { E09LinqIntegration.Run(); return Task.CompletedTask; }),
    ("E10 - Real-World Scenarios", () => { E10RealWorldScenarios.Run(); return Task.CompletedTask; }),
    ("E11 - Common Pitfalls", () => { E11CommonPitfalls.Run(); return Task.CompletedTask; }),
};

if (args.Length > 0 && int.TryParse(args[0], out var index) && index >= 1 && index <= examples.Length)
{
    var (name, run) = examples[index - 1];
    Console.WriteLine($"Running: {name}");
    await run();
}
else
{
    Console.WriteLine("Rayleigh Examples — Rust-inspired Option & Result for C#");
    Console.WriteLine($"Total: {examples.Length} example modules\n");

    foreach (var (name, run) in examples)
    {
        await run();
    }
}

Console.WriteLine("\nDone. Press any key to exit.");
Console.ReadKey(true);
