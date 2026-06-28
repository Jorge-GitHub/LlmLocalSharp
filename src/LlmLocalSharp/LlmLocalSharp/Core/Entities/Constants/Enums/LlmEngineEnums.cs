namespace LlmLocalSharp.Core.Entities.Constants.Enums;

public enum LlmEngineType
{
    None = 0,
    Llama = 1,
    OnnxRuntime = 2,
    Vllm = 3,
    TensorRtLlm = 4,
    MlcLlm = 5,
    Ollama = 6,
    OpenVino = 7
}

public enum LlmBackend
{
    Auto = 0,
    Cpu = 1,
    Cuda = 2,
    DirectML = 3,
    Vulkan = 4
}

public enum CudaRuntimePreference
{
    Auto,
    Cuda11,
    Cuda12,
    Cuda13
}

public enum ChatMessageRole
{
    System = 0,
    User = 1,
    Assistant = 2
}

public enum ChatTemplateType
{
    None = 0,
    Llama2 = 1,
    ChatMl = 2,
    Mistral = 3
}

public enum StopReason
{
    None = 0,
    EndOfSequence = 1,
    MaxTokensReached = 2,
    StopSequenceMatched = 3
}
