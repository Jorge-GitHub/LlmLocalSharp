using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Base;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Engines;

/// <summary>CUDA-accelerated llama.cpp engine.</summary>
public sealed class LlamaCudaEngine : LlamaEngineBase
{
    /// <summary>Creates a CUDA-accelerated llama.cpp engine.</summary>
    public LlamaCudaEngine(LlmLocalClientSettings settings)
        : base(settings,
            folder: settings.Libraries.DefaultGpuLibrary.FolderPath,
            libraryName: settings.Libraries.DefaultGpuLibrary.LibraryName)
    { }
}
