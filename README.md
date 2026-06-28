# LlmLocalSharp

Local LLM runtime for .NET.

LlmLocalSharp is a lightweight .NET wrapper around local llama.cpp-compatible
models. It lets a .NET app load a local GGUF model, create chat sessions,
stream responses, produce embeddings, and switch models without calling a
remote API.

## Requirements

- .NET 10 SDK or newer.
- A local GGUF model file.
- Git LFS, if cloning this repository with native binaries.

```powershell
git lfs install
git lfs pull
```

## Install

When consuming from source, add a project reference:

```powershell
dotnet add <your-project>.csproj reference .\src\LlmLocalSharp\LlmLocalSharp\LlmLocalSharp.csproj
```

When the package is published, use:

```powershell
dotnet add package LlmLocalSharp
```

## Basic Chat

```csharp
using LlmLocalSharp.Core.Clients;
using LlmLocalSharp.Core.Entities.Clients.Responses;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;

LlmLocalClientSettings settings = new()
{
    Runtime =
    {
        ModelPath = @"C:\Models\SmolLM2-135M-Instruct-Q2_K.gguf",
        ModelName = "smollm2-135m",
        Backend = LlmBackend.Cpu,
        ContextSize = 2048,
        GpuLayers = 0,
    },
    Generation =
    {
        ChatTemplate = ChatTemplateType.ChatMl,
        SystemPrompt = "You are a concise assistant.",
        MaxTokens = 256,
        Temperature = 0.7f,
        TopP = 0.9f,
    }
};

using LlmLocalClient client = new();
using LlmSession session = client.CreateSession(settings);

LocalChatResponse response = await session.Chat.SendAsync("Write a one-sentence haiku about C#.");

Console.WriteLine(response.Content);
Console.WriteLine($"Stop reason: {response.StopReason}");
Console.WriteLine($"Tokens: {response.Usage.TotalTokens}");
```

## Streaming Chat

Use `StreamAsync` when you only need generated text:

```csharp
await foreach (string text in session.Chat.StreamAsync("Explain Span<T> in simple terms."))
{
    Console.Write(text);
}
```

Use `StreamChunksAsync` when you also want per-token metadata:

```csharp
await foreach (ChatStreamChunk chunk in session.Chat.StreamChunksAsync("Count from one to five."))
{
    Console.Write(chunk.Text);
    Console.WriteLine($" p={chunk.Probability}");
}
```

## Embeddings

```csharp
using EmbeddingSession embeddings = client.CreateEmbeddingSession(settings);

LocalEmbeddingResponse embedding = await embeddings.EmbedAsync("local inference in .NET");

Console.WriteLine($"Dimensions: {embedding.Dimension}");
Console.WriteLine($"First value: {embedding.Vector[0]}");
```

## Switching Models

`LlmSession.SwitchModel` changes the model used by the session while preserving
chat history.

```csharp
LlmLocalClientSettings otherSettings = new()
{
    Runtime =
    {
        ModelPath = @"C:\Models\OtherModel.gguf",
        ModelName = "other-model",
        Backend = LlmBackend.Cpu,
    },
    Generation =
    {
        ChatTemplate = ChatTemplateType.Mistral,
        MaxTokens = 256,
    }
};

session.SwitchModel(otherSettings);

LocalChatResponse next = await session.Chat.SendAsync("Continue the conversation with the new model.");
Console.WriteLine(next.Content);
```

## Configuration Notes

- `Runtime.ModelPath` must point to a local GGUF model file.
- `Runtime.ModelName` is used as an engine cache key. Use a stable, unique name
  for each model you load.
- `Runtime.Backend` supports `Cpu`, `Cuda`, `DirectML`, `Vulkan`, and `Auto`.
- `Runtime.GpuLayers` controls how many model layers are offloaded to the GPU
  for supported GPU backends.
- `Generation.ChatTemplate` should match the model family. Available templates
  are `Llama2`, `ChatMl`, and `Mistral`.
- `Generation.StopSequences`, `MaxTokens`, `Temperature`, `TopP`, `TopK`,
  `MinP`, and penalty settings control sampling behavior.

## Native Libraries

The default native library folders are configured in `LibrariesSettings`.
Relative paths are resolved from the application base directory, so the native
DLL folders need to be copied next to the application output. The project file
already marks the bundled native libraries as content with
`CopyToOutputDirectory=PreserveNewest`.

For custom native builds, override the library settings:

```csharp
settings.Libraries.Win64CpuLibrary.FolderPath = @"C:\llama-native\cpu";
settings.Libraries.Win64CpuLibrary.LibraryName = "llama.dll";
```

## Running Tests

Integration tests expect test models under a `models` folder at the repository
root. Missing model files mark those tests inconclusive.

```powershell
dotnet test .\src\LlmLocalSharp\LlmLocalSharp.slnx
```
