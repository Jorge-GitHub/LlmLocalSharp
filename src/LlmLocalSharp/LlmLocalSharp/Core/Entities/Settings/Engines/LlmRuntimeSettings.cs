using LlmLocalSharp.Core.Entities.Constants.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LlmLocalSharp.Core.Entities.Settings.Engines;

// ──────────────────────────────────────────────
// Runtime (model loading, backend)
// ──────────────────────────────────────────────

public sealed class LlmRuntimeSettings
{
    public LlmBackend Backend { get; set; } = LlmBackend.Auto;
    public LlmEngineType Engine { get; set; } = LlmEngineType.Llama;
    public string ModelPath { get; set; } = "";
    public string ModelName { get; set; } = "";
    public int ContextSize { get; set; } = 4096;
    public int Threads { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
    public int GpuLayers { get; set; } = 0;
    public CudaRuntimePreference CudaRuntime { get; set; } = CudaRuntimePreference.Auto;
    public bool EnableNativeLogging { get; set; } = false;
    public Action<int, string>? NativeLogHandler { get; set; }
}