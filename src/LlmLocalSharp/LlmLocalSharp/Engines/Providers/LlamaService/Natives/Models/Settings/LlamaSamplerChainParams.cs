using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;

[StructLayout(LayoutKind.Sequential)]
public struct LlamaSamplerChainParams
{
    [MarshalAs(UnmanagedType.I1)]
    public bool no_perf;
}
