using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;

[StructLayout(LayoutKind.Sequential)]
public struct LlamaModelParams
{
    public IntPtr devices;                      // ggml_backend_dev_t*
    public IntPtr tensor_buft_overrides;        // llama_model_tensor_buft_override*
    public int n_gpu_layers;                    // int32_t
    public int split_mode;                      // enum llama_split_mode
    public int main_gpu;                        // int32_t
    public IntPtr tensor_split;                 // float*
    public IntPtr progress_callback;            // llama_progress_callback
    public IntPtr progress_callback_user_data;  // void*
    public IntPtr kv_overrides;                 // llama_model_kv_override*

    [MarshalAs(UnmanagedType.I1)] public bool vocab_only;
    [MarshalAs(UnmanagedType.I1)] public bool use_mmap;
    [MarshalAs(UnmanagedType.I1)] public bool use_direct_io;
    [MarshalAs(UnmanagedType.I1)] public bool use_mlock;
    [MarshalAs(UnmanagedType.I1)] public bool check_tensors;
    [MarshalAs(UnmanagedType.I1)] public bool use_extra_bufts;
    [MarshalAs(UnmanagedType.I1)] public bool no_host;
    [MarshalAs(UnmanagedType.I1)] public bool no_alloc;
}
