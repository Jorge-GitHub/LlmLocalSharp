using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives;

/// <summary>
/// Loads the ggml backend library and calls ggml_backend_load_all once.
/// Required for GPU backend discovery (CUDA, Vulkan, etc.).
/// </summary>
internal sealed class GgmlNativeApi
{
    private readonly Lazy<IntPtr> _lib;
    private readonly Lazy<Api> _api;
    private int _loaded;

    /// <summary>Creates a wrapper around a lazily loaded ggml library.</summary>
    internal GgmlNativeApi(Func<IntPtr> loadLibrary)
    {
        _lib = new Lazy<IntPtr>(loadLibrary, LazyThreadSafetyMode.ExecutionAndPublication);
        _api = new Lazy<Api>(() => new Api(_lib.Value), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Loads all ggml backend plugins once.</summary>
    public void BackendLoadAllOnce()
    {
        if (Interlocked.Exchange(ref _loaded, 1) == 0)
            _api.Value.ggml_backend_load_all();
    }

    private sealed class Api
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ggml_backend_load_all_delegate();

        internal readonly ggml_backend_load_all_delegate ggml_backend_load_all;

        /// <summary>Resolves ggml backend function pointers from the library.</summary>
        internal Api(IntPtr lib)
        {
            ggml_backend_load_all = Get<ggml_backend_load_all_delegate>(lib, "ggml_backend_load_all");
        }

        /// <summary>Resolves a native export as the requested delegate type.</summary>
        private static T Get<T>(IntPtr lib, string name) where T : Delegate
        {
            if (!NativeLibrary.TryGetExport(lib, name, out var proc))
                throw new EntryPointNotFoundException(name);
            return Marshal.GetDelegateForFunctionPointer<T>(proc);
        }
    }
}
