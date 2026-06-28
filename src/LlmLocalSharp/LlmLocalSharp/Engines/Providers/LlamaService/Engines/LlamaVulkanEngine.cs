using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Base;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Engines;

/// <summary>Vulkan-accelerated llama.cpp engine.</summary>
public sealed class LlamaVulkanEngine : LlamaEngineBase
{
    /// <summary>Creates a Vulkan-accelerated llama.cpp engine.</summary>
    public LlamaVulkanEngine(LlmLocalClientSettings settings)
        : base(settings,
            folder: settings.Libraries.VulkanLibrary.FolderPath,
            libraryName: settings.Libraries.VulkanLibrary.LibraryName)
    { }
}
