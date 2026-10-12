using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using jIAnSoft.Rayleigh.Comparison;

if (args is ["--verify-bugs"])
{
    await BugReproduction.Run();
    return;
}

// Both versions are referenced by one executable with identical inputs and runtime settings.
// BenchmarkDotNet launches a separate process for each measured case.
// Override iteration settings through BDN CLI for longer confirmation runs.
var config = DefaultConfig.Instance
    .AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory)
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80).WithId("net8.0"))
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).WithId("net10.0"))
    .AddDiagnoser(MemoryDiagnoser.Default);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
