[English](README.md) | **繁體中文**

# Rayleigh

[![NuGet version](https://img.shields.io/nuget/v/jIAnSoft.Rayleigh.svg?logo=nuget)](https://www.nuget.org/packages/jIAnSoft.Rayleigh/)

受 Rust 啟發的 C# Option 與 Result 型別。

專為 .NET 8 與 .NET 10 打造的零分配、高效能函數式基礎型別庫。

## 為什麼選擇 Rayleigh？

C# 的[可空參考型別](https://learn.microsoft.com/zh-tw/dotnet/csharp/nullable-references)功能雖然有用，
但它完全是可選的，產生的警告也很容易被忽略。`Result<T, E>` 更進一步，將錯誤處理明確嵌入型別系統中。

Rayleigh 透過型別系統來：

- **防止 null 參考錯誤** — 讓你無法在未檢查值是否存在的情況下存取可能缺失的值。
- **清楚表達意圖** — 如果方法可能不回傳值、或可能回傳錯誤，回傳型別會讓這一點顯而易見，不可能被忽略。
- **安全地串接操作** — 使用 `Map`、`Bind` 等組合子建構管線，在遇到 `None` 或 `Err` 時自動短路，消除巢狀的 `if`/`else` 區塊。
- **避免以例外驅動流程控制** — 將 `Result<T, E>` 用於預期的業務錯誤（驗證失敗、找不到資料等），將例外保留給真正非預期的系統錯誤。

## 安裝

```bash
dotnet add package jIAnSoft.Rayleigh
```

或透過 NuGet 套件管理員：

```
Install-Package jIAnSoft.Rayleigh
```

## 何時使用 Option 或 Result

當「沒有值」是正常結果，而且呼叫端不需要知道缺值原因時，使用 `Option<T>`。
當失敗需要原因，且呼叫端可能要記錄、顯示、從 API 回傳、重試或依錯誤分流處理時，使用 `Result<T, E>`。

| 情境 | 使用 |
|---|---|
| 查詢可能找到值，也可能找不到 | `Option<T>` |
| 可空值需要變成明確型別 | `Option<T>` |
| 缺值是預期狀態，不是錯誤 | `Option<T>` |
| 驗證、授權、解析或 I/O 可能失敗 | `Result<T, E>` |
| 呼叫端需要錯誤原因 | `Result<T, E>` |
| 失敗需要被記錄、顯示、回傳、重試，或依錯誤型別做不同處理 | `Result<T, E>` |

簡單說：`Option<T>` 表示可能有值；`Result<T, E>` 表示成功或有原因的失敗。

## 使用方式

### 建立 Option

```csharp
using jIAnSoft.Rayleigh;

// 從值建立
var some = Option<int>.Some(42);
var none = Option<int>.None;

// 使用隱式轉換（推薦寫法）
Option<int> implicitSome = 42;          // 隱式轉為 Some(42)
Option<int> implicitNone = Option.None; // 萬用 None 標記

// 從可空參考型別轉換
string? name = GetName();
var option = name.ToOption();  // Some("Alice") 或 None

// 從可空值型別轉換
int? maybeAge = GetAge();
var ageOption = maybeAge.ToOption();  // Some(25) 或 None
```

### 從 Option 取值

```csharp
// TryGetValue — 防禦性檢查風格
if (!option.TryGetValue(out var value))
{
    return; // 提前返回，沒有值
}
// 此處 value 保證非 null

// Match — 窮盡式處理兩種情況
var message = option.Match(
    some: v => $"你好，{v}！",
    none: () => "你好，訪客！"
);

// UnwrapOr — 提供預設值
var name = option.UnwrapOr("未知");

// Deconstruct — 在 switch 中使用模式比對
var result = option switch
{
    (true, var v)  => $"值：{v}",
    (false, _)     => "無值"
};
```

### 建立 Result

```csharp
using jIAnSoft.Rayleigh;

// 使用工廠方法
var ok  = Result<int, string>.Ok(42);
var err = Result<int, string>.Err("發生錯誤");

// 使用隱式轉換（推薦寫法）
Result<int, string> Divide(int a, int b)
{
    if (b == 0) return "除數不可為零";  // 隱式轉為 Err
    return a / b;                        // 隱式轉為 Ok
}

// 使用 Ok<T> / Err<E> 包裝記錄
Result<int, string> fromWrapper = new Ok<int>(42);
Result<int, string> fromErrWrapper = new Err<string>("錯誤");
```

### 從 Result 取值

```csharp
// TryGetOk — 防禦性檢查風格（推薦）
if (!result.TryGetOk(out var value, out var error))
{
    return BadRequest(error); // 提前返回並帶錯誤資訊
}
// 此處 value 保證非 null

// Match — 窮盡式處理
var response = result.Match(
    ok:  user  => $"歡迎，{user.Name}！",
    err: error => $"錯誤：{error}"
);

// UnwrapOr — 提供預設值
var timeout = GetConfig("Timeout").UnwrapOr(30);
```

### 安全地串接可能失敗的方法

Rayleigh 支援安全地串接多個回傳 `Option` 或 `Result` 的方法，
並在需要時輕鬆轉換 `Option` 與 `Result`。

```csharp
var output = ValidateInput(userInput)
    .Bind(validated => FindUser(validated.UserId))
    .Filter(user => user.IsActive)
    .Map(user => user.Name)
    .UnwrapOr("訪客");
```

上述範例依序執行：

1. 驗證使用者輸入。若驗證失敗，回傳 `None` / `Err`。
2. 若驗證成功，查詢使用者。若找不到使用者，回傳 `None`。
3. 若找到使用者，檢查是否為活躍狀態。若非活躍，變為 `None`。
4. 若使用者為活躍狀態，取出名稱。
5. 最終若有值則回傳，否則回傳 `"訪客"`。

全程不可能出現 `null` 參考，也沒有巢狀的 `if` / `else` 區塊。

### 鐵路導向程式設計（Railway-Oriented Programming）

```csharp
// 定義回傳 Result 的方法
Result<UserDto, AppError> CreateUser(CreateUserRequest request)
{
    return ValidateName(request.Name)
        .Bind(name => ValidateEmail(request.Email).Map(email => (name, email)))
        .Bind(pair => SaveToDatabase(pair.name, pair.email))
        .Map(entity => entity.ToDto())
        .Tap(dto => logger.LogInformation("已建立使用者 {Id}", dto.Id))
        .TapErr(err => logger.LogError("失敗：{Error}", err));
}

// LINQ 查詢語法 — 等同於串接的 Bind 呼叫
var total = from user  in GetUser(userId)
            from order in GetLatestOrder(user.Id)
            select order.Total;
```

### 非同步管線

當操作涉及 I/O（資料庫查詢、HTTP 呼叫、檔案存取等）時，
你需要非同步版本的組合子。Rayleigh 提供 `Task` 和 `ValueTask` 擴充方法，
讓你以相同的鐵路導向風格串接非同步操作 —
錯誤自動傳播，不需要巢狀的 `try`/`catch` 或 `if` 區塊。

#### BindAsync — 串接可能失敗的非同步操作

`BindAsync` 接受一個回傳 `Task<Result<TU, TE>>`（或 `Task<Option<TU>>`）的函式。
若來源為 `Ok`/`Some`，則呼叫該函式；若為 `Err`/`None`，則直接短路跳過。

```csharp
// 每一步回傳 Task<Result<T, E>>，錯誤自動傳播
var user = await ValidateTokenAsync(token)        // Task<Result<UserId, ApiError>>
    .BindAsync(id => FindUserAsync(id))           // -> Task<Result<User, ApiError>>
    .BindAsync(user => LoadPermissionsAsync(user)) // -> Task<Result<UserWithPerms, ApiError>>
```

#### MapAsync — 非同步轉換成功值

`MapAsync` 轉換內部值，但不改變 Result/Option 的結構。
與 `BindAsync` 不同，mapper 回傳的是純 `Task<TU>`，而非包裝型別。

```csharp
var dto = await GetUserAsync(userId)              // Task<Result<User, string>>
    .MapAsync(user => EnrichWithAvatarAsync(user)) // -> Task<Result<UserDto, string>>
```

#### TapAsync / TapErrAsync — 非同步副作用

執行非同步副作用（日誌、通知、指標）但不改變值。
`TapAsync` 在成功時觸發，`TapErrAsync` 在錯誤時觸發。

```csharp
var result = await CreateOrderAsync(request)
    .TapAsync(order => SendConfirmationEmailAsync(order))
    .TapErrAsync(err => AlertOpsChannelAsync(err));
// result 不變 — 副作用已執行但不影響值
```

#### OrElseAsync — 非同步備援

當原始操作失敗時，提供替代方案。

```csharp
var data = await LoadFromCacheAsync(key)
    .OrElseAsync(err => LoadFromDatabaseAsync(key));
// 若快取未命中（Err），則退而從資料庫載入
```

#### MapErrAsync — 非同步轉換錯誤

將一種錯誤型別轉換為另一種（例如，從非同步來源補充錯誤上下文）。

```csharp
var result = await CallExternalApiAsync(request)
    .MapErrAsync(err => EnrichErrorWithTraceAsync(err));
```

#### 完整範例 — 組合所有方法

```csharp
Result<OrderConfirmation, AppError> confirmation = await ValidateOrderAsync(request)
    .BindAsync(order => CheckInventoryAsync(order))
    .BindAsync(order => ProcessPaymentAsync(order))
    .MapAsync(receipt => BuildConfirmationAsync(receipt))
    .TapAsync(conf => SendEmailAsync(conf.Email, conf))
    .TapErrAsync(err => logger.LogErrorAsync("Order failed: {Error}", err))
    .OrElseAsync(err => CreatePendingOrderAsync(request, err));
```

上述管線依序執行：

1. **ValidateOrderAsync** — 驗證傳入的請求。若無效則回傳 `Err`。
2. **CheckInventoryAsync** — 檢查庫存。若缺貨則回傳 `Err`。
3. **ProcessPaymentAsync** — 向客戶收款。若付款失敗則回傳 `Err`。
4. **BuildConfirmationAsync** — 將付款收據轉換為確認 DTO。
5. **SendEmailAsync** — 寄送確認信（副作用，不改變值）。
6. **LogErrorAsync** — 若任何步驟失敗，記錄錯誤（錯誤路徑上的副作用）。
7. **CreatePendingOrderAsync** — 備援：若任何步驟失敗，改為建立待處理訂單。

每一步只在前一步成功時才會執行。錯誤自動傳播，不需要任何 `try`/`catch` 或 `if`/`else` 巢狀結構。

> **`Task` vs `ValueTask`**：上述每個方法都有對應的 `ValueTask` 多載。
> 在高頻路徑中使用 `ValueTask`，可在結果經常同步可用時避免堆積分配。

> **`CancellationToken`**：每個非同步組合子也都提供一個多載，其委派的最後一個參數接受 `CancellationToken`，
> 組合子本身也多一個尾端的 `CancellationToken` 參數：
> ```csharp
> var result = await GetUserAsync(userId)
>     .BindAsync((user, ct) => GetOrdersAsync(user.Id, ct), cancellationToken);
> ```
> 只有在真的要執行委派時才會檢查（並轉發）取消狀態——短路的 `Err`/`None` 分支不會觀察到取消，
> 與函式庫其他部分的 no-op 語意一致。

### 集合操作

#### 安全地進入 Option 世界

BCL 的 `FirstOrDefault` 與 `GetValueOrDefault` 無法區分「找不到」與「找到了一個恰好等於預設值的元素」。
對值型別而言，這會靜默地遺失資訊：

```csharp
var scores = new[] { 0, 5, 10 };

scores.FirstOrDefault();             // 0  ─┬─ 兩者無從區分
Array.Empty<int>().FirstOrDefault(); // 0  ─┘

scores.FirstOrNone();                // Some(0)
Array.Empty<int>().FirstOrNone();    // None
```

字典查詢也是同樣的問題：

```csharp
var counts = new Dictionary<string, int> { ["a"] = 0 };

counts.GetValueOrDefault("a");     // 0  ─┬─ 兩者無從區分
counts.GetValueOrDefault("miss");  // 0  ─┘

counts.GetValueOrNone("a");        // Some(0)
counts.GetValueOrNone("miss");     // None
```

完整的入口方法：

```csharp
source.FirstOrNone();               // Option<T>
source.FirstOrNone(x => x > 10);    // Option<T>，找到第一個符合者即短路
source.SingleOrNone();              // 空或多於一個皆為 None（永不拋例外）
source.ElementAtOrNone(3);          // 超出範圍為 None（含負數索引）
dictionary.GetValueOrNone(key);     // Option<TValue>
```

#### `Sequence()` — 全有或全無

把「集合的 Option/Result」反轉為「Option/Result 的集合」。遇到第一個 `None`/`Err` 即短路：

```csharp
Option<int>[] all  = [Option<int>.Some(1), Option<int>.Some(2)];
Option<int>[] some = [Option<int>.Some(1), Option<int>.None];

all.Sequence();   // Some([1, 2])
some.Sequence();  // None

// Result 版本：回傳「第一個」遇到的錯誤，並在該處停止列舉
var validated = inputs.Select(Validate).Sequence();   // Result<List<Valid>, Error>
```

#### `Partition()` — 蒐集所有錯誤

`Sequence()` 會短路，`Partition()` 則走訪整個序列並蒐集全部內容。
表單驗證正是需要這種行為的場景 —— 使用者應該一次看到所有問題：

```csharp
var (users, errors) = dtos.Select(Validate).Partition();

if (errors.Count > 0)
{
    return BadRequest(errors);   // 回報「全部」驗證失敗，而非只有第一個
}

return Ok(users);
```

#### `Values()` — 保留 Some、丟棄 None

```csharp
Option<int>[] options = [Option<int>.Some(1), Option<int>.None, Option<int>.Some(3)];
options.Values();   // [1, 3]
```

> **`Values()` 與 `Sequence()` 的差異**
> `Values()` 忽略 `None`、盡量保留；`Sequence()` 則視任一 `None` 為整體失敗。

### 從同步值開始非同步管線

以 `Task`/`ValueTask` 為 `this` 的多載適用於管線**本來就是**非同步的情況。
當鏈條的**起點**是同步取得的 `Result`/`Option` 時，請使用以值本身為 `this` 的多載：

```csharp
// Before — 為了滿足型別而多配置一個 Task
await Task.FromResult(Validate(input)).BindAsync(v => SaveAsync(v));

// After
await Validate(input).BindAsync(v => SaveAsync(v));
```

這些多載刻意**不**宣告為 `async`。在短路路徑（`Err`/`None`）上，它們直接回傳已完成的 `ValueTask` ——
沒有狀態機，也沒有任何配置：

```csharp
var pending = Result<int, string>.Err("boom")
    .BindAsync(x => new ValueTask<Result<int, string>>(Result<int, string>.Ok(x)));

pending.IsCompletedSuccessfully;   // true — 完全沒有碰到執行緒集區
```

### Unit 型別

當沒有有意義的回傳值時，使用 `Unit` 作為 `Result` 的成功型別：

```csharp
Result<Unit, string> Save(Entity entity)
{
    if (!IsValid(entity)) return "驗證失敗";
    repository.Save(entity);
    return Unit.Value;
}
```

### Option 與 Result 互轉

```csharp
// Option -> Result（None 轉為帶有指定錯誤的 Err）
var result = option.ToResult("找不到值");
var result2 = option.ToResult(() => new AppError("找不到"));

// Result -> Option（捨棄錯誤資訊）
var option = result.ToOption();

// Result -> Option<Error>（捨棄成功值）
var maybeError = result.Err();
```

## 採用現代 .NET 特性

- **零堆積分配** — 所有核心型別（`Option<T>`、`Result<T, E>`、`Unit`）皆為 `readonly struct`，完全存放於堆疊上。
- **AggressiveInlining** — 所有關鍵路徑方法皆由 JIT 內聯，開銷極低。
- **`IEquatable<T>`** 與 **`IComparable<T>`** — `Option` 和 `Unit` 可進行比較、排序，並作為字典鍵使用。
- **可空標註** — 完整支援 C# 可空參考型別分析。
- **LINQ 查詢語法** — `Select`、`SelectMany`、`Where` 支援 `from`/`where`/`select` 語法。
- **模式比對** — `Deconstruct` 支援 `switch` 表達式與 `is` 模式。
- **非同步支援** — 提供 `Task<T>` 和 `ValueTask<T>` 的擴充方法：`BindAsync`、`MapAsync`、`OrElseAsync`、`TapAsync` 等。
- **隱式轉換** — `Result<T, E>` 可直接從 `T`、`E`、`Ok<T>` 或 `Err<E>` 建立，讓方法回傳更簡潔。
- **未初始化狀態即中毒** — `default(Result<T, E>)` **不是**一個合法的結果。由於 C# 允許任何 struct 被零值初始化
  （陣列元素、未指派的欄位），`Result` 會獨立追蹤「未初始化」狀態，任何要讀取內部值的成員都會拋出
  `InvalidOperationException`。此保證對**所有** `E` 都成立，包含預設值本身就是有意義成員的 `enum` 與 `struct` 錯誤型別。
- **支援 AOT 與 Trimming** — 已宣告 `IsAotCompatible` 與 `IsTrimmable`；無反射、無動態程式碼產生。

> **已知限制 — `T` 與 `E` 不可為相同型別**
> `Result<T, E>` 同時宣告了來自 `T` 與 `E` 的隱式轉換。當兩者被具現化為同一型別時
> （例如 `Result<string, string>`），這兩個運算子的簽章會重複，使用隱式轉換會得到編譯錯誤 `CS0457`。
> 請改用明確的工廠方法或包裹記錄：
>
> ```csharp
> Result<string, string> r = ok ? new Ok<string>("value") : new Err<string>("error");
> ```
>
> 更推薦的做法是為錯誤定義專用型別（`enum`、`record` 或 `readonly record struct`）——
> 這不僅避開本限制，也讓錯誤在型別系統中具備明確語意。

## API 參考

### Option\<T\>

| 方法 | 說明 |
|---|---|
| `Some(T)` / `None` | 建構 |
| `IsSome` / `IsNone` | 狀態檢查 |
| `Contains(T)` | 值相等性檢查 |
| `IsSomeAnd(predicate)` | 條件檢查 |
| `Match(some, none)` | 模式比對（有/無回傳值版本） |
| `Map(mapper)` | 轉換內部值 |
| `Filter(predicate)` | 條件過濾 |
| `Bind(binder)` | Monadic 綁定（flatMap） |
| `Zip(other)` / `ZipWith(other, zipper)` | 組合兩個 Option |
| `Or(other)` / `OrElse(factory)` | 備援值 |
| `Tap(action)` | 執行副作用（不改變值） |
| `Unwrap()` / `UnwrapOr(default)` / `UnwrapOrElse(factory)` / `Expect(msg)` | 取出值 |
| `TryGetValue(out value)` | TryParse 風格取值 |
| `ToResult(error)` / `ToResult(factory)` | 轉換為 Result |
| `MapOr(default, mapper)` / `MapOrElse(factory, mapper)` | 帶備援的映射 |
| `Deconstruct(out isSome, out value)` | 解構支援，用於模式比對與 switch 表達式 |
| `Select` / `SelectMany` / `Where` | LINQ 支援 |
| `Equals` / `CompareTo` / 比較運算子 | 相等、排序與比較支援；`None` 會排在 `Some` 前面 |
| `ToString()` | 方便偵錯的 `Some(value)` 或 `None` 文字 |
| `Flatten()` | 展平巢狀 `Option<Option<T>>`（擴充方法） |

### Result\<T, E\>

| 方法 | 說明 |
|---|---|
| `Ok(T)` / `Err(E)` | 建構 |
| 從 `T` / `E` / `Ok<T>` / `Err<E>` 隱式轉換 | 隱式轉換 |
| `IsOk` / `IsErr` | 狀態檢查 |
| `Contains(T)` / `ContainsErr(E)` | 值/錯誤相等性檢查 |
| `IsOkAnd(predicate)` / `IsErrAnd(predicate)` | 條件檢查 |
| `Match(ok, err)` | 模式比對 |
| `Map(mapper)` / `MapErr(mapper)` | 轉換值或錯誤 |
| `Bind(binder)` | Monadic 綁定 |
| `Or(other)` / `OrElse(factory)` | 備援值 |
| `Tap(action)` / `TapErr(action)` | 執行副作用 |
| `Unwrap()` / `UnwrapOr(default)` / `UnwrapOrElse(factory)` / `Expect(msg)` | 取出值 |
| `UnwrapErr()` / `ExpectErr(msg)` | 取出錯誤 |
| `TryGetOk(out value)` / `TryGetOk(out value, out error)` / `TryGetErr(out error)` | TryParse 風格 |
| `ToOption()` / `Err()` | 轉換為 Option |
| `MapOr` / `MapOrElse` | 帶備援的映射 |
| `Deconstruct(out isOk, out value, out error)` | 解構支援，用於模式比對與 switch 表達式 |
| `Select` / `SelectMany` | LINQ 支援 |
| `Equals` / `CompareTo` / 比較運算子 | 相等、排序與比較支援；`Err` 會排在 `Ok` 前面 |
| `ToString()` | 方便偵錯的 `Ok(value)` 或 `Err(error)` 文字 |
| `Flatten()` | 展平巢狀 `Result<Result<T,E>,E>`（擴充方法） |

### 輔助型別

| 型別 | API | 說明 |
|---|---|---|
| `Unit` | `Unit.Value` | 表示成功但沒有有意義回傳值的結果，常用於 `Result<Unit, E>` |
| `Unit` | `Equals` / `CompareTo` / `==` / `!=` / `ToString()` | 所有 Unit 值都相等、比較結果相同，並顯示為 `()` |
| `OptionNone` | `Option.None` 標記 | 萬用 None 標記，可隱式轉換為任何 `Option<T>` |
| `Ok<T>` / `Err<E>` | 包裝記錄 | 當隱式轉換有歧義或需要更明確語意時，用來建立 `Result<T, E>` |

### 擴充方法

| 類別 | 方法 | 說明 |
|---|---|---|
| `OptionExtensions` | `OrNull()` | 將 `Option<T>` 轉換為 `T?`（支援值型別與參考型別） |
| `NullableExtensions` | `ToOption()` | 將 `T?`（參考或值型別）轉換為 `Option<T>` |
| `EnumerableExtensions` | `Values()` | 從 `IEnumerable<Option<T>>` 過濾出所有 `Some` 值 |
| `EnumerableExtensions` | `FirstOrNone()` / `FirstOrNone(predicate)` | 取第一個元素為 `Option<T>`；空序列或無符合者為 `None` |
| `EnumerableExtensions` | `SingleOrNone()` | 取唯一元素為 `Option<T>`；空**或**多於一個皆為 `None`（不拋例外） |
| `EnumerableExtensions` | `ElementAtOrNone(index)` | 取指定索引的元素為 `Option<T>`；超出範圍為 `None` |
| `EnumerableExtensions` | `GetValueOrNone(key)` | 字典查詢為 `Option<TValue>`；鍵不存在為 `None` |
| `EnumerableExtensions` | `Sequence()` | `IEnumerable<Option<T>>` → `Option<List<T>>`、`IEnumerable<Result<T,E>>` → `Result<List<T>,E>`（會短路） |
| `EnumerableExtensions` | `Partition()` | `IEnumerable<Result<T,E>>` → `(List<T> Values, List<E> Errors)`（蒐集**全部**錯誤） |
| `OptionAsyncExtensions` | `BindAsync` / `MapAsync` / `OrElseAsync` | 非同步 Option 串接（`Task` 與 `ValueTask`） |
| `OptionAsyncExtensions` | `Option<T>` 上的 `BindAsync` / `MapAsync` | 以**同步**的 `Option<T>` 為非同步管線起點，不需 `Task.FromResult` 包裝 |
| `ResultAsyncExtensions` | `BindAsync` / `MapAsync` / `MapErrAsync` / `OrElseAsync` / `TapAsync` / `TapErrAsync` | 非同步 Result 串接（`Task` 與 `ValueTask`） |
| `ResultAsyncExtensions` | `Result<T,E>` 上的 `BindAsync` / `MapAsync` | 以**同步**的 `Result<T,E>` 為非同步管線起點，短路時零配置 |

## 安全性注意事項

`Unwrap()` 與 `UnwrapErr()` 在失敗時擲出的 `InvalidOperationException`，其預設訊息會內嵌「你沒有要求的那一側」的
`ToString()` 結果（`Unwrap()` 在 `Err` 時會包含錯誤內容；`UnwrapErr()` 在 `Ok` 時會包含成功值）：

```csharp
var result = Result<int, string>.Err(secretValidationDetails);
result.Unwrap(); // 訊息內容："Result is Err: {secretValidationDetails}"
```

若 `T` 或 `E` 可能承載敏感資訊（原始使用者輸入、Token、連線字串、個資等），這些內容可能經由未攔截的例外
流入記錄檔、遙測系統或錯誤回應。`ToString()` 也有同樣的行為——它一律會輸出 `Ok(value)` / `Err(error)` 以利除錯。

**建議做法：**
- 當值／錯誤可能含敏感資訊時，優先使用 `Expect(message)` / `ExpectErr(message)`，並提供固定、不含敏感內容的訊息——
  訊息內容完全由呼叫端掌控。
- 若只是要依成功/失敗分流處理，優先使用 `Match` / `TryGetOk` / `TryGetErr`，完全避開預設例外路徑。
- 避免將機密資料直接放進 `E`（或 `T`）；改用你能掌控其 `ToString()` 的錯誤/DTO 型別包裝，
  或在資料進入 `Result`/`Option` 之前先做遮罩處理。

## 專案結構

```
Rayleigh/
├── src/
│   └── jIAnSoft.Rayleigh/     # 核心函式庫
├── tests/
│   └── jIAnSoft.Rayleigh.Tests/ # 單元測試（xUnit）
├── examples/
│   └── jIAnSoft.Rayleigh.Examples/ # 可執行教學（E01–E12，含新手導向的詳細註解）
├── benchmarks/
│   └── jIAnSoft.Rayleigh.Benchmarks/ # BenchmarkDotNet 記憶體配置／效能基準測試
├── CHANGELOG.md                      # 版本變更紀錄
├── LICENSE
└── README.md
```

## 動手學：可執行的教學範例

`examples/` 不是零散的程式碼片段，而是一份**可以直接跑起來的教學**。
十二個模組都會先解釋這個 API「為什麼存在」，再示範怎麼用，
並把每一行程式碼和它的實際執行結果並排印出來：

```bash
# 從頭到尾跑一遍十二個模組
dotnet run --project examples/jIAnSoft.Rayleigh.Examples

# 或只跑其中一個——例如第 8 個模組（集合操作）
dotnet run --project examples/jIAnSoft.Rayleigh.Examples -- 8
```

| # | 模組 | 內容 |
|---|---|---|
| E01 | Option 入門 | 什麼是 Option、四種建立方式、`IsSome` / `Contains` |
| E02 | Option 轉換 | `Map` / `Filter` / `Bind` / `Flatten`，以及該怎麼選 |
| E03 | Option 取值 | `Match`、`TryGetValue`、`Unwrap` 家族、`Or`、`Tap`、`Zip` |
| E04 | Result 入門 | 帶著失敗原因、錯誤型別怎麼選、未初始化的陷阱 |
| E05 | Result 轉換 | `Map` / `MapErr` / `Bind` 與鐵路導向程式設計 |
| E06 | Result 取值 | `Match`、`TryGetOk`、備援、日誌、`Unit` 型別 |
| E07 | Option 與 Result 互轉 | 什麼時候用哪一個，以及怎麼轉換 |
| E08 | 集合操作 | `FirstOrNone`、`GetValueOrNone`、`Sequence`、`Partition`、`Values` |
| E09 | 非同步管線 | `BindAsync` / `MapAsync`、零配置短路、`CancellationToken` |
| E10 | LINQ 查詢語法 | 用 `from` / `where` / `select` 操作 Option 和 Result |
| E11 | 實戰場景 | 設定檔、表單驗證、註冊流程、批次匯入、多層快取 |
| E12 | 常見陷阱 | 十個值得看一次的錯誤寫法，每個都附正確版本 |

> 這些模組是寫給第一次接觸 Option / Result 的人看的。
> 如果你已經熟悉這些概念，E08 和 E12 應該是比較有新東西的兩篇。

## 建置

```bash
dotnet build
```

## 測試

```bash
dotnet test
```

## 效能基準測試

`benchmarks/` 專案使用 [BenchmarkDotNet](https://benchmarkdotnet.org/) 量測 `Option<T>` 與 `Result<T, E>` 的
記憶體配置與效能，用以佐證上方「zero-allocation」的宣稱，並防止未來重構造成效能迴歸。
請以 Release 模式執行（BenchmarkDotNet 不允許在 Debug 組態下執行）：

```bash
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks
```

只跑單一組，或以較少迭代快速驗證：

```bash
# 單一組
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks -- --filter '*EnumerableBenchmarks*'

# 快速模式（精度較低，但快得多）
dotnet run -c Release --project benchmarks/jIAnSoft.Rayleigh.Benchmarks -- --filter '*' --job short
```

| 測試組 | 用以佐證的事項 |
|---|---|
| `OptionBenchmarks` | `Option<T>` 本身零配置；對照組呈現 closure 成本屬於**呼叫端**，而非函式庫 |
| `ResultBenchmarks` | `Result<T,E>` 同上；未初始化防護在成功路徑上無可觀測成本 |
| `EnumerableBenchmarks` | 集合組合子提供更強的語意，但沒有以效能為代價（與 LINQ 對應物比較） |
| `AsyncPipelineBenchmarks` | sync-source 多載短路時配置 **0 B**，對照它所取代的 `Task.FromResult` 包裝 |
| `ThrowPathBenchmarks` | 量化「為何錯誤路徑該用 `Result` 而非例外驅動流程控制」 |

代表性數據（BenchmarkDotNet 0.15.8／AMD Ryzen 9 5950X／.NET 10.0.9）：

| | Mean | Allocated |
|---|---:|---:|
| `Result.Ok(v)` | 0.13 ns | 0 B |
| `result.UnwrapOr(-1)`（`Err` 錯誤路徑） | 0.16 ns | 0 B |
| `try { result.Unwrap(); } catch { }`（同一情境，改用例外） | 2,100 ns | 384 B |
| `err.BindAsync(...)`（sync-source 多載，短路） | 13.9 ns | **0 B** |
| `Task.FromResult(err).BindAsync(...)`（它所取代的包裝寫法） | 38.4 ns | 240 B |

> **關於精度：** 上表以 `--job short`（3 次迭代）取得，`Mean` 欄的誤差區間偏大，僅供方向性參考；
> `Allocated` 欄則由 GC 計數器確定性量測，數值精確。若需可引用的精確耗時，請移除 `--job short` 重跑。

## 授權條款

本專案採用 [MIT 授權條款](LICENSE)。
