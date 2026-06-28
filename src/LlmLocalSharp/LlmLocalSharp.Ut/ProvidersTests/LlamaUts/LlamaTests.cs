using LlmLocalSharp.Core.Clients;
using LlmLocalSharp.Core.Entities.Clients.Responses;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Ut.Helpers;

namespace LlmLocalSharp.Ut.ProvidersTests.LlamaUts;

[TestClass]
[DoNotParallelize]
public class LlamaTests
{
    private const string TestModelFile = "SmolLM2-135M-Instruct-Q2_K.gguf";

    [TestMethod]
    public async Task SendMessage_ReturnsNonEmptyResponse()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        LocalChatResponse response = await session.Chat.SendAsync("Hello, how are you?");

        Assert.IsFalse(string.IsNullOrWhiteSpace(response.Content));
        Assert.AreNotEqual(StopReason.None, response.StopReason);
        Assert.IsGreaterThan(0, response.Usage.CompletionTokens);
        Assert.IsGreaterThan(0, response.Usage.PromptTokens);
        Assert.IsGreaterThan(0, response.Usage.TotalTokens);
        Assert.IsGreaterThan(TimeSpan.Zero, response.Timings.TotalDuration);
    }

    [TestMethod]
    public async Task TwoSessions_ShareSameModel()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();

        using LlmSession sessionOne = client.CreateSession(settings);
        using LlmSession sessionTwo = client.CreateSession(settings);

        LocalChatResponse responseOne = await sessionOne.Chat.SendAsync("Say hello.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(responseOne.Content));

        // Dispose first session — shared model should still be alive for the second
        sessionOne.Dispose();

        LocalChatResponse responseTwo = await sessionTwo.Chat.SendAsync("Say goodbye.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(responseTwo.Content));
    }

    // [TestMethod]
    public async Task StreamMessage_YieldsChunks()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmSession session = new LlmLocalClient().CreateSession(settings);

        int chunkCount = 0;
        await foreach (string chunk in session.Chat.StreamAsync("Tell me a joke."))
        {
            System.Console.Write(chunk);
            chunkCount++;
        }

        Assert.IsGreaterThan(chunkCount, 0);
    }

    [TestMethod]
    public async Task MultiTurnConversation_MaintainsContext()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        LocalChatResponse responseOne = await session.Chat.SendAsync("My name is Jorge.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(responseOne.Content));

        LocalChatResponse responseTwo = await session.Chat.SendAsync("What is my name?");
        Assert.IsFalse(string.IsNullOrWhiteSpace(responseTwo.Content));

        // The model should have context from the first turn
        Assert.IsGreaterThanOrEqualTo(session.Chat.History.Messages.Count, 4,
            "History should contain at least user + assistant + user + assistant messages.");
    }

    [TestMethod]
    public async Task StreamChunksAsync_YieldsChunksWithTokenCount()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        List<ChatStreamChunk> chunks = new();

        await foreach (ChatStreamChunk chunk in session.Chat.StreamChunksAsync("Tell me a short joke."))
        {
            chunks.Add(chunk);
        }

        Assert.IsGreaterThan(0, chunks.Count, "Should yield at least one chunk.");

        // Every chunk must have non-empty text
        foreach (ChatStreamChunk chunk in chunks)
        {
            Assert.IsFalse(string.IsNullOrEmpty(chunk.Text), "Chunk text should not be empty.");
        }

        // CompletionTokens should increment across chunks
        for (int i = 1; i < chunks.Count; i++)
        {
            Assert.IsGreaterThan(chunks[i - 1].CompletionTokens, chunks[i].CompletionTokens,
                "CompletionTokens should increase with each chunk.");
        }

        // Last chunk's token count should match the total
        Assert.AreEqual(chunks.Count, chunks[^1].CompletionTokens,
            "Final CompletionTokens should equal the total number of chunks.");

        // LastResponse should be populated with complete metrics
        LocalChatResponse? lastResponse = session.Chat.LastResponse;
        Assert.IsNotNull(lastResponse);
        Assert.IsFalse(string.IsNullOrWhiteSpace(lastResponse.Content));
        Assert.AreNotEqual(StopReason.None, lastResponse.StopReason);
        Assert.AreEqual(chunks.Count, lastResponse.Usage.CompletionTokens);
        Assert.IsGreaterThan(0, lastResponse.Usage.PromptTokens);
        Assert.IsGreaterThan(TimeSpan.Zero, lastResponse.Timings.TotalDuration);
    }

    [TestMethod]
    public async Task StreamResponseAsync_YieldsAccumulatingResponses()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        List<LocalChatResponse> responses = new();

        await foreach (LocalChatResponse response in session.Chat.StreamResponseAsync("Say hello."))
        {
            responses.Add(response);
        }

        // Must have at least 2 responses: at least one partial + the final complete one
        Assert.IsGreaterThan(1, responses.Count,
            "Should yield partial responses plus a final complete response.");

        // All but the last should be partial (StopReason.None)
        for (int i = 0; i < responses.Count - 1; i++)
        {
            Assert.AreEqual(StopReason.None, responses[i].StopReason,
                $"Response at index {i} should be partial (StopReason.None).");
        }

        // Content should accumulate across partial responses
        for (int i = 1; i < responses.Count - 1; i++)
        {
            Assert.IsGreaterThanOrEqualTo(responses[i - 1].Content.Length, responses[i].Content.Length,
                "Content should accumulate (grow or stay same) across partial responses.");
        }

        // CompletionTokens should increment across partial responses
        for (int i = 1; i < responses.Count - 1; i++)
        {
            Assert.IsGreaterThan(responses[i - 1].Usage.CompletionTokens,
                responses[i].Usage.CompletionTokens,
                "CompletionTokens should increase across partial responses.");
        }

        // TotalDuration should grow across partial responses
        for (int i = 1; i < responses.Count - 1; i++)
        {
            Assert.IsGreaterThanOrEqualTo(responses[i - 1].Timings.TotalDuration,
                responses[i].Timings.TotalDuration,
                "TotalDuration should grow across partial responses.");
        }

        // The final response should have complete metrics
        LocalChatResponse finalResponse = responses[^1];
        Assert.AreNotEqual(StopReason.None, finalResponse.StopReason,
            "Final response should have a definitive StopReason.");
        Assert.IsGreaterThan(0, finalResponse.Usage.PromptTokens);
        Assert.IsGreaterThan(0, finalResponse.Usage.CompletionTokens);
        Assert.IsNotNull(finalResponse.Timings.PromptProcessingTime);
        Assert.IsNotNull(finalResponse.Timings.GenerationTime);
        Assert.IsNotNull(finalResponse.Timings.TokensPerSecond);
        Assert.IsNotNull(finalResponse.Timings.PromptTokensPerSecond);
        Assert.IsGreaterThan(TimeSpan.Zero, finalResponse.Timings.TotalDuration);
        Assert.IsFalse(string.IsNullOrWhiteSpace(finalResponse.Content));

        // Final response should match LastResponse
        Assert.AreEqual(session.Chat.LastResponse!.ChatResponseId, finalResponse.ChatResponseId,
            "Final response should be the same as LastResponse.");
    }

    [TestMethod]
    public async Task SwitchModel_PreservesHistory()
    {
        LlmLocalClientSettings primarySettings = this.GetSettings();
        LlmLocalClientSettings alternateSettings = this.GetAlternateSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(primarySettings);

        // Send a message on the first model
        LocalChatResponse firstResponse = await session.Chat.SendAsync("My name is Jorge.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(firstResponse.Content));

        int historyCountBeforeSwitch = session.Chat.History.Messages.Count;

        // Switch to the second model
        session.SwitchModel(alternateSettings);

        // History must be preserved after switching
        Assert.HasCount(historyCountBeforeSwitch, session.Chat.History.Messages,
            "History should be preserved after switching models.");

        // Send a message on the new model — should work without errors
        LocalChatResponse secondResponse = await session.Chat.SendAsync("What is my name?");
        Assert.IsFalse(string.IsNullOrWhiteSpace(secondResponse.Content));

        // History should now contain all turns from both models
        Assert.IsGreaterThanOrEqualTo(historyCountBeforeSwitch + 2, session.Chat.History.Messages.Count,
            "History should contain turns from both models.");
    }

    [TestMethod]
    public async Task StreamChunksAsync_YieldsChunksWithProbability()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        List<ChatStreamChunk> chunks = new();

        await foreach (ChatStreamChunk chunk in session.Chat.StreamChunksAsync("Tell me a short joke."))
        {
            chunks.Add(chunk);
        }

        Assert.IsGreaterThan(0, chunks.Count, "Should yield at least one chunk.");

        // Every chunk must have a probability in (0, 1]
        foreach (ChatStreamChunk chunk in chunks)
        {
            Assert.IsGreaterThan(0f, chunk.Probability,
                "Probability should be greater than zero.");
            Assert.IsLessThanOrEqualTo(1f, chunk.Probability,
                "Probability should not exceed 1.");
        }
    }

    [TestMethod]
    public async Task OnLogits_CallbackFiresWithCorrectVocabSize()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using LlmSession session = client.CreateSession(settings);

        int callbackCount = 0;
        int expectedVocabSize = session.Chat.ModelMetadata.VocabSize;

        session.Chat.OnLogits = (ReadOnlySpan<float> logits) =>
        {
            Assert.AreEqual(expectedVocabSize, logits.Length,
                "Logits span length should match the model's vocab size.");
            callbackCount++;
        };

        LocalChatResponse chat = await session.Chat.SendAsync("Say hello.");
        LocalChatResponse chat2 = await session.Chat.SendAsync("How much is 2 + 5?");

        Assert.IsGreaterThan(0, callbackCount,
            "OnLogits callback should have been invoked at least once.");
    }

    [TestMethod]
    public async Task EmbedAsync_ReturnsValidEmbeddingVector()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        using LlmLocalClient client = new LlmLocalClient();
        using EmbeddingSession session = client.CreateEmbeddingSession(settings);

        LocalEmbeddingResponse response = await session.EmbedAsync("Hello, world!");

        Assert.IsGreaterThan(0, response.Dimension,
            "Embedding dimension should be greater than zero.");
        Assert.HasCount(response.Dimension, response.Vector,
            "Vector length should match the reported dimension.");

        // At least some values should be non-zero
        bool hasNonZero = false;
        foreach (float value in response.Vector)
        {
            if (value != 0f)
            {
                hasNonZero = true;
                break;
            }
        }

        Assert.IsTrue(hasNonZero, "Embedding vector should contain non-zero values.");
    }

    private LlmLocalClientSettings GetSettings()
    {
        string modelPath = TestModelResolver.GetModelPath(TestModelFile);

        LlmLocalClientSettings settings = new LlmLocalClientSettings();

        settings.Runtime.ModelPath = modelPath;
        settings.Runtime.Backend = LlmBackend.Cpu;
        settings.Runtime.ModelName = "smollm2-135m";
        settings.Runtime.ContextSize = 2048;
        settings.Runtime.GpuLayers = 0;

        settings.Generation.ChatTemplate = ChatTemplateType.ChatMl;
        settings.Generation.Temperature = 0.7f;
        settings.Generation.TopP = 0.9f;
        settings.Generation.MaxTokens = 256;

        return settings;
    }

    private LlmLocalClientSettings GetAlternateSettings()
    {
        LlmLocalClientSettings settings = this.GetSettings();
        settings.Runtime.ModelName = "smollm2-135m-alternate";
        return settings;
    }

}
