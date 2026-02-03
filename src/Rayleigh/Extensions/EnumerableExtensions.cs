namespace jIAnSoft.Rayleigh.Extensions;

/// <summary>
/// 提供 <see cref="IEnumerable{T}"/> 的擴充方法。
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// 從 <see cref="Option{T}"/> 集合中過濾出所有的 Some 值。
    /// </summary>
    /// <typeparam name="T">值的類型。</typeparam>
    /// <param name="source">Option 集合。</param>
    /// <returns>包含所有 Some 值的集合。</returns>
    /// <example>
    /// <code>
    /// var options = new[] { Option&lt;int&gt;.Some(1), Option&lt;int&gt;.None, Option&lt;int&gt;.Some(3) };
    /// var values = options.Values();  // [1, 3]
    /// </code>
    /// </example>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source) where T : notnull
    {
        foreach (var option in source)
        {
            if (option.TryGetValue(out var value))
            {
                yield return value;
            }
        }
    }
}
