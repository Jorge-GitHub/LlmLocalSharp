using LlmLocalSharp.Core.Entities.Clients;

namespace LlmLocalSharp.Core.Interop;

/// <summary>
/// Wraps a native llama_model pointer with thread-safe reference counting.
/// The model is freed only when the last reference is released.
/// </summary>
internal sealed class SharedModelHandle : IDisposable
{
    private readonly LlmSafeHandle _modelHandle;
    private readonly object _lock = new();

    private int _referenceCount;
    private bool _disposed;

    public IntPtr ModelPointer => this._modelHandle.DangerousGetHandle();
    public IntPtr VocabularyPointer { get; }
    public string ModelPath { get; }
    public LlmModelMetadata Metadata { get; }

    public Action<SharedModelHandle>? OnFinalRelease { get; set; }

    /// <summary>Creates a shared model handle with an initial reference count.</summary>
    public SharedModelHandle(
        LlmSafeHandle modelHandle,
        IntPtr vocabularyPointer,
        string modelPath,
        LlmModelMetadata metadata)
    {
        this._modelHandle = modelHandle ?? throw new ArgumentNullException(nameof(modelHandle));

        if (modelHandle.IsInvalid)
        {
            throw new ArgumentException("Model handle must not be invalid.", nameof(modelHandle));
        }

        if (vocabularyPointer == IntPtr.Zero)
        {
            throw new ArgumentException("Vocabulary pointer must not be zero.", nameof(vocabularyPointer));
        }

        this.VocabularyPointer = vocabularyPointer;
        this.ModelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
        this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        this._referenceCount = 1;
    }

    /// <summary>Adds a live reference to the shared model handle.</summary>
    public void AddReference()
    {
        lock (this._lock)
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(SharedModelHandle),
                    $"Cannot add reference to disposed model: {this.ModelPath}");
            }

            this._referenceCount++;
        }
    }

    /// <summary>Releases one reference and frees the model when the last reference is gone.</summary>
    public void Dispose()
    {
        Action<SharedModelHandle>? finalReleaseCallback = null;

        lock (this._lock)
        {
            if (this._disposed)
            {
                return;
            }

            this._referenceCount--;

            if (this._referenceCount > 0)
            {
                return;
            }

            this._disposed = true;
            finalReleaseCallback = this.OnFinalRelease;
        }

        this._modelHandle.Dispose();
        finalReleaseCallback?.Invoke(this);
    }
}
