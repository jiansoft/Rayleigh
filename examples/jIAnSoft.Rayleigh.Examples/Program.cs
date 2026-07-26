using jIAnSoft.Rayleigh.Examples;

// =====================================================================
//  Rayleigh 範例導覽
// =====================================================================
//
//  這個專案是 Rayleigh 的「可執行教學」。每個模組都會實際跑一遍程式碼，
//  把「這行寫下去會得到什麼」直接印在畫面上。
//
//  執行方式：
//    dotnet run --project examples/jIAnSoft.Rayleigh.Examples          <- 全部跑一遍
//    dotnet run --project examples/jIAnSoft.Rayleigh.Examples -- 3     <- 只跑第 3 個模組
//
//  建議的閱讀順序就是編號順序：E01 -> E12。
//  如果你完全沒接觸過 Option / Result，請務必從 E01 開始，它會先解釋
//  「為什麼需要這種東西」，而不是一上來就丟 API 給你。
//
// =====================================================================

var examples = new (string Name, string Description, Func<Task> Run)[]
{
    ("E01 Option 入門",
        "什麼是 Option、怎麼建立、怎麼判斷有沒有值",
        () => { E01OptionBasics.Run(); return Task.CompletedTask; }),

    ("E02 Option 轉換",
        "Map / Filter / Bind / Flatten，以及 Map 和 Bind 怎麼選",
        () => { E02OptionTransformations.Run(); return Task.CompletedTask; }),

    ("E03 Option 取值",
        "Match / TryGetValue / Unwrap 家族 / Or / Tap / Zip",
        () => { E03OptionAdvanced.Run(); return Task.CompletedTask; }),

    ("E04 Result 入門",
        "帶著失敗原因的回傳值，以及錯誤型別該怎麼選",
        () => { E04ResultBasics.Run(); return Task.CompletedTask; }),

    ("E05 Result 轉換",
        "Map / MapErr / Bind 與鐵路導向程式設計",
        () => { E05ResultTransformations.Run(); return Task.CompletedTask; }),

    ("E06 Result 取值",
        "Match / TryGetOk / 備援 / 日誌 / Unit 型別",
        () => { E06ResultAdvanced.Run(); return Task.CompletedTask; }),

    ("E07 Option 與 Result 互轉",
        "什麼時候用哪一個，以及怎麼在兩者之間轉換",
        () => { E07OptionResultInterop.Run(); return Task.CompletedTask; }),

    ("E08 集合操作",
        "FirstOrNone / GetValueOrNone / Sequence / Partition / Values",
        () => { E08CollectionOperations.Run(); return Task.CompletedTask; }),

    ("E09 非同步管線",
        "BindAsync / MapAsync、零配置短路、CancellationToken",
        E09AsyncPipelines.RunAsync),

    ("E10 LINQ 查詢語法",
        "用 from / where / select 操作 Option 和 Result",
        () => { E10LinqIntegration.Run(); return Task.CompletedTask; }),

    ("E11 實戰場景",
        "設定檔、表單驗證、註冊流程、批次匯入、多層快取",
        () => { E11RealWorldScenarios.Run(); return Task.CompletedTask; }),

    ("E12 常見陷阱",
        "10 個最容易犯的錯，以及正確的寫法",
        () => { E12CommonPitfalls.Run(); return Task.CompletedTask; })
};

// 有帶參數就只跑指定的那一個模組，方便針對單一主題重複研讀。
if (args.Length > 0 && int.TryParse(args[0], out var index) && index >= 1 && index <= examples.Length)
{
    var (name, _, run) = examples[index - 1];
    Console.WriteLine($"執行：{name}");
    await run();
}
else
{
    PrintMenu(examples);

    foreach (var (_, _, run) in examples)
    {
        await run();
    }

    Console.WriteLine();
    Console.WriteLine("全部模組執行完畢。");
    Console.WriteLine("提示：加上編號可以只跑其中一個，例如  dotnet run -- 8");
}

return;

// 開頭先把整份目錄印出來，讓讀者知道總共有哪些內容、自己在哪個位置。
static void PrintMenu((string Name, string Description, Func<Task> Run)[] examples)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine("  Rayleigh 範例導覽 — Rust 風格的 Option 與 Result");
    Console.WriteLine(new string('=', 78));
    Console.ResetColor();

    Console.WriteLine();
    Console.WriteLine("  建議照順序閱讀。每個模組都會實際執行程式碼並印出結果。");
    Console.WriteLine();

    for (var i = 0; i < examples.Length; i++)
    {
        var (name, description, _) = examples[i];
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"  {i + 1,2}. {name,-24}");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(description);
        Console.ResetColor();
    }
}
