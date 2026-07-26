namespace jIAnSoft.Rayleigh;

/// <summary>
/// 提供 <see cref="IEnumerable{T}"/> 與 <see cref="Option{T}"/>／<see cref="Result{T,TE}"/> 之間的擴充方法。
/// </summary>
/// <remarks>
/// <para><b>參數驗證的時機</b></para>
/// <para>
/// 本類別中所有回傳 <see cref="IEnumerable{T}"/> 的方法都刻意拆成「非 iterator 的外殼方法」與
/// 「私有的 local iterator 函式」兩層。原因是含 <c>yield return</c> 的方法在 C# 中屬於延遲執行：
/// 方法本體要到第一次 <c>MoveNext()</c> 才會執行，若把 null 檢查寫在 iterator 內，
/// 呼叫端會在遠離錯誤來源的 <c>foreach</c> 現場才收到例外，且型別會是難以理解的
/// <see cref="NullReferenceException"/> 而非 <see cref="ArgumentNullException"/>。
/// 這也是 BCL 中 LINQ 運算子一致採用的結構。
/// </para>
/// </remarks>
public static class EnumerableExtensions
{
    /// <summary>
    /// 從 <see cref="Option{T}"/> 集合中過濾出所有的 Some 值。
    /// </summary>
    /// <typeparam name="T">值的類型。</typeparam>
    /// <param name="source">Option 集合。</param>
    /// <returns>包含所有 Some 值的集合（延遲執行）。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時<b>立即</b>擲出。</exception>
    /// <example>
    /// <code>
    /// var options = new[] { Option&lt;int&gt;.Some(1), Option&lt;int&gt;.None, Option&lt;int&gt;.Some(3) };
    /// var values = options.Values();  // [1, 3]
    /// </code>
    /// </example>
    public static IEnumerable<T> Values<T>(this IEnumerable<Option<T>> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return Iterate(source);

        static IEnumerable<T> Iterate(IEnumerable<Option<T>> source)
        {
            // 針對已知的具體集合型別做型別測試，讓 foreach 綁定到該型別自身的列舉方式，
            // 而非 IEnumerable<T> 介面：陣列會編譯成索引迴圈，List<T> 則使用其 struct enumerator，
            // 兩者都能避免介面派送與 enumerator 裝箱。
            //
            // 註：此處刻意不使用 Span<T>（例如 CollectionsMarshal.AsSpan）——
            // ref struct 無法跨越 yield return 邊界（CS4007），迭代器方法中不可行。
            switch (source)
            {
                case Option<T>[] array:
                    foreach (var option in array)
                    {
                        if (option.TryGetValue(out var value))
                        {
                            yield return value;
                        }
                    }

                    break;

                case List<Option<T>> list:
                    foreach (var option in list)
                    {
                        if (option.TryGetValue(out var value))
                        {
                            yield return value;
                        }
                    }

                    break;

                default:
                    foreach (var option in source)
                    {
                        if (option.TryGetValue(out var value))
                        {
                            yield return value;
                        }
                    }

                    break;
            }
        }
    }

    #region Entry Points（從命令式集合進入 Option 世界）

    /// <summary>
    /// 取得序列的第一個元素；若序列為空則回傳 <see cref="Option{T}.None"/>。
    /// </summary>
    /// <typeparam name="T">元素類型。</typeparam>
    /// <param name="source">來源序列。</param>
    /// <returns>第一個元素的 <see cref="Option{T}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// <para><b>為什麼不要用 <c>FirstOrDefault()</c></b></para>
    /// <para>
    /// <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/> 對值型別會回傳
    /// <c>default(T)</c>，使「找不到元素」與「找到了一個恰好等於預設值的元素」無法區分——
    /// 例如在 <c>int</c> 序列中無法分辨「空序列」與「第一個元素是 0」。本方法沒有這個問題。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var scores = new[] { 0, 5, 10 };
    ///
    /// scores.FirstOrNone();                 // Some(0)  ← FirstOrDefault 會回傳 0，與「空序列」無法區分
    /// Array.Empty&lt;int&gt;().FirstOrNone();     // None
    /// scores.FirstOrNone(x =&gt; x &gt; 100);     // None
    /// </code>
    /// </example>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        // 對 IList<T> 走索引存取，完全不建立 enumerator。
        if (source is IList<T> list)
        {
            return list.Count > 0 ? Option<T>.Some(list[0]) : Option<T>.None;
        }

        foreach (var item in source)
        {
            return Option<T>.Some(item);
        }

        return Option<T>.None;
    }

    /// <summary>
    /// 取得序列中第一個符合條件的元素；若沒有符合的元素則回傳 <see cref="Option{T}.None"/>。
    /// </summary>
    /// <typeparam name="T">元素類型。</typeparam>
    /// <param name="source">來源序列。</param>
    /// <param name="predicate">篩選條件。</param>
    /// <returns>第一個符合條件之元素的 <see cref="Option{T}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 或 <paramref name="predicate"/> 為 <c>null</c> 時擲出。</exception>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
            {
                return Option<T>.Some(item);
            }
        }

        return Option<T>.None;
    }

    /// <summary>
    /// 取得序列中唯一的元素；若序列為空<b>或</b>包含多於一個元素，則回傳 <see cref="Option{T}.None"/>。
    /// </summary>
    /// <typeparam name="T">元素類型。</typeparam>
    /// <param name="source">來源序列。</param>
    /// <returns>唯一元素的 <see cref="Option{T}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// 與 <see cref="Enumerable.Single{TSource}(IEnumerable{TSource})"/> 不同，元素多於一個時<b>不會</b>拋出例外，
    /// 而是視為「沒有唯一解」回傳 <see cref="Option{T}.None"/>。若你需要區分「空」與「過多」兩種失敗原因，
    /// 請改用回傳 <see cref="Result{T,TE}"/> 的自訂邏輯。
    /// </remarks>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IList<T> list)
        {
            return list.Count == 1 ? Option<T>.Some(list[0]) : Option<T>.None;
        }

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return Option<T>.None;
        }

        var first = enumerator.Current;
        return enumerator.MoveNext() ? Option<T>.None : Option<T>.Some(first);
    }

    /// <summary>
    /// 取得序列中指定索引的元素；若索引超出範圍則回傳 <see cref="Option{T}.None"/>。
    /// </summary>
    /// <typeparam name="T">元素類型。</typeparam>
    /// <param name="source">來源序列。</param>
    /// <param name="index">以 0 起算的索引。負數一律視為超出範圍。</param>
    /// <returns>該位置元素的 <see cref="Option{T}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    public static Option<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        if (index < 0)
        {
            return Option<T>.None;
        }

        if (source is IList<T> list)
        {
            return index < list.Count ? Option<T>.Some(list[index]) : Option<T>.None;
        }

        var remaining = index;
        foreach (var item in source)
        {
            if (remaining == 0)
            {
                return Option<T>.Some(item);
            }

            remaining--;
        }

        return Option<T>.None;
    }

    /// <summary>
    /// 從字典取值；若鍵不存在則回傳 <see cref="Option{TValue}.None"/>。
    /// </summary>
    /// <typeparam name="TKey">鍵的類型。</typeparam>
    /// <typeparam name="TValue">值的類型。</typeparam>
    /// <param name="dictionary">來源字典。</param>
    /// <param name="key">要查詢的鍵。</param>
    /// <returns>對應值的 <see cref="Option{TValue}"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="dictionary"/> 或 <paramref name="key"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// 相較於 <c>dictionary.GetValueOrDefault(key).ToOption()</c>，本方法對值型別同樣正確：
    /// 前者無法區分「鍵不存在」與「鍵存在但值為 <c>default(TValue)</c>」。
    /// </remarks>
    /// <example>
    /// <code>
    /// var counts = new Dictionary&lt;string, int&gt; { ["a"] = 0 };
    ///
    /// counts.GetValueOrNone("a");  // Some(0)   ← GetValueOrDefault 回傳 0，與「不存在」無法區分
    /// counts.GetValueOrNone("z");  // None
    /// </code>
    /// </example>
    public static Option<TValue> GetValueOrNone<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey key)
        where TKey : notnull
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(key);

        return dictionary.TryGetValue(key, out var value) ? Option<TValue>.Some(value) : Option<TValue>.None;
    }

    #endregion

    #region Sequence / Traverse

    /// <summary>
    /// 將 <c>IEnumerable&lt;Option&lt;T&gt;&gt;</c> 反轉為 <c>Option&lt;List&lt;T&gt;&gt;</c>：
    /// 全部為 Some 才回傳 Some，任一為 None 即整體為 None。
    /// </summary>
    /// <typeparam name="T">值的類型。</typeparam>
    /// <param name="source">Option 集合。</param>
    /// <returns>若全部有值則為 <c>Some(所有值)</c>；否則為 <see cref="Option{T}.None"/>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// <para><b>短路行為</b></para>
    /// <para>遇到第一個 None 就立即停止列舉，不會走訪剩餘元素。</para>
    /// <para><b>與 <see cref="Values{T}"/> 的差異</b></para>
    /// <para><see cref="Values{T}"/> 是「忽略 None、保留 Some」；本方法是「任一 None 就整體失敗」。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var allSome = new[] { Option&lt;int&gt;.Some(1), Option&lt;int&gt;.Some(2) };
    /// allSome.Sequence();  // Some([1, 2])
    ///
    /// var hasNone = new[] { Option&lt;int&gt;.Some(1), Option&lt;int&gt;.None };
    /// hasNone.Sequence();  // None
    /// </code>
    /// </example>
    public static Option<List<T>> Sequence<T>(this IEnumerable<Option<T>> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        // 先取得長度以預先配置容量，避免成功路徑上 List 反覆擴充（每次擴充都是一次配置 + 複製）。
        // 取捨：短路路徑（提前遇到 None/Err）會配置用不到的容量。這是刻意的選擇——
        // 短路時該 List 本來就會被整個丟棄，多配置的成本一次付清；而成功路徑才是需要最佳化的常見情況。
        var values = source.TryGetNonEnumeratedCount(out var count) ? new List<T>(count) : [];

        foreach (var option in source)
        {
            if (!option.TryGetValue(out var value))
            {
                return Option<List<T>>.None;
            }

            values.Add(value);
        }

        return Option<List<T>>.Some(values);
    }

    /// <summary>
    /// 將 <c>IEnumerable&lt;Result&lt;T, TE&gt;&gt;</c> 反轉為 <c>Result&lt;List&lt;T&gt;, TE&gt;</c>：
    /// 全部為 Ok 才回傳 Ok，遇到第一個 Err 即回傳該錯誤。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="source">Result 集合。</param>
    /// <returns>若全部成功則為 <c>Ok(所有值)</c>；否則為第一個遇到的 <c>Err</c>。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// <para><b>短路行為</b></para>
    /// <para>
    /// 遇到第一個 Err 就立即停止列舉並回傳該錯誤，不會走訪剩餘元素。
    /// 若你需要蒐集<b>所有</b>錯誤（例如表單驗證要一次回報全部問題），請改用 <see cref="Partition{T,TE}"/>。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 驗證多個欄位，任一失敗即中止
    /// var results = inputs.Select(Validate);
    /// var all = results.Sequence();  // Result&lt;List&lt;Valid&gt;, Error&gt;
    /// </code>
    /// </example>
    public static Result<List<T>, TE> Sequence<T, TE>(this IEnumerable<Result<T, TE>> source)
        where T : notnull
        where TE : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        // 先取得長度以預先配置容量，避免成功路徑上 List 反覆擴充（每次擴充都是一次配置 + 複製）。
        // 取捨：短路路徑（提前遇到 None/Err）會配置用不到的容量。這是刻意的選擇——
        // 短路時該 List 本來就會被整個丟棄，多配置的成本一次付清；而成功路徑才是需要最佳化的常見情況。
        var values = source.TryGetNonEnumeratedCount(out var count) ? new List<T>(count) : [];

        foreach (var result in source)
        {
            if (!result.TryGetOk(out var value, out var error))
            {
                return Result<List<T>, TE>.Err(error);
            }

            values.Add(value);
        }

        return Result<List<T>, TE>.Ok(values);
    }

    /// <summary>
    /// 將 <c>IEnumerable&lt;Result&lt;T, TE&gt;&gt;</c> 拆分為「所有成功值」與「所有錯誤」兩份清單。
    /// </summary>
    /// <typeparam name="T">成功值的類型。</typeparam>
    /// <typeparam name="TE">錯誤類型。</typeparam>
    /// <param name="source">Result 集合。</param>
    /// <returns>包含成功值清單與錯誤清單的元組。</returns>
    /// <exception cref="ArgumentNullException">當 <paramref name="source"/> 為 <c>null</c> 時擲出。</exception>
    /// <remarks>
    /// <para><b>與 <see cref="Sequence{T,TE}"/> 的差異</b></para>
    /// <para>
    /// <see cref="Sequence{T,TE}"/> 在第一個錯誤就短路；本方法會走訪<b>全部</b>元素並蒐集所有錯誤，
    /// 適合「一次回報所有驗證問題」的場景。代價是必然走訪整個序列，且會配置兩份清單。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (users, errors) = dtos.Select(Validate).Partition();
    ///
    /// if (errors.Count > 0)
    /// {
    ///     return BadRequest(errors);  // 一次回報所有驗證錯誤
    /// }
    /// </code>
    /// </example>
    public static (List<T> Values, List<TE> Errors) Partition<T, TE>(this IEnumerable<Result<T, TE>> source)
        where T : notnull
        where TE : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        List<T> values = [];
        List<TE> errors = [];

        foreach (var result in source)
        {
            if (result.TryGetOk(out var value, out var error))
            {
                values.Add(value);
            }
            else
            {
                errors.Add(error);
            }
        }

        return (values, errors);
    }

    #endregion
}
