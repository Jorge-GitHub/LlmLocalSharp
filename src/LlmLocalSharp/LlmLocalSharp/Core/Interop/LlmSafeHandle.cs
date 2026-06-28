using System.Runtime.InteropServices;

namespace LlmLocalSharp.Core.Interop;

/// <summary>
/// A SafeHandle wrapper for native pointers with a custom free function.
/// Use this when you want deterministic + GC-backed cleanup.
/// </summary>
public sealed class LlmSafeHandle : SafeHandle
{
    private readonly Action<IntPtr> _free;

    /// <summary>Initializes a safe handle with the native free callback.</summary>
    private LlmSafeHandle(Action<IntPtr> free)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        this._free = free ?? throw new ArgumentNullException(nameof(free));
    }

    public override bool IsInvalid => this.handle == IntPtr.Zero;

    /// <summary>Wraps a raw native pointer in a safe handle.</summary>
    public static LlmSafeHandle Create(IntPtr rawHandle, Action<IntPtr> free)
    {
        if (rawHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Invalid native handle (null).");
        }

        LlmSafeHandle safe = new LlmSafeHandle(free);
        safe.SetHandle(rawHandle);
        return safe;
    }

    /// <summary>Releases the native handle through the configured free callback.</summary>
    protected override bool ReleaseHandle()
    {
        this._free(this.handle);
        return true;
    }
}
