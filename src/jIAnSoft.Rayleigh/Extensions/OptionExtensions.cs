using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh;

/// <summary>
/// 提供 <see cref="Option{T}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法提供額外的功能，特別是那些需要特定泛型約束的操作，
/// 例如 <see cref="Flatten{T}"/> 需要巢狀的 <see cref="Option{T}"/>。
/// </para>
/// </remarks>
public static class OptionExtensions
{
    /// <summary>
    /// 展平巢狀的 <see cref="Option{T}"/>，將 <c>Option&lt;Option&lt;T&gt;&gt;</c> 轉換為 <c>Option&lt;T&gt;</c>。
    /// </summary>
    /// <typeparam name="T">內部 Option 的值類型。</typeparam>
    /// <param name="nested">巢狀的 Option。</param>
    /// <returns>
    /// 若外層和內層都是 Some，則回傳內層的值；
    /// 若任一層為 None，則回傳 <see cref="Option{T}.None"/>。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法對應 Rust 的 <c>flatten()</c> 方法，用於移除一層 Option 包裝。
    /// 這在處理回傳 <c>Option&lt;Option&lt;T&gt;&gt;</c> 的操作時特別有用。
    /// </para>
    /// <para>
    /// <c>Flatten</c> 等同於 <c>Bind(x => x)</c>，但語意更明確。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 巢狀 Option 的情況
    /// Option&lt;Option&lt;int&gt;&gt; nested = Option&lt;Option&lt;int&gt;&gt;.Some(Option&lt;int&gt;.Some(42));
    /// var flattened = nested.Flatten();
    /// // flattened: Some(42)
    ///
    /// // 外層為 Some，內層為 None
    /// Option&lt;Option&lt;int&gt;&gt; nested2 = Option&lt;Option&lt;int&gt;&gt;.Some(Option&lt;int&gt;.None);
    /// var flattened2 = nested2.Flatten();
    /// // flattened2: None
    ///
    /// // 外層為 None
    /// Option&lt;Option&lt;int&gt;&gt; nested3 = Option&lt;Option&lt;int&gt;&gt;.None;
    /// var flattened3 = nested3.Flatten();
    /// // flattened3: None
    ///
    /// // 實際應用：Map 後可能產生巢狀 Option
    /// Option&lt;string&gt; TryParse(string input) => ...;
    /// var input = Option&lt;string&gt;.Some("42");
    /// var parsed = input.Map(TryParse).Flatten();  // 等同於 input.Bind(TryParse)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Flatten<T>(this in Option<Option<T>> nested) where T : notnull => nested.Bind(inner => inner);

    /// <summary>
    /// 將 <see cref="Option{T}"/> 轉換為可空的值類型，若無值則傳回 <c>null</c>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為值類型。</typeparam>
    /// <param name="option">要轉換的 Option。</param>
    /// <returns>若有值則傳回該值；若無值則傳回 <c>null</c>。</returns>
    /// <example>
    /// <code>
    /// Option&lt;int&gt; some = 42;
    /// int? nullableInt = some.OrNull(); // 42
    ///
    /// Option&lt;int&gt; none = Option&lt;int&gt;.None;
    /// int? nullInt = none.OrNull(); // null
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? OrNull<T>(this in Option<T> option) where T : struct =>
        option.TryGetValue(out var value) ? value : null;
}

/// <summary>
/// 提供 <see cref="Option{T}"/> 針對參考類型的擴充方法。
/// </summary>
public static class OptionClassExtensions
{
    /// <summary>
    /// 將 <see cref="Option{T}"/> 轉換為可空的參考類型，若無值則傳回 <c>null</c>。
    /// </summary>
    /// <typeparam name="T">值的類型，必須為參考類型。</typeparam>
    /// <param name="option">要轉換的 Option。</param>
    /// <returns>若有值則傳回該值；若無值則傳回 <c>null</c>。</returns>
    /// <example>
    /// <code>
    /// Option&lt;string&gt; some = "Hello";
    /// string? nullableStr = some.OrNull(); // "Hello"
    ///
    /// Option&lt;string&gt; none = Option&lt;string&gt;.None;
    /// string? nullStr = none.OrNull(); // null
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? OrNull<T>(this in Option<T> option) where T : class =>
        option.TryGetValue(out var value) ? value : null;
}
