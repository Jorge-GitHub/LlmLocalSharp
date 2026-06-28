using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Engines.Base;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Clients.Responses;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Extensions.Entities.Clients.Interfaces;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace LlmLocalSharp.Core.Clients;

/// <summary>
/// High-level chat session that wraps <see cref="ILlmChatSession"/>
/// with async streaming, thread safety, conversation history,
/// pluggable chat templates, and context overflow handling.
/// </summary>
public sealed class ChatSession : IDisposable
{
    private const double OverflowThreshold = 0.85;

    private volatile bool _disposed;
    private ILlmChatSession? _session;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _isFirstSubmission = true;
    private LogitsCallback? _onLogits;

    public LlmEngineBase Engine { get; private set; }
    public ChatHistory History { get; }
    public IChatTemplate Template { get; private set; }
    public LocalChatResponse? LastResponse { get; private set; }

    /// <summary>
    /// Optional callback that fires with the raw logits array before each sampling step.
    /// Can be set before or after the native session is created.
    /// </summary>
    public LogitsCallback? OnLogits
    {
        get
        {
            this._gate.Wait();
            try
            {
                return this._session?.OnLogits;
            }
            finally
            {
                this._gate.Release();
            }
        }
        set
        {
            this._gate.Wait();
            try
            {
                this._onLogits = value;
                if (this._session is not null)
                {
                    this._session.OnLogits = value;
                }
            }
            finally
            {
                this._gate.Release();
            }
        }
    }

    public LlmModelMetadata ModelMetadata
    {
        get
        {
            this._gate.Wait();
            try
            {
                this.EnsureCreated();

                return this._session!.ModelMetadata;
            }
            finally
            {
                this._gate.Release();
            }
        }
    }

    /// <summary>Creates a high-level chat session for the given engine and template.</summary>
    public ChatSession(LlmEngineBase engine, IChatTemplate template, string? systemPrompt = null)
    {
        this.Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.Template = template ?? throw new ArgumentNullException(nameof(template));
        this.History = new ChatHistory();

        if (systemPrompt is not null)
        {
            this.History.AddSystemMessage(systemPrompt);
        }
    }

    /// <summary>
    /// Stream assistant tokens one-by-one as raw strings.
    /// Lightweight — no per-token allocations beyond the string itself.
    /// Full metrics are available via <see cref="LastResponse"/> after the stream completes.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        await foreach ((string text, int _, float _) in this.StreamCoreAsync(userMessage, cancelToken))
        {
            yield return text;
        }
    }

    /// <summary>
    /// Stream assistant tokens as <see cref="ChatStreamChunk"/> objects,
    /// which include the token text and a running completion-token count.
    /// Full metrics are available via <see cref="LastResponse"/> after the stream completes.
    /// </summary>
    public async IAsyncEnumerable<ChatStreamChunk> StreamChunksAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        await foreach ((string text, int completionTokens, float probability) in this.StreamCoreAsync(userMessage, cancelToken))
        {
            yield return new ChatStreamChunk
            {
                Text = text,
                CompletionTokens = completionTokens,
                Probability = probability
            };
        }
    }

    /// <summary>
    /// Stream assistant tokens as accumulating <see cref="LocalChatResponse"/> objects.
    /// Each yielded response contains the full text generated so far, with partial metrics.
    /// The final response (where <see cref="ChatResponseBase.StopReason"/> is not <see cref="StopReason.None"/>)
    /// contains complete timing, usage, and context data.
    /// </summary>
    public async IAsyncEnumerable<LocalChatResponse> StreamResponseAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        StringBuilder accumulated = new();
        Stopwatch elapsed = Stopwatch.StartNew();

        await foreach ((string text, int completionTokens, float _) in this.StreamCoreAsync(userMessage, cancelToken))
        {
            accumulated.Append(text);

            yield return new LocalChatResponse
            {
                Content = accumulated.ToString(),
                StopReason = StopReason.None,
                Usage = new TokenUsage
                {
                    CompletionTokens = completionTokens
                },
                Timings = new LlmTimings
                {
                    TotalDuration = elapsed.Elapsed
                }
            };
        }

        // Final response with complete metrics
        if (this.LastResponse is not null)
        {
            yield return this.LastResponse;
        }
    }

    /// <summary>
    /// Core streaming implementation shared by all public streaming methods.
    /// Handles gate, buffers, prompt submission, token generation, history, and <see cref="LastResponse"/>.
    /// </summary>
    private async IAsyncEnumerable<(string Text, int CompletionTokens, float Probability)> StreamCoreAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        await this._gate.WaitAsync(cancelToken);

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        byte[] byteBuf = ArrayPool<byte>.Shared.Rent(4096);
        char[] charBuf = ArrayPool<char>.Shared.Rent(4096);
        Decoder decoder = Encoding.UTF8.GetDecoder();
        StringBuilder assistantResponse = new();
        int completionTokens = 0;

        try
        {
            this.EnsureCreated();
            this.History.AddUserMessage(userMessage);

            // ── Prompt submission with timing ──
            Stopwatch promptStopwatch = Stopwatch.StartNew();
            bool overflowHandled = this.HandleOverflowIfNeeded();

            if (!overflowHandled)
            {
                string formattedTurn = this.FormatCurrentTurn(userMessage);
                this._session!.SubmitPrompt(formattedTurn, this._isFirstSubmission);
                this._isFirstSubmission = false;
            }

            promptStopwatch.Stop();

            // ── Generation loop with timing ──
            Stopwatch generationStopwatch = Stopwatch.StartNew();

            while (!cancelToken.IsCancellationRequested)
            {
                if (!this._session!.NextToken(
                    byteBuf, out int byteCount) || byteCount <= 0)
                {
                    break;
                }

                completionTokens++;

                decoder.Convert(
                    byteBuf, 0, byteCount,
                    charBuf, 0, charBuf.Length,
                    flush: false,
                    out _, out int charsUsed, out _);

                if (charsUsed > 0)
                {
                    string chunk = new string(charBuf, 0, charsUsed);
                    float probability = this._session!.LastTokenProbability;
                    assistantResponse.Append(chunk);
                    yield return (chunk, completionTokens, probability);
                }

                await Task.Yield();
            }

            generationStopwatch.Stop();
            totalStopwatch.Stop();

            // Record the assistant response in history
            string fullResponse = assistantResponse.ToString().Trim();
            if (fullResponse.Length > 0)
            {
                this.History.AddAssistantMessage(fullResponse);
            }

            // Capture metrics for callers to access via LastResponse
            this.LastResponse = this._session!.ToChatResponse(
                fullResponse, completionTokens,
                totalStopwatch, promptStopwatch,
                generationStopwatch);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
            this._gate.Release();
        }
    }

    /// <summary>Send a message and collect the full response with metadata.</summary>
    public async Task<LocalChatResponse> SendAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        await this._gate.WaitAsync(cancellationToken);

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        byte[] byteBuf = ArrayPool<byte>.Shared.Rent(4096);
        char[] charBuf = ArrayPool<char>.Shared.Rent(4096);
        Decoder decoder = Encoding.UTF8.GetDecoder();
        StringBuilder assistantResponse = new();
        int completionTokens = 0;

        try
        {
            this.EnsureCreated();
            this.History.AddUserMessage(userMessage);

            // ── Prompt submission with timing ──
            Stopwatch promptStopwatch = Stopwatch.StartNew();
            bool overflowHandled = this.HandleOverflowIfNeeded();

            if (!overflowHandled)
            {
                string formattedTurn = this.FormatCurrentTurn(userMessage);
                this._session!.SubmitPrompt(formattedTurn, this._isFirstSubmission);
                this._isFirstSubmission = false;
            }

            promptStopwatch.Stop();
            int promptTokenCount = this._session!.PromptTokenCount;

            // ── Generation loop with timing ──
            Stopwatch generationStopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!this._session!.NextToken(byteBuf, out int byteCount) || byteCount <= 0)
                {
                    break;
                }

                completionTokens++;

                decoder.Convert(
                    byteBuf, 0, byteCount,
                    charBuf, 0, charBuf.Length,
                    flush: false,
                    out _, out int charsUsed, out _);

                if (charsUsed > 0)
                {
                    string chunk = new string(charBuf, 0, charsUsed);
                    assistantResponse.Append(chunk);
                }

                await Task.Yield();
            }

            generationStopwatch.Stop();
            totalStopwatch.Stop();

            // Record the assistant response in history
            string fullResponse = assistantResponse.ToString().Trim();
            if (fullResponse.Length > 0)
            {
                this.History.AddAssistantMessage(fullResponse);
            }

            LocalChatResponse response = this._session.ToChatResponse(
                fullResponse, completionTokens,
                totalStopwatch, promptStopwatch,
                generationStopwatch);

            this.LastResponse = response;

            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
            this._gate.Release();
        }
    }

    /// <summary>
    /// Stream assistant tokens one-by-one as raw strings from multi-modal content parts.
    /// Non-text parts are silently ignored by the template.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessageContentPart> contentParts,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        await foreach ((string text, int _, float _) in this.StreamCoreAsync(contentParts, cancelToken))
        {
            yield return text;
        }
    }

    /// <summary>
    /// Stream assistant tokens as <see cref="ChatStreamChunk"/> objects from multi-modal content parts.
    /// Non-text parts are silently ignored by the template.
    /// </summary>
    public async IAsyncEnumerable<ChatStreamChunk> StreamChunksAsync(
        IReadOnlyList<ChatMessageContentPart> contentParts,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        await foreach ((string text, int completionTokens, float probability) in this.StreamCoreAsync(contentParts, cancelToken))
        {
            yield return new ChatStreamChunk
            {
                Text = text,
                CompletionTokens = completionTokens,
                Probability = probability
            };
        }
    }

    /// <summary>
    /// Stream assistant tokens as accumulating <see cref="LocalChatResponse"/> objects from multi-modal content parts.
    /// Non-text parts are silently ignored by the template.
    /// </summary>
    public async IAsyncEnumerable<LocalChatResponse> StreamResponseAsync(
        IReadOnlyList<ChatMessageContentPart> contentParts,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        StringBuilder accumulated = new();
        Stopwatch elapsed = Stopwatch.StartNew();

        await foreach ((string text, int completionTokens, float _) in this.StreamCoreAsync(contentParts, cancelToken))
        {
            accumulated.Append(text);

            yield return new LocalChatResponse
            {
                Content = accumulated.ToString(),
                StopReason = StopReason.None,
                Usage = new TokenUsage
                {
                    CompletionTokens = completionTokens
                },
                Timings = new LlmTimings
                {
                    TotalDuration = elapsed.Elapsed
                }
            };
        }

        if (this.LastResponse is not null)
        {
            yield return this.LastResponse;
        }
    }

    /// <summary>Send multi-modal content parts and collect the full response with metadata.</summary>
    public async Task<LocalChatResponse> SendAsync(
        IReadOnlyList<ChatMessageContentPart> contentParts,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        await this._gate.WaitAsync(cancellationToken);

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        byte[] byteBuf = ArrayPool<byte>.Shared.Rent(4096);
        char[] charBuf = ArrayPool<char>.Shared.Rent(4096);
        Decoder decoder = Encoding.UTF8.GetDecoder();
        StringBuilder assistantResponse = new();
        int completionTokens = 0;

        try
        {
            this.EnsureCreated();
            this.History.AddUserMessage(contentParts);

            Stopwatch promptStopwatch = Stopwatch.StartNew();
            bool overflowHandled = this.HandleOverflowIfNeeded();

            if (!overflowHandled)
            {
                string formattedTurn = this.FormatCurrentTurn(contentParts);
                this._session!.SubmitPrompt(formattedTurn, this._isFirstSubmission);
                this._isFirstSubmission = false;
            }

            promptStopwatch.Stop();
            int promptTokenCount = this._session!.PromptTokenCount;

            Stopwatch generationStopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!this._session!.NextToken(byteBuf, out int byteCount) || byteCount <= 0)
                {
                    break;
                }

                completionTokens++;

                decoder.Convert(
                    byteBuf, 0, byteCount,
                    charBuf, 0, charBuf.Length,
                    flush: false,
                    out _, out int charsUsed, out _);

                if (charsUsed > 0)
                {
                    string chunk = new string(charBuf, 0, charsUsed);
                    assistantResponse.Append(chunk);
                }

                await Task.Yield();
            }

            generationStopwatch.Stop();
            totalStopwatch.Stop();

            string fullResponse = assistantResponse.ToString().Trim();
            if (fullResponse.Length > 0)
            {
                this.History.AddAssistantMessage(fullResponse);
            }

            LocalChatResponse response = this._session.ToChatResponse(
                fullResponse, completionTokens,
                totalStopwatch, promptStopwatch,
                generationStopwatch);

            this.LastResponse = response;

            return response;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
            this._gate.Release();
        }
    }

    /// <summary>
    /// Core streaming implementation for multi-modal content parts.
    /// </summary>
    private async IAsyncEnumerable<(string Text, int CompletionTokens, float Probability)> StreamCoreAsync(
        IReadOnlyList<ChatMessageContentPart> contentParts,
        [EnumeratorCancellation] CancellationToken cancelToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        await this._gate.WaitAsync(cancelToken);

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        byte[] byteBuf = ArrayPool<byte>.Shared.Rent(4096);
        char[] charBuf = ArrayPool<char>.Shared.Rent(4096);
        Decoder decoder = Encoding.UTF8.GetDecoder();
        StringBuilder assistantResponse = new();
        int completionTokens = 0;

        try
        {
            this.EnsureCreated();
            this.History.AddUserMessage(contentParts);

            Stopwatch promptStopwatch = Stopwatch.StartNew();
            bool overflowHandled = this.HandleOverflowIfNeeded();

            if (!overflowHandled)
            {
                string formattedTurn = this.FormatCurrentTurn(contentParts);
                this._session!.SubmitPrompt(formattedTurn, this._isFirstSubmission);
                this._isFirstSubmission = false;
            }

            promptStopwatch.Stop();

            Stopwatch generationStopwatch = Stopwatch.StartNew();

            while (!cancelToken.IsCancellationRequested)
            {
                if (!this._session!.NextToken(
                    byteBuf, out int byteCount) || byteCount <= 0)
                {
                    break;
                }

                completionTokens++;

                decoder.Convert(
                    byteBuf, 0, byteCount,
                    charBuf, 0, charBuf.Length,
                    flush: false,
                    out _, out int charsUsed, out _);

                if (charsUsed > 0)
                {
                    string chunk = new string(charBuf, 0, charsUsed);
                    float probability = this._session!.LastTokenProbability;
                    assistantResponse.Append(chunk);
                    yield return (chunk, completionTokens, probability);
                }

                await Task.Yield();
            }

            generationStopwatch.Stop();
            totalStopwatch.Stop();

            string fullResponse = assistantResponse.ToString().Trim();
            if (fullResponse.Length > 0)
            {
                this.History.AddAssistantMessage(fullResponse);
            }

            this.LastResponse = this._session!.ToChatResponse(
                fullResponse, completionTokens,
                totalStopwatch, promptStopwatch,
                generationStopwatch);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuf);
            ArrayPool<char>.Shared.Return(charBuf);
            this._gate.Release();
        }
    }

    /// <summary>
    /// Swaps the engine and template while preserving conversation history.
    /// The old native session is disposed, and the next call to
    /// <see cref="SendAsync"/> or <see cref="StreamAsync"/> will create
    /// a new native context from the new engine and re-submit the full history.
    /// </summary>
    internal void SwitchEngine(LlmEngineBase newEngine, IChatTemplate newTemplate)
    {
        this._gate.Wait();

        try
        {
            this._session?.Dispose();
            this._session = null;
            this.Engine = newEngine;
            this.Template = newTemplate;
            this._isFirstSubmission = true;
            this.LastResponse = null;
        }
        finally
        {
            this._gate.Release();
        }
    }

    /// <summary>Disposes the native chat session if it has been created.</summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._gate.Wait();

        try
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            this._session?.Dispose();
            this._session = null;
        }
        finally
        {
            this._gate.Release();
            this._gate.Dispose();
        }
    }

    /// <summary>
    /// Checks if the context is nearing capacity. If so, trims oldest turns,
    /// resets the native session, and re-submits the full remaining history.
    /// </summary>
    /// <returns><c>true</c> if overflow was handled and the full prompt was already submitted.</returns>
    private bool HandleOverflowIfNeeded()
    {
        int contextPosition = this._session!.ContextPosition;
        int contextSize = this._session.ContextSize;

        if (contextSize <= 0 || (double)contextPosition / contextSize < OverflowThreshold)
        {
            return false;
        }

        // Trim half the user turns to free up context space
        int turnsToRemove = Math.Max(1, this.History.TurnCount / 2);
        this.History.TrimOldestTurns(turnsToRemove);

        // Reset native context and re-submit full remaining history
        this._session.Reset();
        this._isFirstSubmission = true;

        string fullPrompt = this.Template.FormatPrompt(this.History.Messages);
        this._session.SubmitPrompt(fullPrompt, this._isFirstSubmission);
        this._isFirstSubmission = false;

        return true;
    }

    /// <summary>Formats the current text-only turn according to first-turn state.</summary>
    private string FormatCurrentTurn(string userMessage)
    {
        if (this._isFirstSubmission)
        {
            return this.Template.FormatPrompt(this.History.Messages);
        }

        return this.Template.FormatUserTurn(userMessage);
    }

    /// <summary>Formats the current multi-modal turn according to first-turn state.</summary>
    private string FormatCurrentTurn(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        if (this._isFirstSubmission)
        {
            return this.Template.FormatPrompt(this.History.Messages);
        }

        return this.Template.FormatUserTurn(contentParts);
    }

    /// <summary>Creates the native chat session on first use and applies callbacks.</summary>
    private void EnsureCreated()
    {
        if (this._session is null)
        {
            this._session = this.Engine.CreateSession();
            this._session.OnLogits = this._onLogits;
        }
    }
}
