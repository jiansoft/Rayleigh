using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh.Extensions;

/// <summary>
/// 提供 <see cref="Result{T,E}"/> 的擴充方法。
/// </summary>
/// <remarks>
/// <para>
/// 這些擴充方法提供額外的功能，特別是那些需要特定泛型約束的操作，
/// 例如 <see cref="Flatten{T, TE}(Result{Result{T, TE}, TE})"/> 需要巢狀的 <see cref="Result{T,E}"/>。
/// </para>
/// </remarks>
public static class ResultExtensions
{
    /// <summary>
    /// 展平巢狀的 <see cref="Result{T,E}"/>，將 <c>Result&lt;Result&lt;T, E&gt;, E&gt;</c> 轉換為 <c>Result&lt;T, E&gt;</c>。
    /// </summary>
    /// <typeparam name="T">內部 Result 的成功值類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="nested">巢狀的 Result。</param>
    /// <returns>
    /// 若外層和內層都是 Ok，則回傳內層的成功值；
    /// 若任一層為 Err，則回傳該層的錯誤。
    /// </returns>
    /// <remarks>
    /// <para>
    /// 此方法對應 Rust 的 <c>flatten()</c> 方法，用於移除一層 Result 包裝。
    /// 這在處理回傳 <c>Result&lt;Result&lt;T, E&gt;, E&gt;</c> 的操作時特別有用。
    /// </para>
    /// <para>
    /// <c>Flatten</c> 等同於 <c>Bind(x => x)</c>，但語意更明確。
    /// </para>
    /// <para><b>錯誤處理</b></para>
    /// <para>
    /// 當外層為 Err 時，回傳外層的錯誤；
    /// 當外層為 Ok 但內層為 Err 時，回傳內層的錯誤。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 巢狀 Result 的情況
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Ok(Result&lt;int, string&gt;.Ok(42));
    /// var flattened = nested.Flatten();
    /// // flattened: Ok(42)
    ///
    /// // 外層為 Ok，內層為 Err
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested2 =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Ok(Result&lt;int, string&gt;.Err("inner error"));
    /// var flattened2 = nested2.Flatten();
    /// // flattened2: Err("inner error")
    ///
    /// // 外層為 Err
    /// Result&lt;Result&lt;int, string&gt;, string&gt; nested3 =
    ///     Result&lt;Result&lt;int, string&gt;, string&gt;.Err("outer error");
    /// var flattened3 = nested3.Flatten();
    /// // flattened3: Err("outer error")
    ///
    /// // 實際應用：Map 後可能產生巢狀 Result
    /// Result&lt;User, Error&gt; ValidateUser(UserDto dto) => ...;
    /// var dto = Result&lt;UserDto, Error&gt;.Ok(new UserDto(...));
    /// var user = dto.Map(ValidateUser).Flatten();  // 等同於 dto.Bind(ValidateUser)
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, TE> Flatten<T, TE>(this Result<Result<T, TE>, TE> nested)
        where T : notnull
        where TE : notnull
        => nested.Bind(inner => inner);
}
