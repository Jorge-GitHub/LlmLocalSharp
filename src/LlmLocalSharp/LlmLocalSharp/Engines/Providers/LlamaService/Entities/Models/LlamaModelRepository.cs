using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;
using System.Collections.Concurrent;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Entities.Models;

/// <summary>
/// Per-engine repository that maps normalized model paths to shared model handles.
/// Uses lazy "get or create" semantics with reference counting so multiple sessions
/// can share the same loaded model.
/// </summary>
internal sealed class LlamaModelRepository : IDisposable
{
    private readonly ConcurrentDictionary<string, SharedModelHandle> _models = new();
    private readonly LlamaNativeApi _native;
    private readonly object _loadLock = new();
    private bool _disposed;

    /// <summary>Creates a model repository for a llama.cpp native API instance.</summary>
    public LlamaModelRepository(LlamaNativeApi native)
    {
        this._native = native ?? throw new ArgumentNullException(nameof(native));
    }

    /// <summary>
    /// Returns an existing shared model (with an incremented reference) or loads a new one.
    /// The caller must dispose the returned handle when done.
    /// </summary>
    public SharedModelHandle AcquireModel(string modelPath, int gpuLayers)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Model path is required.", nameof(modelPath));
        }

        string normalizedPath = Path.GetFullPath(modelPath);

        // Fast path: model already loaded
        if (this._models.TryGetValue(normalizedPath, out SharedModelHandle? existing))
        {
            try
            {
                existing.AddReference();

                return existing;
            }
            catch (ObjectDisposedException)
            {
                // Race: model was disposed between TryGetValue and AddReference.
                // Fall through to the load path.
            }
        }

        // Slow path: double-checked locking for model loading
        lock (this._loadLock)
        {
            if (this._models.TryGetValue(normalizedPath, out SharedModelHandle? rechecked))
            {
                try
                {
                    rechecked.AddReference();

                    return rechecked;
                }
                catch (ObjectDisposedException)
                {
                    // Was disposed between check and AddReference; load a new one.
                }
            }

            SharedModelHandle handle = this.LoadModel(normalizedPath, gpuLayers);
            this._models[normalizedPath] = handle;

            return handle;
        }
    }

    /// <summary>Loads a model and builds metadata for the shared handle.</summary>
    private SharedModelHandle LoadModel(string normalizedPath, int gpuLayers)
    {
        this._native.BackendInitOnce();

        LlamaModelParams modelParameters = this._native.ModelDefaultParams();
        modelParameters.n_gpu_layers = gpuLayers;

        LlmSafeHandle modelHandle = this._native.LoadModel(normalizedPath, modelParameters);

        IntPtr vocabularyPointer = this._native.GetVocab(modelHandle.DangerousGetHandle());
        if (vocabularyPointer == IntPtr.Zero)
        {
            modelHandle.Dispose();
            throw new InvalidOperationException(
                $"Failed to get vocabulary handle for model: {normalizedPath}");
        }

        LlmModelMetadata metadata = new()
        {
            ModelPath = normalizedPath,
            Description = this._native.GetModelDescription(modelHandle.DangerousGetHandle()),
            VocabSize = this._native.GetVocabSize(vocabularyPointer),
            TrainingContextLength = this._native.GetTrainingContextLength(modelHandle.DangerousGetHandle()),
            FileSizeBytes = this._native.GetModelSize(modelHandle.DangerousGetHandle()),
            ParameterCount = this._native.GetModelParameterCount(modelHandle.DangerousGetHandle()),
            EmbeddingDimension = this._native.GetEmbeddingDimension(modelHandle.DangerousGetHandle()),
        };

        SharedModelHandle handle = new SharedModelHandle(
            modelHandle, vocabularyPointer, normalizedPath, metadata);

        handle.OnFinalRelease = this.OnModelFinalRelease;

        return handle;
    }

    /// <summary>Removes a model from the cache after its final reference is released.</summary>
    private void OnModelFinalRelease(SharedModelHandle handle)
    {
        this._models.TryRemove(handle.ModelPath, out _);
    }

    /// <summary>Disposes all cached model handles.</summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        foreach (SharedModelHandle handle in this._models.Values)
        {
            handle.Dispose();
        }

        this._models.Clear();
    }
}
