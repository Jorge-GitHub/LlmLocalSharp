using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Base;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Engines;

/// <summary>DirectML-accelerated llama.cpp engine.</summary>
public sealed class LlamaDirectMlEngine : LlamaEngineBase
{
    /// <summary>Creates a DirectML-accelerated llama.cpp engine.</summary>
    public LlamaDirectMlEngine(LlmLocalClientSettings settings)
        : base(settings,
            folder: settings.Libraries.DirectMlLibrary.FolderPath,
            libraryName: settings.Libraries.DirectMlLibrary.LibraryName) 
    { }
}
