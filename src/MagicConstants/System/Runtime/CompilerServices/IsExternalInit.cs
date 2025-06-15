using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// This dummy class is required for init-only property support in netstandard2.0.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
