#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill so <c>record</c>/init-only properties compile under netstandard2.1 (this type ships with the
    /// runtime from .NET 5 on; older targets need a stand-in with this exact name/namespace).
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif
