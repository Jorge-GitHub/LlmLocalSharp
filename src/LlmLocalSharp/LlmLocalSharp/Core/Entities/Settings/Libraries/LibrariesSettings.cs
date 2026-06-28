namespace LlmLocalSharp.Core.Entities.Settings.Libraries;

public sealed class LibrariesSettings
{
    public LlmLibrarySettings Win64CpuLibrary { get; set; } = new()
    {
        FolderPath = @"Engines\Providers\LlamaService\Libraries\Native\Win-x64\Cpu",
        LibraryName = "llama.dll",
    };

    public LlmLibrarySettings DefaultGpuLibrary { get; set; } = new()
    {
        FolderPath = @"Engines\Providers\LlamaService\Libraries\Native\Win-x64\Cuda",
        LibraryName = "llama_cuda.dll",
    };

    public LlmLibrarySettings DirectMlLibrary { get; set; } = new()
    {
        FolderPath = @"Engines\Providers\LlamaService\Libraries\Native\Win-x64\DirectML",
        LibraryName = "llama_directml.dll",
    };

    public LlmLibrarySettings VulkanLibrary { get; set; } = new()
    {
        FolderPath = @"Engines\Providers\LlamaService\Libraries\Native\Win-x64\Vulkan",
        LibraryName = "llama_vulkan.dll",
    };
}