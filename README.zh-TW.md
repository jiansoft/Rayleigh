[English](README.md) | **繁體中文**

# Rayleigh

受 Rust 啟發的 C# Option 與 Result 型別。

專為 .NET 8 打造的零分配、高效能函數式基礎型別庫。

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

## 使用方式

### 建立 Option

```csharp
using jIAnSoft.Rayleigh;

// 從值建立
var some = Option<int>.Some(42);
var none = Option<int>.None;

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
| `Select` / `SelectMany` / `Where` | LINQ 支援 |
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
| `Select` / `SelectMany` | LINQ 支援 |
| `Flatten()` | 展平巢狀 `Result<Result<T,E>,E>`（擴充方法） |

### 擴充方法

| 類別 | 方法 | 說明 |
|---|---|---|
| `NullableExtensions` | `ToOption()` | 將 `T?`（參考或值型別）轉換為 `Option<T>` |
| `EnumerableExtensions` | `Values()` | 從 `IEnumerable<Option<T>>` 過濾出所有 `Some` 值 |
| `OptionAsyncExtensions` | `BindAsync` / `MapAsync` / `OrElseAsync` | 非同步 Option 串接（`Task` 與 `ValueTask`） |
| `ResultAsyncExtensions` | `BindAsync` / `MapAsync` / `MapErrAsync` / `OrElseAsync` / `TapAsync` / `TapErrAsync` | 非同步 Result 串接（`Task` 與 `ValueTask`） |

## 專案結構

```
Rayleigh/
├── src/
│   └── Rayleigh/
│       ├── Option.cs          # Option<T> 型別
│       ├── Result.cs          # Result<T, E>、Ok<T>、Err<E> 型別
│       ├── Unit.cs            # Unit 型別
│       └── Extensions.cs      # 所有擴充方法
├── tests/
│   └── Rayleigh.Tests/        # 單元測試（xUnit）
├── examples/
│   └── Rayleigh.Examples/     # 可執行範例與常見誤用展示
├── LICENSE
└── README.md
```

## 建置

```bash
dotnet build
```

## 測試

```bash
dotnet test
```

## 授權條款

本專案採用 [MIT 授權條款](LICENSE)。
