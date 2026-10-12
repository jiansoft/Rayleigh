using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace jIAnSoft.Rayleigh;

// Keep this cold path outside Result<T, TE>: a shared generic caller should not
// need a generic dictionary lookup merely to reference the throw target.
internal static class ResultThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNullValue() => throw new ArgumentNullException("value");
}
