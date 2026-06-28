using LlmLocalSharp.Core.Entities.Constants;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;
using System.Runtime.InteropServices;
using System.Text;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives;

/// <summary>
/// Managed wrapper around the raw llama.cpp function pointers.
/// Thread-safe for backend init; all other calls assume single-threaded per context.
/// </summary>
public sealed class LlamaNativeApi
{
    private readonly GgmlNativeApi? _ggml;
    private readonly Lazy<IntPtr> _library;
    private readonly Lazy<LlamaNativeDelegate> _api;
    private readonly string? _nativeLibraryFolder;

    private int _backendInited;
    private int _loggerSet;

    /// <param name="loadLibrary">Factory that loads the main llama shared library.</param>
    /// <param name="loadGgmlLibrary">Optional factory for a separate ggml shared library.</param>
    /// <param name="nativeLibraryFolder">
    /// The resolved folder that contains all native DLLs (llama.dll, ggml*.dll, etc.).
    /// Used to temporarily set the working directory so that ggml_backend_load_all
    /// can discover backend plugins (ggml-cpu-*.dll) at runtime.
    /// </param>
    public LlamaNativeApi(
        Func<IntPtr> loadLibrary,
        Func<IntPtr>? loadGgmlLibrary = null,
        string? nativeLibraryFolder = null)
    {
        this._nativeLibraryFolder = nativeLibraryFolder;
        this._library = new Lazy<IntPtr>(loadLibrary, LazyThreadSafetyMode.ExecutionAndPublication);
        this._api = new Lazy<LlamaNativeDelegate>(
            () => new LlamaNativeDelegate(this._library.Value),
            LazyThreadSafetyMode.ExecutionAndPublication);

        this._ggml = loadGgmlLibrary is null
            ? null
            : new GgmlNativeApi(loadGgmlLibrary);
    }

    // Static so it won't be GC'd while native code holds a reference
    private static volatile Action<int, string>? _logHandler;

    private static readonly LlamaNativeDelegate.LlamaLogCallback _logCallback =
        (level, message, _) =>
        {
            _logHandler?.Invoke(level, message);
        };

    /// <summary>Sets the managed callback used for native llama.cpp log messages.</summary>
    internal static void SetLogHandler(Action<int, string>? handler)
    {
        _logHandler = handler;
    }

    // ──────────── init ────────────

    /// <summary>Initializes llama.cpp and loads available ggml backends once.</summary>
    public void BackendInitOnce()
    {
        if (Interlocked.Exchange(ref _backendInited, 1) == 0)
        {
            this.EnableLoggingOnce();
            this._api.Value.llama_backend_init();
            this.LoadBackendsWithDirectoryContext();
        }
    }

    /// <summary>
    /// Calls ggml_backend_load_all while the working directory is set to the
    /// native library folder. Tries the main llama lib first, then ggml.dll.
    /// </summary>
    private void LoadBackendsWithDirectoryContext()
    {
        string? previousDirectory = null;

        try
        {
            // Temporarily change working directory so ggml can find its plugins
            if (!string.IsNullOrEmpty(_nativeLibraryFolder) &&
                System.IO.Directory.Exists(_nativeLibraryFolder))
            {
                previousDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = this._nativeLibraryFolder;
            }

            // Try loading from main llama library first
            if (this.TryLoadBackendsFromMainLibrary())
            {
                return;
            }

            // Fall back to separate ggml.dll
            try
            {
                this._ggml?.BackendLoadAllOnce();
            }
            catch (Exception ex) when (
                ex is System.IO.FileNotFoundException ||
                ex is EntryPointNotFoundException ||
                ex is DllNotFoundException)
            {
                throw;
            }
        }
        finally
        {
            // Restore working directory
            if (previousDirectory is not null)
            {
                Environment.CurrentDirectory = previousDirectory;
            }
        }
    }

    /// <summary>Attempts to load ggml backends from the main llama library.</summary>
    private bool TryLoadBackendsFromMainLibrary()
    {
        try
        {
            IntPtr lib = _library.Value;
            if (NativeLibrary.TryGetExport(lib, "ggml_backend_load_all", out var proc))
            {
                var loadAll = Marshal.GetDelegateForFunctionPointer<GgmlBackendLoadAllDelegate>(proc);
                loadAll();

                return true;
            }
        }
        catch
        {
            throw;
        }

        return false;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GgmlBackendLoadAllDelegate();

    /// <summary>Registers the native log callback once.</summary>
    private void EnableLoggingOnce()
    {
        if (Interlocked.Exchange(ref _loggerSet, 1) == 0)
        {
            this._api.Value.llama_log_set(_logCallback, IntPtr.Zero);
        }
    }

    // ──────────── defaults ────────────

    /// <summary>Gets llama.cpp's default model parameters.</summary>
    public LlamaModelParams ModelDefaultParams()
        => this._api.Value.llama_model_default_params();

    /// <summary>Gets llama.cpp's default context parameters.</summary>
    public LlamaContextParams ContextDefaultParams()
        => this._api.Value.llama_context_default_params();

    // ──────────── model / context lifecycle ────────────

    /// <summary>Loads a model file and returns a safe model handle.</summary>
    public LlmSafeHandle LoadModel(string modelPath, in LlamaModelParams modelParams)
    {
        this.BackendInitOnce();

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException(ExceptionMessage.ModelPathIsRequired,
                nameof(modelPath));
        }

        IntPtr rawHandle = this._api.Value.llama_model_load_from_file(modelPath, modelParams);
        if (rawHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to load model from '{modelPath}'.");
        }

        return LlmSafeHandle.Create(rawHandle, pointer => this._api.Value.llama_free_model(pointer));
    }

    /// <summary>Creates a llama.cpp context for a loaded model.</summary>
    public LlmSafeHandle CreateContext(IntPtr model, in LlamaContextParams contextParams)
    {
        this.BackendInitOnce();

        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired,
                nameof(model));
        }

        IntPtr rawHandle = this._api.Value.llama_new_context_with_model(model, contextParams);
        if (rawHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create llama context.");
        }

        return LlmSafeHandle.Create(rawHandle, pointer => this._api.Value.llama_free(pointer));
    }

    // ──────────── vocab / tokens ────────────

    /// <summary>Gets the vocabulary pointer for a loaded model.</summary>
    public IntPtr GetVocab(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired,
                nameof(model));
        }

        return this._api.Value.llama_model_get_vocab(model);
    }

    /// <summary>Tokenizes text into a caller-provided token buffer.</summary>
    public int Tokenize(
        IntPtr vocab, string text, IntPtr tokens, int nTokensMax,
        bool addSpecial, bool parseSpecial)
    {
        if (vocab == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(vocab));
        }
        if (tokens == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(tokens));
        }
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        int textLen = Encoding.UTF8.GetByteCount(text);

        return this._api.Value.llama_tokenize(vocab, text,
            textLen, tokens, nTokensMax, addSpecial, parseSpecial);
    }

    /// <summary>Converts a token to its text piece in a caller-provided buffer.</summary>
    public int TokenToPiece(IntPtr vocab, int token, IntPtr buf,
        int length, int lstrip, bool special)
    {
        if (vocab == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(vocab));
        }

        if (buf == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(buf));
        }

        return this._api.Value.llama_token_to_piece(
            vocab, token, buf, length, lstrip, special);
    }

    /// <summary>Returns true if <paramref name="token"/> is an end-of-generation token (EOS, EOT, etc.).</summary>
    public bool TokenIsEog(IntPtr vocab, int token)
    {
        if (vocab == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(vocab));
        }

        return _api.Value.llama_token_is_eog(vocab, token);
    }

    // ──────────── model metadata ────────────

    /// <summary>Gets the number of tokens in the vocabulary.</summary>
    public int GetVocabSize(IntPtr vocab)
    {
        if (vocab == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(vocab));
        }

        return this._api.Value.llama_vocab_n_tokens(vocab);
    }

    /// <summary>Gets the model training context length.</summary>
    public int GetTrainingContextLength(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired, nameof(model));
        }

        return this._api.Value.llama_model_n_ctx_train(model);
    }

    /// <summary>Gets llama.cpp's model description string.</summary>
    public string GetModelDescription(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired, nameof(model));
        }

        // 256 bytes is enough for model description strings like "llama 7B Q4_K_M"
        IntPtr buf = Marshal.AllocHGlobal(256);
        try
        {
            int written = this._api.Value.llama_model_desc(model, buf, 256);
            if (written <= 0)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUTF8(buf) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>Gets the model file size in bytes.</summary>
    public ulong GetModelSize(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired, nameof(model));
        }

        return this._api.Value.llama_model_size(model);
    }

    /// <summary>Gets the number of model parameters.</summary>
    public ulong GetModelParameterCount(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired, nameof(model));
        }

        return this._api.Value.llama_model_n_params(model);
    }

    // ──────────── embeddings ────────────

    /// <summary>Gets the embedding vector pointer for a decoded token index.</summary>
    public IntPtr GetEmbeddings(IntPtr context, int index)
    {
        if (context == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(context));
        }

        return this._api.Value.llama_get_embeddings_ith(context, index);
    }

    /// <summary>Gets the model embedding dimension.</summary>
    public int GetEmbeddingDimension(IntPtr model)
    {
        if (model == IntPtr.Zero)
        {
            throw new ArgumentException(ExceptionMessage.ModelHandleIsRequired, nameof(model));
        }

        return this._api.Value.llama_model_n_embd(model);
    }

    // ──────────── memory (KV cache) ────────────

    /// <summary>Clears the llama.cpp context memory.</summary>
    public void MemoryClear(IntPtr context)
    {
        if (context == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(context));
        }

        IntPtr memory = this._api.Value.llama_get_memory(context);
        if (memory == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get memory handle from context.");
        }

        this._api.Value.llama_memory_clear(memory, true);
    }

    // ──────────── decode / logits ────────────

    /// <summary>Decodes a batch into the llama.cpp context.</summary>
    public int Decode(IntPtr context, LlamaBatch batch)
    {
        if (context == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(context));
        }

        return this._api.Value.llama_decode(context, batch);
    }

    /// <summary>Gets the logits pointer for the current context state.</summary>
    public IntPtr GetLogits(IntPtr context)
    {
        if (context == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(context));
        }

        return this._api.Value.llama_get_logits(context);
    }

    // ──────────── batch helpers ────────────

    /// <summary>Allocates a llama.cpp batch.</summary>
    public LlamaBatch BatchInit(int maximumNumberOfTokensPerBatch,
        int embedding, int maximumNumberOfParallelSequences)
        => this._api.Value.llama_batch_init(maximumNumberOfTokensPerBatch,
            embedding, maximumNumberOfParallelSequences);

    /// <summary>Creates a one-shot batch around a caller-provided token buffer.</summary>
    public LlamaBatch BatchGetOne(IntPtr tokens, int nTokens, int pos0, int seqId)
    {
        if (tokens == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(tokens));
        }

        return this._api.Value.llama_batch_get_one(tokens, nTokens, pos0, seqId);
    }

    /// <summary>Frees a llama.cpp batch.</summary>
    public void BatchFree(LlamaBatch batch)
        => this._api.Value.llama_batch_free(batch);

    // ──────────── sampler ────────────

    /// <summary>Gets llama.cpp's default sampler chain parameters.</summary>
    public LlamaSamplerChainParams SamplerChainDefaultParams()
        => this._api.Value.llama_sampler_chain_default_params();

    /// <summary>Creates a sampler chain safe handle.</summary>
    public LlmSafeHandle SamplerChainInit(LlamaSamplerChainParams chainParams)
    {
        IntPtr rawHandle = this._api.Value.llama_sampler_chain_init(chainParams);
        if (rawHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create sampler chain.");
        }

        return LlmSafeHandle.Create(rawHandle, pointer => this._api.Value.llama_sampler_free(pointer));
    }

    /// <summary>Adds a sampler to an existing sampler chain.</summary>
    public void SamplerChainAdd(IntPtr chain, IntPtr sampler)
    {
        if (chain == IntPtr.Zero) throw new ArgumentException(nameof(chain));
        if (sampler == IntPtr.Zero) throw new ArgumentException(nameof(sampler));
        this._api.Value.llama_sampler_chain_add(chain, sampler);
    }

    /// <summary>Creates a temperature sampler.</summary>
    public IntPtr SamplerInitTemp(float temp)
        => this._api.Value.llama_sampler_init_temp(temp);

    /// <summary>Creates a top-p sampler.</summary>
    public IntPtr SamplerInitTopP(float topP, nuint minKeep = 1)
        => this._api.Value.llama_sampler_init_top_p(topP, minKeep);

    /// <summary>Creates a top-k sampler.</summary>
    public IntPtr SamplerInitTopK(int topK)
        => this._api.Value.llama_sampler_init_top_k(topK);

    /// <summary>Creates a min-p sampler.</summary>
    public IntPtr SamplerInitMinP(float minP, nuint minKeep = 1)
        => this._api.Value.llama_sampler_init_min_p(minP, minKeep);

    /// <summary>Creates a repetition and presence/frequency penalty sampler.</summary>
    public IntPtr SamplerInitPenalties(
        int penaltyLastN, float penaltyRepeat,
        float penaltyFreq, float penaltyPresent)
        => this._api.Value.llama_sampler_init_penalties(
            penaltyLastN, penaltyRepeat, penaltyFreq, penaltyPresent);

    /// <summary>
    /// Creates the distribution sampler that actually selects a token.
    /// Must be the LAST sampler added to the chain.
    /// </summary>
    public IntPtr SamplerInitDist(uint seed)
        => this._api.Value.llama_sampler_init_dist(seed);

    /// <summary>Samples the next token from a sampler chain.</summary>
    public int SamplerSample(IntPtr sampler, IntPtr context, int idx)
    {
        if (sampler == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(sampler));
        }

        if (context == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(context));
        }

        return this._api.Value.llama_sampler_sample(sampler, context, idx);
    }

    /// <summary>Accepts a sampled token into sampler state.</summary>
    public void SamplerAccept(IntPtr sampler, int token)
    {
        if (sampler == IntPtr.Zero)
        {
            throw new ArgumentException(nameof(sampler));
        }

        this._api.Value.llama_sampler_accept(sampler, token);
    }

}
