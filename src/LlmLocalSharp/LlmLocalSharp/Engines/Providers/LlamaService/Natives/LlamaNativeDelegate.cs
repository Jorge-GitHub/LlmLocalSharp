using LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;
using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives;

/// <summary>
/// Holds all function-pointer delegates resolved from the llama.cpp shared library.
/// </summary>
internal sealed class LlamaNativeDelegate
{
    // ──────────── logging ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void LlamaLogCallback(
        int level,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string message,
        IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_log_set_delegate(LlamaLogCallback callback, IntPtr userData);

    internal readonly llama_log_set_delegate llama_log_set;

    // ──────────── core init / defaults ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_backend_init_delegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate LlamaModelParams llama_model_default_params_delegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate LlamaContextParams llama_context_default_params_delegate();

    internal readonly llama_backend_init_delegate llama_backend_init;
    internal readonly llama_model_default_params_delegate llama_model_default_params;
    internal readonly llama_context_default_params_delegate llama_context_default_params;

    // ──────────── lifecycle ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_model_load_from_file_delegate(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        in LlamaModelParams parameters);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_new_context_with_model_delegate(
        IntPtr model, in LlamaContextParams parameters);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_free_model_delegate(IntPtr model);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_free_delegate(IntPtr ctx);

    internal readonly llama_model_load_from_file_delegate llama_model_load_from_file;
    internal readonly llama_new_context_with_model_delegate llama_new_context_with_model;
    internal readonly llama_free_model_delegate llama_free_model;
    internal readonly llama_free_delegate llama_free;

    // ──────────── vocab / tokens ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_model_get_vocab_delegate(IntPtr model);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_tokenize_delegate(
        IntPtr vocab,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
        int text_len,
        IntPtr tokens, int n_tokens_max,
        [MarshalAs(UnmanagedType.I1)] bool add_special,
        [MarshalAs(UnmanagedType.I1)] bool parse_special);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_token_to_piece_delegate(
        IntPtr vocab, int token, IntPtr buf, int length,
        int lstrip, [MarshalAs(UnmanagedType.I1)] bool special);

    // FIX: Added — needed to detect end-of-generation
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal delegate bool llama_token_is_eog_delegate(IntPtr vocab, int token);

    internal readonly llama_model_get_vocab_delegate llama_model_get_vocab;
    internal readonly llama_tokenize_delegate llama_tokenize;
    internal readonly llama_token_to_piece_delegate llama_token_to_piece;
    internal readonly llama_token_is_eog_delegate llama_token_is_eog;

    // ──────────── model metadata ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_vocab_n_tokens_delegate(IntPtr vocab);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_model_n_ctx_train_delegate(IntPtr model);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_model_desc_delegate(IntPtr model, IntPtr buf, nuint bufSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ulong llama_model_size_delegate(IntPtr model);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ulong llama_model_n_params_delegate(IntPtr model);

    internal readonly llama_vocab_n_tokens_delegate llama_vocab_n_tokens;
    internal readonly llama_model_n_ctx_train_delegate llama_model_n_ctx_train;
    internal readonly llama_model_desc_delegate llama_model_desc;
    internal readonly llama_model_size_delegate llama_model_size;
    internal readonly llama_model_n_params_delegate llama_model_n_params;

    // ──────────── memory (KV cache) ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_get_memory_delegate(IntPtr ctx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_memory_clear_delegate(
        IntPtr memory, [MarshalAs(UnmanagedType.I1)] bool data);

    internal readonly llama_get_memory_delegate llama_get_memory;
    internal readonly llama_memory_clear_delegate llama_memory_clear;

    // ──────────── decode / logits ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_decode_delegate(IntPtr ctx, LlamaBatch batch);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_get_logits_delegate(IntPtr ctx);

    internal readonly llama_decode_delegate llama_decode;
    internal readonly llama_get_logits_delegate llama_get_logits;

    // ──────────── embeddings ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_get_embeddings_ith_delegate(IntPtr ctx, int index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_model_n_embd_delegate(IntPtr model);

    internal readonly llama_get_embeddings_ith_delegate llama_get_embeddings_ith;
    internal readonly llama_model_n_embd_delegate llama_model_n_embd;

    // ──────────── batch helpers ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate LlamaBatch llama_batch_init_delegate(int n_tokens, int embd, int n_seq_max);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate LlamaBatch llama_batch_get_one_delegate(
        IntPtr tokens, int n_tokens, int pos_0, int seq_id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_batch_free_delegate(LlamaBatch batch);

    internal readonly llama_batch_init_delegate llama_batch_init;
    internal readonly llama_batch_get_one_delegate llama_batch_get_one;
    internal readonly llama_batch_free_delegate llama_batch_free;

    // ──────────── sampler chain ────────────

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate LlamaSamplerChainParams llama_sampler_chain_default_params_delegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_chain_init_delegate(LlamaSamplerChainParams p);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_sampler_chain_add_delegate(IntPtr chain, IntPtr sampler);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_temp_delegate(float temp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_top_p_delegate(float top_p, nuint min_keep);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_top_k_delegate(int topK);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_min_p_delegate(float minP, nuint minKeep);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_penalties_delegate(
        int penaltyLastN,
        float penaltyRepeat,
        float penaltyFreq,
        float penaltyPresent);

    // FIX: Added — the distribution sampler that actually picks a token
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr llama_sampler_init_dist_delegate(uint seed);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int llama_sampler_sample_delegate(IntPtr sampler, IntPtr ctx, int idx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_sampler_accept_delegate(IntPtr sampler, int token);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void llama_sampler_free_delegate(IntPtr sampler);

    internal readonly llama_sampler_chain_default_params_delegate llama_sampler_chain_default_params;
    internal readonly llama_sampler_chain_init_delegate llama_sampler_chain_init;
    internal readonly llama_sampler_chain_add_delegate llama_sampler_chain_add;
    internal readonly llama_sampler_init_temp_delegate llama_sampler_init_temp;
    internal readonly llama_sampler_init_top_p_delegate llama_sampler_init_top_p;
    internal readonly llama_sampler_init_top_k_delegate llama_sampler_init_top_k;
    internal readonly llama_sampler_init_min_p_delegate llama_sampler_init_min_p;
    internal readonly llama_sampler_init_penalties_delegate llama_sampler_init_penalties;
    internal readonly llama_sampler_init_dist_delegate llama_sampler_init_dist;
    internal readonly llama_sampler_sample_delegate llama_sampler_sample;
    internal readonly llama_sampler_accept_delegate llama_sampler_accept;
    internal readonly llama_sampler_free_delegate llama_sampler_free;

    // ═══════════════════════════════════════════
    // Constructor — resolve all function pointers
    // ═══════════════════════════════════════════

    /// <summary>Resolves all required llama.cpp function pointers from the library.</summary>
    internal LlamaNativeDelegate(IntPtr lib)
    {
        // logging
        llama_log_set = Get<llama_log_set_delegate>(lib, "llama_log_set");

        // init/defaults
        llama_backend_init = Get<llama_backend_init_delegate>(lib, "llama_backend_init");
        llama_model_default_params = Get<llama_model_default_params_delegate>(lib, "llama_model_default_params");
        llama_context_default_params = Get<llama_context_default_params_delegate>(lib, "llama_context_default_params");

        // lifecycle
        llama_model_load_from_file = Get<llama_model_load_from_file_delegate>(lib, "llama_model_load_from_file");
        llama_new_context_with_model = Get<llama_new_context_with_model_delegate>(lib, "llama_new_context_with_model");
        llama_free_model = Get<llama_free_model_delegate>(lib, "llama_free_model");
        llama_free = Get<llama_free_delegate>(lib, "llama_free");

        // vocab/tokens
        llama_model_get_vocab = Get<llama_model_get_vocab_delegate>(lib, "llama_model_get_vocab");
        llama_tokenize = Get<llama_tokenize_delegate>(lib, "llama_tokenize");
        llama_token_to_piece = Get<llama_token_to_piece_delegate>(lib, "llama_token_to_piece");
        llama_token_is_eog = Get<llama_token_is_eog_delegate>(lib, "llama_token_is_eog");

        // model metadata
        llama_vocab_n_tokens = Get<llama_vocab_n_tokens_delegate>(lib, "llama_vocab_n_tokens");
        llama_model_n_ctx_train = Get<llama_model_n_ctx_train_delegate>(lib, "llama_model_n_ctx_train");
        llama_model_desc = Get<llama_model_desc_delegate>(lib, "llama_model_desc");
        llama_model_size = Get<llama_model_size_delegate>(lib, "llama_model_size");
        llama_model_n_params = Get<llama_model_n_params_delegate>(lib, "llama_model_n_params");

        // memory (KV cache)
        llama_get_memory = Get<llama_get_memory_delegate>(lib, "llama_get_memory");
        llama_memory_clear = Get<llama_memory_clear_delegate>(lib, "llama_memory_clear");

        // decode/logits
        llama_decode = Get<llama_decode_delegate>(lib, "llama_decode");
        llama_get_logits = Get<llama_get_logits_delegate>(lib, "llama_get_logits");

        // embeddings
        llama_get_embeddings_ith = Get<llama_get_embeddings_ith_delegate>(lib, "llama_get_embeddings_ith");
        llama_model_n_embd = Get<llama_model_n_embd_delegate>(lib, "llama_model_n_embd");

        // batch
        llama_batch_init = Get<llama_batch_init_delegate>(lib, "llama_batch_init");
        llama_batch_get_one = Get<llama_batch_get_one_delegate>(lib, "llama_batch_get_one");
        llama_batch_free = Get<llama_batch_free_delegate>(lib, "llama_batch_free");

        // sampler
        llama_sampler_chain_default_params = Get<llama_sampler_chain_default_params_delegate>(lib, "llama_sampler_chain_default_params");
        llama_sampler_chain_init = Get<llama_sampler_chain_init_delegate>(lib, "llama_sampler_chain_init");
        llama_sampler_chain_add = Get<llama_sampler_chain_add_delegate>(lib, "llama_sampler_chain_add");
        llama_sampler_init_temp = Get<llama_sampler_init_temp_delegate>(lib, "llama_sampler_init_temp");
        llama_sampler_init_top_p = Get<llama_sampler_init_top_p_delegate>(lib, "llama_sampler_init_top_p");
        llama_sampler_init_top_k = Get<llama_sampler_init_top_k_delegate>(lib, "llama_sampler_init_top_k");
        llama_sampler_init_min_p = Get<llama_sampler_init_min_p_delegate>(lib, "llama_sampler_init_min_p");
        llama_sampler_init_penalties = Get<llama_sampler_init_penalties_delegate>(lib, "llama_sampler_init_penalties");
        llama_sampler_init_dist = Get<llama_sampler_init_dist_delegate>(lib, "llama_sampler_init_dist");
        llama_sampler_sample = Get<llama_sampler_sample_delegate>(lib, "llama_sampler_sample");
        llama_sampler_accept = Get<llama_sampler_accept_delegate>(lib, "llama_sampler_accept");
        llama_sampler_free = Get<llama_sampler_free_delegate>(lib, "llama_sampler_free");
    }

    /// <summary>Resolves a native export as the requested delegate type.</summary>
    private static T Get<T>(IntPtr lib, string name) where T : Delegate
    {
        if (!System.Runtime.InteropServices.NativeLibrary.TryGetExport(lib, name, out var proc))
            throw new EntryPointNotFoundException($"Could not find '{name}' in native library.");

        return Marshal.GetDelegateForFunctionPointer<T>(proc);
    }
}
