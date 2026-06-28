using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;

[StructLayout(LayoutKind.Sequential)]
public struct LlamaContextParams
{
    public uint n_ctx;                // uint32_t
    public uint n_batch;              // uint32_t
    public uint n_ubatch;             // uint32_t
    public uint n_seq_max;            // uint32_t
    public int n_threads;             // int32_t
    public int n_threads_batch;       // int32_t
    public int rope_scaling_type;     // enum llama_rope_scaling_type
    public int pooling_type;          // enum llama_pooling_type
    public int attention_type;        // enum llama_attention_type
    public int flash_attn_type;       // enum llama_flash_attn_type
    public float rope_freq_base;
    public float rope_freq_scale;
    public float yarn_ext_factor;
    public float yarn_attn_factor;
    public float yarn_beta_fast;
    public float yarn_beta_slow;
    public uint yarn_orig_ctx;
    public float defrag_thold;
    public IntPtr cb_eval;            // ggml_backend_sched_eval_callback
    public IntPtr cb_eval_user_data;  // void*
    public int type_k;                // enum ggml_type
    public int type_v;                // enum ggml_type
    public IntPtr abort_callback;     // ggml_abort_callback
    public IntPtr abort_callback_data;// void*

    [MarshalAs(UnmanagedType.I1)] public bool embeddings;
    [MarshalAs(UnmanagedType.I1)] public bool offload_kqv;
    [MarshalAs(UnmanagedType.I1)] public bool no_perf;
    [MarshalAs(UnmanagedType.I1)] public bool op_offload;
    [MarshalAs(UnmanagedType.I1)] public bool swa_full;
    [MarshalAs(UnmanagedType.I1)] public bool kv_unified;

    public IntPtr samplers;           // struct llama_sampler_seq_config*
    public nuint n_samplers;          // size_t
}
