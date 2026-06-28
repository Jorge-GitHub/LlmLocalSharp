using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Engines.Base;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Core.Entities.Settings.Engines;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Entities.Models;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives;
using LlmLocalSharp.Utilities;
using System.Diagnostics;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Base;

/// <summary>
/// Base class for llama.cpp-backed engines.
/// Subclasses only differ in which native library folder they point to.
/// Owns a <see cref="LlamaModelRepository"/> for sharing models across sessions.
/// </summary>
public abstract class LlamaEngineBase : LlmEngineBase
{
    protected LlamaNativeApi Native { get; }
    private LlamaModelRepository ModelRepository { get; }

    /// <summary>Initializes a llama.cpp-backed engine using the configured native library.</summary>
    protected LlamaEngineBase(LlmLocalClientSettings settings,
        string folder, string libraryName) : base(settings)
    {
        string resolved = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(folder),
            AppContext.BaseDirectory);

        this.Native = new LlamaNativeApi(
            loadLibrary: () => LazyNative.LoadFromFolder(resolved, libraryName),
            loadGgmlLibrary: () => LazyNative.LoadFromFolder(resolved, "ggml.dll"),
            nativeLibraryFolder: resolved
        );

        this.ModelRepository = new LlamaModelRepository(this.Native);

        LlmRuntimeSettings runtime = settings.Runtime;
        if (runtime.EnableNativeLogging)
        {
            Action<int, string> handler = runtime.NativeLogHandler
                ?? ((level, message) => Debug.WriteLine($"[llama:{level}] {message}"));

            LlamaNativeApi.SetLogHandler(handler);
        }
    }

    /// <summary>Creates a chat session backed by a shared loaded model.</summary>
    public override ILlmChatSession CreateSession()
    {
        LlmSessionSettings sessionOptions = this.Settings.ToSessionOptions();

        SharedModelHandle sharedModel = this.ModelRepository.AcquireModel(
            sessionOptions.ModelPath, sessionOptions.GpuLayers);

        try
        {
            return new LlamaChatSession(this.Native, sharedModel, sessionOptions);
        }
        catch
        {
            sharedModel.Dispose();
            throw;
        }
    }

    /// <summary>Creates an embedding session backed by a shared loaded model.</summary>
    public override ILlmEmbeddingSession CreateEmbeddingSession()
    {
        LlmSessionSettings sessionOptions = this.Settings.ToSessionOptions();

        SharedModelHandle sharedModel = this.ModelRepository.AcquireModel(
            sessionOptions.ModelPath, sessionOptions.GpuLayers);

        try
        {
            return new LlamaEmbeddingSession(this.Native, sharedModel, sessionOptions);
        }
        catch
        {
            sharedModel.Dispose();
            throw;
        }
    }

    /// <summary>Disposes the shared model repository and engine resources.</summary>
    public override void Dispose()
    {
        this.ModelRepository.Dispose();
        base.Dispose();
    }
}
