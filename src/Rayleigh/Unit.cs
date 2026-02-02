namespace jIAnSoft.Rayleigh;

/// <summary>
/// 表示只有一個值的類型。此類型通常用於表示不回傳值（void）的方法成功完成。
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
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
}
