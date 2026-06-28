using System.Runtime.InteropServices;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;

/// <summary>
/// Must include the <c>embd</c> field (float*) that sits between token and pos.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LlamaBatch
{
    public int n_tokens;    // int32_t
    public IntPtr token;    // llama_token*   (int32_t*)
    public IntPtr embd;     // float*         ← was missing in original code
    public IntPtr pos;      // llama_pos*     (int32_t*)
    public IntPtr n_seq_id; // int32_t*
    public IntPtr seq_id;   // llama_seq_id** (int32_t**)
    public IntPtr logits;   // int8_t*
}