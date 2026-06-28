using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Engines.Base;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Responses;

namespace LlmLocalSharp.Core.Clients;

/// <summary>
/// High-level embedding session that wraps <see cref="ILlmEmbeddingSession"/>
/// with async access, thread safety, and lazy native context creation.
/// </summary>
public sealed class EmbeddingSession : IDisposable
{
    private volatile bool _disposed;
    private ILlmEmbeddingSession? _session;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly LlmEngineBase _engine;

    public LlmModelMetadata ModelMetadata
    {
        get
        {
            this._gate.Wait();
            try
            {
                this.EnsureCreated();

                return this._session!.ModelMetadata;
            }
            finally
            {
                this._gate.Release();
            }
        }
    }

    /// <summary>Creates a high-level embedding session for the given engine.</summary>
    public EmbeddingSession(LlmEngineBase engine)
    {
        this._engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <summary>
    /// Encodes the given text and returns its embedding vector.
    /// Thread-safe — concurrent calls are serialized via a gate.
    /// </summary>
    public async Task<LocalEmbeddingResponse> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        await this._gate.WaitAsync(cancellationToken);

        try
        {
            this.EnsureCreated();

            ReadOnlySpan<float> embedding = this._session!.Embed(text);

            // Copy span to array — the span is only valid during the native call
            float[] vector = embedding.ToArray();

            return new LocalEmbeddingResponse
            {
                Vector = vector,
                Dimension = this._session.EmbeddingDimension
            };
        }
        finally
        {
            this._gate.Release();
        }
    }

    /// <summary>Disposes the native embedding session if it has been created.</summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._gate.Wait();

        try
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            this._session?.Dispose();
            this._session = null;
        }
        finally
        {
            this._gate.Release();
            this._gate.Dispose();
        }
    }

    /// <summary>Creates the native embedding session on first use.</summary>
    private void EnsureCreated()
    {
        if (this._session is null)
        {
            this._session = this._engine.CreateEmbeddingSession();
        }
    }
}
