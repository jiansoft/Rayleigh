using System.Runtime.InteropServices;

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
/// <para><b>集合快速路徑</b></para>
/// <para>
/// <see cref="Sequence{T}(IEnumerable{Option{T}})"/>、<see cref="Sequence{T,TE}(IEnumerable{Result{T,TE}})"/>
/// 與 <see cref="Partition{T,TE}"/> 會先對來源做型別測試，若為陣列或 <see cref="List{T}"/> 就改走
/// <see cref="ReadOnlySpan{T}"/>，避開 enumerator 配置與介面派送，並以 <c>ref readonly</c> 走訪避免每輪複製 struct。
/// </para>
/// <para>
/// 這是安全的，因為這些方法的走訪過程<b>不會回呼任何使用者程式碼</b>
/// （只讀取 <see cref="Option{T}"/>／<see cref="Result{T,TE}"/> 的狀態欄位），
/// 集合不可能在走訪期間被修改，因此不需要 <see cref="List{T}"/> enumerator 的版本檢查。
/// 接受委派的方法（例如 <see cref="FirstOrNone{T}(IEnumerable{T}, Func{T, bool})"/>）不適用此結論，
/// 因此刻意不加快速路徑，詳見該方法的備註。
/// </para>
/// <para><b>已量測的取捨</b></para>
/// <para>
/// 收益在 net8.0 上全面且顯著（1024 筆時耗時降低約 53%～68%）；在 net10.0 上仍有明顯效益
/// （約 30%～49%），但幅度較小——因為 .NET 10 的 JIT 已能自行對陣列的列舉做去虛擬化與物件堆疊配置，
/// 手寫快速路徑等於部分重複了 Runtime 已經做到的事。
/// </para>
/// <para>
/// 代價是 net10.0 上<b>極小</b>的輸入（實測 8 筆陣列的 <c>Sequence&lt;Result&gt;</c>）會慢約 4 ns，
/// 因為多付了一次型別測試，而該規模下 JIT 的自動最佳化本來就已接近最佳。
/// 這個取捨是刻意接受的：4 ns 相對於該操作必然發生的 <see cref="List{T}"/> 配置（88 bytes）微不足道，
/// 且交叉點很快就會越過；而 net8.0 在同樣的 8 筆輸入上反而快了約 50%——
/// 需要被最佳化的正是較慢的那個 Runtime。
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
        // 補上 IReadOnlyList<T>：兩者沒有繼承關係，只實作後者的型別（例如
        // ReadOnlyCollection<T> 之外的自訂唯讀集合）不會被前者的型別測試命中。
        switch (source)
        {
            case IList<T> list:
                return list.Count > 0 ? Option<T>.Some(list[0]) : Option<T>.None;

            case IReadOnlyList<T> readOnlyList:
                return readOnlyList.Count > 0 ? Option<T>.Some(readOnlyList[0]) : Option<T>.None;
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
    /// <remarks>
    /// <para><b>為什麼這個多載沒有集合快速路徑</b></para>
    /// <para>
    /// 與無參數的 <see cref="FirstOrNone{T}(IEnumerable{T})"/> 不同，本多載刻意只走泛用列舉，原因有三：
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///   <b>收益微乎其微</b>：本方法會在第一個符合條件的元素短路，實測即使來源有 1024 個元素，
    ///   整體耗時仍在個位數到十餘奈秒之間，enumerator 的成本佔比極低——與無參數版「完全不建立 enumerator」
    ///   的情況不同，後者是常數時間操作，enumerator 反而是主要成本。
    ///   </description></item>
    ///   <item><description>
    ///   <b>不能使用 span</b>：<paramref name="predicate"/> 是使用者程式碼，可以在走訪期間修改來源集合，
    ///   持有 <see cref="ReadOnlySpan{T}"/> 會有記憶體安全疑慮。
    ///   </description></item>
    ///   <item><description>
    ///   <b>索引迴圈無法在本專案穩定存在</b>：改寫成 <c>for (var i = 0; i &lt; list.Count; i++)</c> 後，
    ///   <c>dotnet format</c>（CI 的必過關卡）會依樣式規則將其還原為 <c>foreach</c>，
    ///   使該分支退化成與泛用路徑相同的介面列舉。留下一段實際上沒有加速效果的「快速路徑」，
    ///   比沒有更糟——它會誤導後續維護者。
    ///   </description></item>
    /// </list>
    /// <para>
    /// <see cref="Sequence{T}(IEnumerable{Option{T}})"/> 等不接受委派的方法沒有這些限制，因此保留了 span 快速路徑。
    /// </para>
    /// </remarks>
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

        switch (source)
        {
            case IList<T> list:
                return list.Count == 1 ? Option<T>.Some(list[0]) : Option<T>.None;

            case IReadOnlyList<T> readOnlyList:
                return readOnlyList.Count == 1 ? Option<T>.Some(readOnlyList[0]) : Option<T>.None;
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

        switch (source)
        {
            case IList<T> list:
                return index < list.Count ? Option<T>.Some(list[index]) : Option<T>.None;

            case IReadOnlyList<T> readOnlyList:
                return index < readOnlyList.Count ? Option<T>.Some(readOnlyList[index]) : Option<T>.None;
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

        // 陣列與 List<T> 走 span：省下 enumerator 配置與介面派送，並讓邊界檢查可被 JIT 消除。
        // 走訪過程不回呼使用者程式碼，集合不可能在期間被修改，因此持有 span 是安全的。
        return source switch
        {
            Option<T>[] array => SequenceCore<T>(array),
            List<Option<T>> list => SequenceCore<T>(CollectionsMarshal.AsSpan(list)),
            _ => SequenceEnumerable(source)
        };

        static Option<List<T>> SequenceEnumerable(IEnumerable<Option<T>> source)
        {
            // 先取得長度以預先配置容量，避免成功路徑上 List 反覆擴充（每次擴充都是一次配置 + 複製）。
            // 取捨：短路路徑（提前遇到 None）會配置用不到的容量。這是刻意的選擇——
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
    }

    /// <summary>
    /// <see cref="Sequence{T}(IEnumerable{Option{T}})"/> 的 span 快速路徑本體。
    /// </summary>
    /// <remarks>
    /// 以 <c>ref readonly</c> 走訪，避免每輪複製一份 <see cref="Option{T}"/>——
    /// 對較大的 <typeparamref name="T"/>（例如 <see cref="Guid"/>，<c>Option&lt;Guid&gt;</c> 為 20 bytes）差異可觀。
    /// </remarks>
    private static Option<List<T>> SequenceCore<T>(ReadOnlySpan<Option<T>> source) where T : notnull
    {
        var values = new List<T>(source.Length);

        foreach (ref readonly var option in source)
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

        // 快速路徑的理由與安全性說明同 Sequence 的 Option 版本。
        return source switch
        {
            Result<T, TE>[] array => SequenceCore<T, TE>(array),
            List<Result<T, TE>> list => SequenceCore<T, TE>(CollectionsMarshal.AsSpan(list)),
            _ => SequenceEnumerable(source)
        };

        static Result<List<T>, TE> SequenceEnumerable(IEnumerable<Result<T, TE>> source)
        {
            // 先取得長度以預先配置容量，避免成功路徑上 List 反覆擴充（每次擴充都是一次配置 + 複製）。
            // 取捨：短路路徑（提前遇到 Err）會配置用不到的容量。這是刻意的選擇——
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
    }

    /// <summary>
    /// <see cref="Sequence{T,TE}(IEnumerable{Result{T,TE}})"/> 的 span 快速路徑本體，理由同 <see cref="SequenceCore{T}"/>。
    /// </summary>
    private static Result<List<T>, TE> SequenceCore<T, TE>(ReadOnlySpan<Result<T, TE>> source)
        where T : notnull
        where TE : notnull
    {
        var values = new List<T>(source.Length);

        foreach (ref readonly var result in source)
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
    /// <para><b>容量策略</b></para>
    /// <para>
    /// 已知長度時只為<b>成功值</b>清單預配容量，錯誤清單維持惰性成長。
    /// 這是刻意的不對稱：本方法必然走訪全部元素（不像 <see cref="Sequence{T,TE}"/> 會短路），
    /// 因此預配不會浪費；而典型用途是驗證，絕大多數元素預期為 Ok，
    /// 若兩份都按總長度預配，錯誤清單幾乎必然是純粹浪費。
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

        // 快速路徑的理由與安全性說明同 Sequence。
        return source switch
        {
            Result<T, TE>[] array => PartitionCore<T, TE>(array),
            List<Result<T, TE>> list => PartitionCore<T, TE>(CollectionsMarshal.AsSpan(list)),
            _ => PartitionEnumerable(source)
        };

        static (List<T> Values, List<TE> Errors) PartitionEnumerable(IEnumerable<Result<T, TE>> source)
        {
            var values = source.TryGetNonEnumeratedCount(out var count) ? new List<T>(count) : [];
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
    }

    /// <summary>
    /// <see cref="Partition{T,TE}"/> 的 span 快速路徑本體，理由同 <see cref="SequenceCore{T}"/>。
    /// </summary>
    private static (List<T> Values, List<TE> Errors) PartitionCore<T, TE>(ReadOnlySpan<Result<T, TE>> source)
        where T : notnull
        where TE : notnull
    {
        var values = new List<T>(source.Length);
        List<TE> errors = [];

        foreach (ref readonly var result in source)
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
