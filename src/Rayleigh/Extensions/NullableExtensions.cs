namespace jIAnSoft.Rayleigh.Extensions;

/// <summary>
/// 提供將可空類型轉換為 <see cref="Option{T}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法讓您可以輕鬆地將現有的可空參考類型或 <see cref="Nullable{T}"/>
/// 轉換為 <see cref="Option{T}"/>，以便使用函數式風格處理可能不存在的值。
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 從可空參考類型轉換
/// string? name = GetName();
/// var option = name.ToOption();  // Some("value") 或 None
///
/// // 從 Nullable&lt;T&gt; 轉換
/// int? age = GetAge();
/// var ageOption = age.ToOption();  // Some(25) 或 None
///
/// // 結合 LINQ 風格操作
/// var greeting = GetUser()
///     .ToOption()
///     .Map(user => user.Name)
///     .Map(name => $"Hello, {name}!")
///     .UnwrapOr("Hello, Guest!");
/// </code>
/// </example>
public static class NullableExtensions
{
    /// <summary>
    /// 將可空參考類型轉換為 <see cref="Option{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為參考類型。</typeparam>
    /// <param name="value">可能為 <c>null</c> 的值。</param>
    /// <returns>
    /// 若 <paramref name="value"/> 不為 <c>null</c>，則回傳 <see cref="Option{T}.Some"/>；
    /// 否則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <example>
    /// <code>
    /// string? name = "Alice";
    /// var some = name.ToOption();  // Some("Alice")
    ///
    /// string? nullName = null;
    /// var none = nullName.ToOption();  // None
    ///
    /// // 實際應用：從字典取值
    /// var dict = new Dictionary&lt;string, User&gt;();
    /// var user = dict.GetValueOrDefault("key").ToOption();
    /// </code>
    /// </example>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is not null ? Option<T>.Some(value) : Option<T>.None;

    /// <summary>
    /// 將 <see cref="Nullable{T}"/> 轉換為 <see cref="Option{T}"/>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為值類型。</typeparam>
    /// <param name="value">可能為 <c>null</c> 的 <see cref="Nullable{T}"/>。</param>
    /// <returns>
    /// 若 <paramref name="value"/> 有值，則回傳 <see cref="Option{T}.Some"/>；
    /// 否則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <example>
    /// <code>
    /// int? age = 25;
    /// var some = age.ToOption();  // Some(25)
    ///
    /// int? nullAge = null;
    /// var none = nullAge.ToOption();  // None
    ///
    /// // 實際應用：解析可能失敗的操作
    /// int? parsed = int.TryParse(input, out var result) ? result : null;
    /// var option = parsed.ToOption();
    /// </code>
    /// </example>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? Option<T>.Some(value.Value) : Option<T>.None;
}
