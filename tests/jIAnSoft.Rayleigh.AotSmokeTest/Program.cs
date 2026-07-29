using jIAnSoft.Rayleigh;

// ============================================================================
// Native AOT / trimming 煙霧測試
//
// 每個檢查都刻意走過一種「AOT 下最容易出問題」的具現化形態：
//   - 值型別泛型引數（各自產生獨立的原生程式碼）
//   - 參考型別泛型引數（共用具現化）
//   - 巢狀泛型（Option<Option<T>>、Result<List<T>, TE>）
//   - enum 作為錯誤型別（未初始化偵測的關鍵路徑）
//   - async 狀態機（AsyncValueTaskMethodBuilder 在 AOT 下的行為）
//   - 集合快速路徑（span / CollectionsMarshal）
//   - 例外路徑（AOT 下的例外處理與訊息格式化）
//
// 任一檢查失敗即以非零離開碼結束，讓 CI 能直接判定。
// ============================================================================

var failures = new List<string>();

void Check(string name, Func<bool> assertion)
{
    try
    {
        if (assertion())
        {
            Console.WriteLine($"  PASS  {name}");
        }
        else
        {
            failures.Add(name);
            Console.WriteLine($"  FAIL  {name}");
        }
    }
    catch (Exception ex)
    {
        failures.Add($"{name} ({ex.GetType().Name}: {ex.Message})");
        Console.WriteLine($"  FAIL  {name} — {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine($"Rayleigh AOT smoke test — runtime {Environment.Version}");

// --- Option：值型別與參考型別各一 ---
Check("Option<int> Some/Map/Bind/Match", () =>
    Option<int>.Some(21)
        .Map(static x => x * 2)
        .Bind(static x => x > 0 ? Option<int>.Some(x) : Option<int>.None)
        .Match(static v => v, static () => -1) == 42);

Check("Option<string> None 短路", () =>
    Option<string>.None.Map(static s => s.Length).IsNone);

Check("Option<Guid> 較大 value type", () =>
    Option<Guid>.Some(Guid.Empty).UnwrapOr(Guid.NewGuid()) == Guid.Empty);

Check("Option 巢狀 Flatten", () =>
    Option<Option<int>>.Some(Option<int>.Some(7)).Flatten().Unwrap() == 7);

Check("Option 相等性與雜湊", () =>
    Option<int>.Some(1) == Option<int>.Some(1)
    && Option<int>.Some(1).GetHashCode() == Option<int>.Some(1).GetHashCode());

Check("Option 排序（None < Some）", () => Option<int>.None < Option<int>.Some(0));

// --- Result：enum 錯誤型別是未初始化偵測的關鍵路徑 ---
Check("Result<int, AotError> Ok 鏈", () =>
    Result<int, AotError>.Ok(21).Map(static x => x * 2).Unwrap() == 42);

Check("Result<int, AotError> Err 短路", () =>
    Result<int, AotError>.Err(AotError.NotFound).Map(static x => x * 2).UnwrapErr() == AotError.NotFound);

Check("default(Result) 對 enum 錯誤型別中毒化", () =>
{
    try
    {
        _ = default(Result<int, AotError>).Unwrap();
        return false;
    }
    catch (InvalidOperationException)
    {
        return true;
    }
});

Check("default(Result).Deconstruct 中毒化", () =>
{
    try
    {
        var (_, _, _) = default(Result<int, AotError>);
        return false;
    }
    catch (InvalidOperationException)
    {
        return true;
    }
});

Check("IsUninitialized 可安全區分未初始化與合法 Err", () =>
    default(Result<int, AotError>).IsUninitialized
    && !Result<int, AotError>.Err(default).IsUninitialized);

Check("Result.ToString 格式化（AOT 下的字串內插）", () =>
    Result<int, string>.Ok(1).ToString() == "Ok(1)"
    && default(Result<int, string>).ToString() == "Uninitialized");

Check("Result 巢狀 Flatten", () =>
    Result<Result<int, string>, string>.Ok(Result<int, string>.Ok(9)).Flatten().Unwrap() == 9);

Check("Ok/Err 包裹記錄的隱式轉換", () =>
{
    Result<int, string> ok = new Ok<int>(5);
    Result<int, string> err = new Err<string>("boom");
    return ok.Unwrap() == 5 && err.UnwrapErr() == "boom";
});

// --- 集合快速路徑：span / CollectionsMarshal 在 AOT 下的行為 ---
Check("Sequence 陣列快速路徑", () =>
    new[] { Result<int, string>.Ok(1), Result<int, string>.Ok(2) }.Sequence().Unwrap().Count == 2);

Check("Sequence List 快速路徑", () =>
    new List<Option<int>> { Option<int>.Some(1), Option<int>.Some(2) }.Sequence().Unwrap().Count == 2);

Check("Sequence 延遲序列路徑", () =>
    Enumerable.Range(0, 4).Select(Result<int, string>.Ok).Sequence().Unwrap().Count == 4);

Check("Partition 拆分", () =>
{
    var (values, errors) = new[]
    {
        Result<int, string>.Ok(1),
        Result<int, string>.Err("e"),
        Result<int, string>.Ok(3)
    }.Partition();
    return values.Count == 2 && errors.Count == 1;
});

Check("Values() 迭代器", () =>
    new[] { Option<int>.Some(1), Option<int>.None, Option<int>.Some(3) }.Values().Sum() == 4);

Check("集合入口方法", () =>
    new[] { 0, 1, 2 }.FirstOrNone().Unwrap() == 0
    && new[] { 0, 1, 2 }.FirstOrNone(static x => x == 2).Unwrap() == 2
    && new[] { 5 }.SingleOrNone().Unwrap() == 5
    && new[] { 1, 2 }.ElementAtOrNone(1).Unwrap() == 2
    && new Dictionary<string, int> { ["a"] = 0 }.GetValueOrNone("a").Unwrap() == 0);

// --- Nullable 互通 ---
Check("Nullable 互通", () =>
{
    int? nullable = 3;
    string? text = null;
    return nullable.ToOption().Unwrap() == 3
           && text.ToOption().IsNone
           && Option<int>.Some(3).OrNull() == 3;
});

// --- LINQ 查詢語法（編譯器產生的 SelectMany 具現化） ---
Check("LINQ 查詢語法", () =>
{
    var query = from a in Result<int, string>.Ok(2)
                from b in Result<int, string>.Ok(3)
                select a * b;
    return query.Unwrap() == 6;
});

// --- async：AOT 下的 async 狀態機 ---
Check("async ValueTask 管線（sync-source 短路）", () =>
    Result<int, string>.Err("boom")
        .BindAsync(static x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)))
        .AsTask().GetAwaiter().GetResult().UnwrapErr() == "boom");

Check("async ValueTask 管線（真正非同步）", () =>
    RunAsync().GetAwaiter().GetResult() == 84);

Check("async Task 管線", () =>
    Task.FromResult(Option<int>.Some(21))
        .MapAsync(static x => Task.FromResult(x * 2))
        .GetAwaiter().GetResult().Unwrap() == 42);

// --- Unit ---
Check("Unit", () => Unit.Value == default && Unit.Value.ToString() == "()");

Console.WriteLine();
if (failures.Count > 0)
{
    Console.WriteLine($"AOT smoke test FAILED — {failures.Count} 項未通過：");
    foreach (var failure in failures)
    {
        Console.WriteLine($"  - {failure}");
    }

    return 1;
}

Console.WriteLine("AOT smoke test PASSED — 所有檢查通過。");
return 0;

static async Task<int> RunAsync()
{
    var result = await Result<int, string>.Ok(42)
        .MapAsync<int, int, string>(static async x =>
        {
            await Task.Yield(); // 強制真正暫停，讓 async 狀態機在 AOT 下實際被配置與執行
            return x * 2;
        });

    return result.Unwrap();
}

internal enum AotError
{
    NotFound,
    Invalid
}
