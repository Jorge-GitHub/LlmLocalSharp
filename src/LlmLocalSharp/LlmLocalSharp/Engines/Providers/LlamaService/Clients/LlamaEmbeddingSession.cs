using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;
using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Clients;

/// <summary>
/// A single embedding session backed by llama.cpp native inference.
/// Creates a context with embeddings enabled and no sampler chain.
/// Reusable: the context is reset between calls to <see cref="Embed"/>.
/// </summary>
internal sealed class LlamaEmbeddingSession : ILlmEmbeddingSession
{
    private readonly LlamaNativeApi _native;
    private readonly SharedModelHandle _sharedModel;
    private readonly LlmSafeHandle _context;
    private readonly int _embeddingDimension;

    private int _position;

    public int EmbeddingDimension => this._embeddingDimension;
    public LlmModelMetadata ModelMetadata => this._sharedModel.Metadata;

    /// <summary>Creates an embedding session over a shared llama.cpp model.</summary>
    public LlamaEmbeddingSession(
        LlamaNativeApi native,
        SharedModelHandle sharedModel,
        LlmSessionSettings options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        this._native = native ?? throw new ArgumentNullException(nameof(native));
        this._sharedModel = sharedModel ?? throw new ArgumentNullException(nameof(sharedModel));
        this._embeddingDimension = sharedModel.Metadata.EmbeddingDimension;
        this._context = this.CreateEmbeddingContext(options);
    }

    /// <summary>Embeds text and returns a span over the native embedding vector.</summary>
    public ReadOnlySpan<float> Embed(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        // Reset context for each call so the session is reusable
        if (this._position > 0)
        {
            this._native.MemoryClear(this._context.DangerousGetHandle());
            this._position = 0;
        }

        this.TokenizeAndDecode(text);

        IntPtr embeddingsPointer = this._native.GetEmbeddings(
            this._context.DangerousGetHandle(), index: -1);

        if (embeddingsPointer == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to retrieve embedding vector from native context.");
        }

        unsafe
        {
            return new ReadOnlySpan<float>(
                (float*)embeddingsPointer.ToPointer(), this._embeddingDimension);
        }
    }

    /// <summary>Disposes the native context and releases the shared model reference.</summary>
    public void Dispose()
    {
        this._context.Dispose();
        this._sharedModel.Dispose();
    }

    /// <summary>Tokenizes the input text and decodes it into the embedding context.</summary>
    private void TokenizeAndDecode(string text)
    {
        int maxBufferTokens = Math.Max(512, text.Length * 2);
        IntPtr tokenBuffer = Marshal.AllocHGlobal(sizeof(int) * maxBufferTokens);

        try
        {
            int tokenCount = this._native.Tokenize(
                this._sharedModel.VocabularyPointer,
                text,
                tokenBuffer,
                maxBufferTokens,
                addSpecial: true,
                parseSpecial: true);

            if (tokenCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Tokenize returned {tokenCount}. Text may be too long or invalid.");
            }

            LlamaBatch batch = this._native.BatchGetOne(
                tokenBuffer, tokenCount, this._position, seqId: 0);

            int returnCode = this._native.Decode(
                this._context.DangerousGetHandle(), batch);

            if (returnCode != 0)
            {
                throw new InvalidOperationException(
                    $"Decode failed with rc={returnCode}.");
            }

            this._position += tokenCount;
        }
        finally
        {
            Marshal.FreeHGlobal(tokenBuffer);
        }
    }

    /// <summary>Creates a llama.cpp context configured for embedding generation.</summary>
    private LlmSafeHandle CreateEmbeddingContext(LlmSessionSettings options)
    {
        LlamaContextParams contextParams = this._native.ContextDefaultParams();
        contextParams.n_ctx = (uint)options.ContextSize;
        contextParams.n_threads = options.Threads;
        contextParams.n_threads_batch = options.Threads;
        contextParams.embeddings = true;
        contextParams.pooling_type = -1; // auto-detect

        return this._native.CreateContext(
            this._sharedModel.ModelPointer, contextParams);
    }
}
