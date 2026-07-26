namespace jIAnSoft.Rayleigh;

/// <summary>
/// 表示只有一個值的類型。此類型通常用於表示不回傳值（void）的方法成功完成。
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>
    /// 取得此類型的唯一值。
    /// </summary>
    public static readonly Unit Value = new();

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>
    /// 判斷兩個指定的 Unit 值是否相等。
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// 判斷兩個指定的 Unit 值是否不相等。
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// 比較目前的實例與另一個物件的順序。
    /// </summary>
    /// <param name="obj">要比較的物件。</param>
    /// <returns>
    /// 若 <paramref name="obj"/> 為 <c>null</c> 則為 <c>1</c>（依 .NET 慣例，任何值都大於 null）；
    /// 若為 <see cref="Unit"/> 則為 <c>0</c>（所有 Unit 值皆相等）。
    /// </returns>
    /// <exception cref="ArgumentException">當 <paramref name="obj"/> 不是 <see cref="Unit"/> 時擲出。</exception>
    /// <remarks>
    /// 實作非泛型的 <see cref="IComparable"/> 是為了與 <see cref="Option{T}"/>、<see cref="Result{T,TE}"/> 保持一致，
    /// 讓 <see cref="Unit"/> 能被 <c>ArrayList.Sort()</c> 等僅識別非泛型介面的舊有 API 使用。
    /// </remarks>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Unit => 0,
        _ => throw new ArgumentException($"Object must be of type {typeof(Unit)}", nameof(obj))
    };
}
