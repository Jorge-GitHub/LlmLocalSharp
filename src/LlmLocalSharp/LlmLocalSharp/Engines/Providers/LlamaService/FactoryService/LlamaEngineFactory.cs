using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Engines.Providers.LlamaService.Base;
using LlmLocalSharp.Engines.Providers.LlamaService.Engines;

namespace LlmLocalSharp.Engines.Providers.LlamaService.FactoryService;

/// <summary>
/// Factory that selects the right llama.cpp engine variant
/// based on the requested backend (CPU, CUDA, Auto).
/// </summary>
internal sealed class LlamaEngineFactory
{
    /// <summary>Creates the llama.cpp engine for the requested backend.</summary>
    public LlamaEngineBase Create(LlmLocalClientSettings settings)
    {
        return settings.Runtime.Backend switch
        {
            LlmBackend.Auto => TryCreateAuto(settings),
            LlmBackend.Cpu => new LlamaCpuEngine(settings),
            LlmBackend.Cuda => new LlamaCudaEngine(settings),
            LlmBackend.DirectML => new LlamaDirectMlEngine(settings),
            LlmBackend.Vulkan => new LlamaVulkanEngine(settings),
            _ => throw new NotImplementedException(
                    $"Backend '{settings.Runtime.Backend}' is not implemented yet.")
        };
    }

    /// <summary>Attempts GPU backends first and falls back to CPU.</summary>
    private static LlamaEngineBase TryCreateAuto(LlmLocalClientSettings settings)
    {
        try
        {
            return new LlamaCudaEngine(settings);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        catch (BadImageFormatException) { }

        return new LlamaCpuEngine(settings);
    }
}
