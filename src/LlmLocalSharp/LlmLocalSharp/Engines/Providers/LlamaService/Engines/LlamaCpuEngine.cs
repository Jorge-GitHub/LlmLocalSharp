using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Base;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Engines;

/// <summary>CPU-only llama.cpp engine.</summary>
public sealed class LlamaCpuEngine : LlamaEngineBase
{
    /// <summary>Creates a CPU-only llama.cpp engine.</summary>
    public LlamaCpuEngine(LlmLocalClientSettings settings)
        : base(settings,
            folder: settings.Libraries.Win64CpuLibrary.FolderPath,
            libraryName: settings.Libraries.Win64CpuLibrary.LibraryName)
    { }
}
