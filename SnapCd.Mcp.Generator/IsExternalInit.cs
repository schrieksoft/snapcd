// Polyfill required for `init` setters and `record` types on netstandard2.0.
// Source generators target netstandard2.0 per Roslyn requirements; this shim lets us use
// modern C# syntax in the generator code.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
