namespace jIAnSoft.Rayleigh.Benchmarks;

// Frozen from the working tree at review start (2026-10-11). Only the extension modifier was removed.
internal static class ValuesIteratorBaseline
{
    public static IEnumerable<T> Values<T>(IEnumerable<Option<T>> source) where T : notnull
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

}
