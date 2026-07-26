# Changelog

本檔案記錄 Rayleigh 的重要變更。

格式參考 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/)。
版本號採 `YY.M.D` 形式的日期版本。

---

## [26.7.26]

### 修正

- **[重要] `Result<T, TE>` 的未初始化偵測在 `TE` 為值型別時完全失效。**

  舊實作以 `_error is null` 判斷未初始化狀態：

  ```csharp
  private void ThrowIfUninitialized()
  {
      if (!IsOk && _error is null) ThrowUninitializedException();
  }
  ```

  但 `where TE : notnull` 不等同於 `where TE : struct`，`TE?` 僅是可為 null 的**標註**而非
  `Nullable<TE>`。當 `TE` 為 enum 或 struct 時，`_error is null` 會被 JIT 常數摺疊為 `false`，
  整套防護在該泛型具現化中靜默消失。

  影響尤其嚴重的原因是 enum 的預設值通常是有意義的成員：

  ```csharp
  public enum UserError { NotFound, Inactive }   // NotFound == 0

  var r = default(Result<User, UserError>);
  r.Match(ok: ..., err: ...);   // 修正前：不拋出，err 分支收到 UserError.NotFound
                                // 修正後：InvalidOperationException
  ```

  任何 `default(Result<T, TE>)`（陣列元素、未指派欄位、`List` 預留區）都會偽裝成合法的
  `Err(NotFound)`，讓呼叫端拿到看似正常的業務錯誤而無從追查來源。

  現改用明確的三態欄位 `ResultState { Uninitialized = 0, Ok = 1, Err = 2 }`，對所有 `TE` 一致生效。
  **此修正不改變 struct 大小**（實測 `Result<int, MyEnum>` 仍為 12 bytes、`Result<Guid, string>` 仍為 32 bytes），
  因為 `byte` 與原本的 `bool` 同佔 1 byte，padding 吸收。

- `EnumerableExtensions.Values()` 對 `null` 來源改為**立即**拋出 `ArgumentNullException`，
  而非延遲到列舉時才拋出 `NullReferenceException`。含 `yield return` 的方法屬延遲執行，
  若把 null 檢查寫在 iterator 內，呼叫端會在遠離錯誤來源的 `foreach` 現場才收到例外。

### 行為變更

> 以下變更可觀察，但僅影響「未初始化的 `Result`」這個本就不該出現在正常程式流程中的狀態。

- `default(Result<T, TE>).ToString()` 由 `"Err()"` 改為 **`"Uninitialized"`**。
  舊輸出在 `TE` 為 enum 時會顯示成具誤導性的 `"Err(None)"`，讓未初始化的 struct
  在偵錯視窗中看起來像一個合法的失敗結果。此為偵錯輔助輸出，不建議作為程式邏輯的判斷依據。

- `default(Result<T, TE>)` 不再等於「錯誤值恰好為 `default(TE)` 的合法 `Err`」：

  ```csharp
  default(Result<int, UserError>) == Result<int, UserError>.Err(UserError.NotFound)
  // 修正前：true（兩者無法區分）
  // 修正後：false
  ```

  兩個未初始化的 `Result` 仍彼此相等，`GetHashCode` 亦維持一致，
  不影響 `Dictionary` / `HashSet` 的不變性。

### 新增

**集合入口方法** — 從命令式集合進入 `Option` 世界。核心價值是**對值型別同樣正確**：

| 方法 | 相較於 BCL 的優勢 |
|---|---|
| `FirstOrNone()` / `FirstOrNone(predicate)` | `FirstOrDefault` 無法區分「空序列」與「第一個元素是 `default(T)`」 |
| `SingleOrNone()` | `Single()` 在元素多於一個時拋例外；本方法回傳 `None` |
| `ElementAtOrNone(index)` | 索引超出範圍（含負數）回傳 `None` 而非拋例外 |
| `GetValueOrNone(key)` | `GetValueOrDefault` 無法區分「鍵不存在」與「值為 `default(TValue)`」 |

```csharp
new[] { 0, 5 }.FirstOrNone();        // Some(0)
Array.Empty<int>().FirstOrNone();    // None
// 對照：FirstOrDefault() 對兩者都回傳 0
```

**組合子**

- `Sequence()` — `IEnumerable<Option<T>>` → `Option<List<T>>`，
  `IEnumerable<Result<T,TE>>` → `Result<List<T>, TE>`。遇第一個 `None`/`Err` 即**短路**。
- `Partition()` — `IEnumerable<Result<T,TE>>` → `(List<T> Values, List<TE> Errors)`。
  走訪**全部**並蒐集所有錯誤，適合一次回報所有驗證問題。

**非同步** — 以同步的 `Result<T,TE>` / `Option<T>` 本身為 `this` 的多載，涵蓋
`BindAsync`、`MapAsync`、`MapErrAsync`、`OrElseAsync`、`TapAsync`、`TapErrAsync`
（Option 為 `BindAsync`、`MapAsync`、`OrElseAsync`），每個都有 `CancellationToken` 版本：

```csharp
// Before：為了型別相容而多配置一個 Task
await Task.FromResult(Validate(input)).BindAsync(v => SaveAsync(v));

// After
await Validate(input).BindAsync(v => SaveAsync(v));
```

這些多載刻意**不**宣告為 `async`：短路（`Err`／`None`）時直接回傳同步完成的 `ValueTask`，
不建立 async 狀態機，零配置。實測（BenchmarkDotNet，AMD Ryzen 9 5950X／.NET 10.0.9）：

| 寫法 | Allocated |
|---|---|
| `Task.FromResult(err).BindAsync(...)`（舊寫法） | 240 B |
| `err.BindAsync(...)`（新多載，短路） | **0 B** |

### 效能與內部改善

- 所有 throw helper（4 處）加上 `MethodImplOptions.NoInlining`，避免「建立例外物件」的冷路徑 IL
  被內聯進 `AggressiveInlining` 的熱路徑方法而撐大體積、反過來影響 JIT 的內聯決策。
  與 .NET runtime 自身 `ThrowHelper` 的做法一致。
- `Result.SelectMany` 與全部非同步擴充（約 28 處）改用單次 `TryGetOk` / `TryGetValue`，
  消除同一個值被重複驗證與分支多達三次的情況。
- `Option.Zip` / `ZipWith` 消除對第二個運算元的重複狀態檢查。

實測數據（BenchmarkDotNet 0.15.8／AMD Ryzen 9 5950X／.NET 10.0.9／ShortRun）：

| 項目 | Mean | Allocated |
|---|---:|---:|
| `Result.Ok(v)` 建立 | 0.13 ns | 0 B |
| `Result.UnwrapOr` on `Err`（含未初始化防護） | 0.31 ns | 0 B |
| `Option.Unwrap` on `Some` | ~0 ns | 0 B |
| `FirstOrNone`（`IList` 快速路徑，n=1024） | 0.09 ns | 0 B |
| `FirstOrDefault`（LINQ 對照組，n=1024） | 0.58 ns | 0 B |
| `GetValueOrNone` vs `GetValueOrDefault` | 5.5 ns vs 4.5 ns | 0 B / 0 B |
| `Sequence` 短路（Err 在中點，n=1024） | 809 ns | 4152 B |
| `Sequence` 全量（全 Ok，n=1024） | 1552 ns | 4152 B |

新增的三態 `ResultState` 未對成功路徑造成可觀測成本；`FirstOrNone` 在提供更強語意的同時
反而略快於 `FirstOrDefault`（不建立 enumerator）。

> **關於數據精度**：上表以 ShortRun（3 次迭代）取得，`Mean` 的誤差區間偏大，僅供方向性參考；
> `Allocated` 欄位則是由 GC 計數器確定性量測，數值精確可信。
> 若需可引用的精確耗時，請以預設 job（移除 `--job short`）重跑。

**為何錯誤路徑該用 `Result` 而非例外**——同一個錯誤情境的兩種寫法：

| 寫法 | Mean | Allocated |
|---|---:|---:|
| `result.UnwrapOr(-1)`（Result 錯誤路徑） | 0.16 ns | 0 B |
| `try { result.Unwrap(); } catch { }`（例外驅動） | 2,100 ns | 384 B |

約 13,000 倍的差距，這正是 `Result<T, E>` 存在的理由。

### 其他

- `Unit` 補上非泛型 `IComparable`，與 `Option` / `Result` 保持一致。
- 例外訊息改用 `typeof(...)` 呈現完整泛型型別名稱（原本 `nameof` 只產生 `"Option"` / `"Result"`，
  遺漏泛型參數）；並補上 `paramName`。
- 移除 `Option<T>` 多餘的顯式無參數建構子（C# 10 的 struct 無參數建構子不會被
  `default(T)`、陣列配置或 `Activator.CreateInstance<T>()` 呼叫，保留它只會製造誤解）。
- 建置設定鎖定 `<LangVersion>12.0</LangVersion>`，即 net8.0 與 net10.0 的語言版本交集。
  在此之前 net8.0 以 C# 12、net10.0 以 C# 14 編譯，使用 C# 13+ 語法會「只有 net8.0 建置失敗」，
  問題延遲到 CI 才浮現。
- 文件：`Result<T, TE>` 補上「`T` 與 `TE` 不可為相同型別」的已知限制說明
  （`Result<string, string>` 使用隱式轉換會得到 `CS0457`，因兩個 implicit operator 簽章重複），
  並提供三種 workaround。

### 範例專案

`examples/` 全面改寫為中文教學版，並重新編號為 E01–E12：

- 每個模組先解釋<b>為什麼</b>需要這個 API，再示範怎麼用；註解密度大幅提高，以初學者為對象。
- 輸出改為「程式碼 → 執行結果」對照，並標示推薦／避免的寫法。
- 新增 **E08 集合操作**（`FirstOrNone`、`GetValueOrNone`、`Sequence`、`Partition`、`Values`），
  含與 `FirstOrDefault` / `GetValueOrDefault` 的實際對照。
- **E09 非同步**新增 sync-source 多載示範，並實際印出短路時 `ValueTask.IsCompletedSuccessfully` 為 `true`。
- **E12 常見陷阱**擴充為 10 項，其中「用例外處理預期內錯誤」會當場跑一次計時對照。
- 原 E08–E11 順延為 E09–E12。

### 測試

測試數由 433 增加至 533。主要補強：

- **值型別 `TE` 的未初始化覆蓋**（先前完全缺席，正是上述 bug 得以存在的原因）：
  29 個操作 × enum `TE`，外加自訂 `record struct` 錯誤型別的驗證。
- 新集合方法的完整測試，包含與 `FirstOrDefault` / `GetValueOrDefault` 的**對照組**斷言。
- sync-source 非同步多載的零配置驗證：斷言短路時 `ValueTask.IsCompletedSuccessfully` 為 `true`——
  若日後有人將這些多載改成 `async`，測試會立即失敗。
- 補上長期缺乏覆蓋的輔助型別：`OptionNone`（`Option.None` 萬用標記的實際型別）、
  `Option` 靜態工廠、`Ok<T>` / `Err<TE>` 包裹記錄，以及 `Unit` 新增的非泛型 `IComparable`。

---

## [26.5.29] 及更早

初始版本與後續迭代，詳見 Git 歷史。
