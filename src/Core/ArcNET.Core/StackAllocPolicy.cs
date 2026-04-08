namespace ArcNET.Core;

/// <summary>
/// Shared constants governing stack-allocation size limits.
/// Buffers at or below <see cref="MaxStackAllocBytes"/> may use <c>stackalloc byte[n]</c>.
/// Larger buffers must be rented from <see cref="System.Buffers.ArrayPool{T}.Shared"/>.
/// </summary>
public static class StackAllocPolicy
{
    /// <summary>Maximum number of bytes that may be stack-allocated.</summary>
    public const int MaxStackAllocBytes = 256;
}
