using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

// ============================================================================
// 所有 benchmark 一律同時在 net8.0 與 net10.0 上執行。
//
// 為什麼在設定檔統一指定，而不是在每個類別掛 [SimpleJob]：
// 「兩個目標框架都要量測」是本專案的硬性要求，若靠每個 benchmark 類別自行掛屬性，
// 新增類別時很容易漏掉，結果會是「只量了 host TFM，卻以為量了兩個」——比沒量更危險。
//
// 刻意不使用 Job.AsBaseline()：各 benchmark 類別已用 [Benchmark(Baseline = true)]
// 指定方法層級的基準線，兩者同時存在會讓 Ratio 欄位的意義變得模糊。
// 跨 runtime 的比較請直接讀各 Job 的 Mean 絕對值——那才能明確區分
// 「程式碼修改帶來的改善」與「.NET 10 Runtime 自身帶來的改善」。
// ============================================================================

var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80).WithId("net8.0"))
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).WithId("net10.0"))
    .AddDiagnoser(MemoryDiagnoser.Default);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
