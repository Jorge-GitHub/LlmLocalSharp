using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Extensions.Natives;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives.Models.Settings;
using System.Runtime.InteropServices;
using System.Text;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Clients;

/// <summary>
/// A single chat session backed by llama.cpp native inference.
/// Receives a shared model handle from the engine — does not load its own model.
/// </summary>
internal sealed class LlamaChatSession : ILlmChatSession
{
    private readonly LlamaNativeApi _native;
    private readonly SharedModelHandle _sharedModel;
    private readonly LlmSafeHandle _context;
    private readonly LlmSafeHandle _sampler;
    private readonly LlamaChatSessionStatus _status = new();
    private readonly int _maxTokens;
    private readonly int _contextSize;
    private readonly IReadOnlyList<string> _stopSequences;
    private readonly int _maxStopSequenceLength;

    public int ContextPosition => this._status.Position;
    public int ContextSize => this._contextSize;
    public int PromptTokenCount => this._status.PromptTokenCount;
    public StopReason StopReason => this._status.StopReason;
    public LlmModelMetadata ModelMetadata => this._sharedModel.Metadata;
    public float LastTokenProbability => this._status.LastTokenProbability;
    public LogitsCallback? OnLogits { get; set; }

    /// <summary>Creates a chat session over a shared llama.cpp model.</summary>
    public LlamaChatSession(
        LlamaNativeApi native,
        SharedModelHandle sharedModel,
        LlmSessionSettings options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        this._native = native ?? throw new ArgumentNullException(nameof(native));
        this._sharedModel = sharedModel ?? throw new ArgumentNullException(nameof(sharedModel));
        this._maxTokens = options.MaxTokens > 0 ? options.MaxTokens : 512;
        this._contextSize = options.ContextSize;
        this._stopSequences = options.StopSequences ?? Array.Empty<string>();
        this._maxStopSequenceLength = this.GetMaxStopSequenceLength();
        this._context = this.CreateContext(options);

        try
        {
            // Order: penalties → top-K → top-P → min-P → temperature → dist
            this._sampler = this._native.SamplerChainInit(
                this._native.SamplerChainDefaultParams());

            this.InitChatSession(options);
        }
        catch
        {
            this._context.Dispose();
            throw;
        }
    }

    /// <summary>Submits a formatted prompt into the native context.</summary>
    public void SubmitPrompt(string formattedPrompt, bool isFirstSubmission)
    {
        if (formattedPrompt is null)
        {
            throw new ArgumentNullException(nameof(formattedPrompt));
        }
        this._status.ResetGenerationStateForANewTurn();
        this.SubmitPromptToEngine(formattedPrompt, isFirstSubmission);
    }

    /// <summary>Clears native memory and resets session status.</summary>
    public void Reset()
    {
        this._native.MemoryClear(this._context.DangerousGetHandle());
        this._status.Reset();
    }

    /// <summary>Generates the next token and writes its UTF-8 bytes.</summary>
    public bool NextToken(Span<byte> utf8, out int bytesWritten)
    {
        bytesWritten = 0;

        if (this._status.Done)
        {
            return false;
        }

        if (this._status.GeneratedCount >= this._maxTokens)
        {
            this._status.SetMaxTokensReached();

            return false;
        }

        if (!this._status.HasPending)
        {
            if (!this.TryNextToken())
            {
                return false;
            }
        }

        return this.TryConvertTokenToUTF8(utf8, out bytesWritten);
    }

    /// <summary>Disposes the sampler, context, and shared model reference.</summary>
    public void Dispose()
    {
        this._sampler.Dispose();
        this._context.Dispose();
        this._sharedModel.Dispose();
    }

    /// <summary>Feeds a sampled token back into the native context for the next prediction.</summary>
    private void FeedTokenBackForNextPrediction(int token)
    {
        unsafe
        {
            int tokenValue = token;
            IntPtr tokenPointer = (IntPtr)(&tokenValue);

            int returnCode = this.GetLlamaBatchCode(tokenPointer,
                numberOfTokens: 1);

            if (returnCode != 0)
            {
                throw new InvalidOperationException(
                    $"Decode failed with rc={returnCode}.");
            }
        }

        this._status.Position += 1;
    }

    /// <summary>Decodes a llama batch and returns the native status code.</summary>
    private int GetLlamaBatchCode(IntPtr inputToken,
        int numberOfTokens)
    {
        LlamaBatch batch = this._native.BatchGetOne(
            inputToken, numberOfTokens, this._status.Position,
            seqId: 0);

        return this._native.Decode(
            this._context.DangerousGetHandle(), batch);
    }

    /// <summary>Samples the next token and stores it as pending output.</summary>
    private bool TryNextToken()
    {
        int token = this.GetLogitsAndInvokeCallbackBeforeSampling();

        if (this._native.TokenIsEog(
            this._sharedModel.VocabularyPointer, token))
        {
            this._status.SetEndSequence();

            return false;
        }

        this._status.PendingToken = token;
        this._status.HasPending = true;
        this.FeedTokenBackForNextPrediction(token);

        return true;
    }

    /// <summary>Reads logits, invokes the optional callback, and samples a token.</summary>
    private int GetLogitsAndInvokeCallbackBeforeSampling()
    {
        IntPtr logitsPtr = this._native.GetLogits(
            this._context.DangerousGetHandle());
        int vocabSize = this._sharedModel.Metadata.VocabSize;
        ReadOnlySpan<float> logits;

        unsafe
        {
            logits = new ReadOnlySpan<float>((float*)logitsPtr.ToPointer(), vocabSize);
        }

        this.OnLogits?.Invoke(logits);

        return this.SampleTokenAndComputeProbability(logits);
    }

    /// <summary>Samples a token and computes its probability from the logits.</summary>
    private int SampleTokenAndComputeProbability(ReadOnlySpan<float> logits)
    {
        int token = this._native.SamplerSample(
            this._sampler.DangerousGetHandle(),
            this._context.DangerousGetHandle(), idx: -1);

        this._native.SamplerAccept(
            this._sampler.DangerousGetHandle(), token);

        this._status.ComputeProbabilityViaLogSumExp(logits, token);

        return token;
    }

    /// <summary>Converts the pending token to UTF-8 bytes when it has display text.</summary>
    private bool TryConvertTokenToUTF8(Span<byte> utf8,
        out int bytesWritten)
    {
        bytesWritten = 0;
        int number = this.GetTokenToPieceNumber(utf8);
        if (number <= 0)
        {
            this._status.SetTokenWithNoText();

            return true;
        }

        bytesWritten = number;

        return this.ConvertTokenToUTF8(utf8, number);
    }

    /// <summary>Finalizes token conversion and evaluates stop sequences.</summary>
    private bool ConvertTokenToUTF8(Span<byte> utf8,
        int tokenToPieceNumber)
    {
        this._status.HasPending = false;
        this._status.GeneratedCount++;

        // ── Stop-sequence check ──
        if (this._stopSequences.Count > 0)
        {
            Span<char> charBuffer = stackalloc char[tokenToPieceNumber];
            int charCount = Encoding.UTF8.GetChars(
                utf8.Slice(0, tokenToPieceNumber), charBuffer);
            this._status.AppendToRecentOutput(charBuffer.Slice(0, charCount));

            if (!this._status.KeepSequences(this._stopSequences,
                this._maxStopSequenceLength))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Converts the pending token into a llama.cpp piece.</summary>
    private int GetTokenToPieceNumber(Span<byte> utf8)
    {
        int number;

        unsafe
        {
            fixed (byte* pointer = utf8)
            {
                number = this._native.TokenToPiece(
                    this._sharedModel.VocabularyPointer,
                    this._status.PendingToken,
                    (IntPtr)pointer,
                    utf8.Length,
                    lstrip: 0,
                    special: false);
            }
        }

        return number;
    }

    /// <summary>Tokenizes and submits a formatted prompt to llama.cpp.</summary>
    private void SubmitPromptToEngine(string formattedPrompt, bool isFirstSubmission)
    {
        int maxBufTokens = Math.Max(512, formattedPrompt.Length * 2);
        IntPtr tokenBuffer = Marshal.AllocHGlobal(sizeof(int) * maxBufTokens);
        try
        {
            int tokenCount = this._native.Tokenize(
                this._sharedModel.VocabularyPointer,
                formattedPrompt,
                tokenBuffer,
                maxBufTokens,
                addSpecial: isFirstSubmission,
                parseSpecial: true);

            if (tokenCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Tokenize returned {tokenCount}. Prompt may be too long or invalid.");
            }

            this.SubmitTokenToEngine(tokenBuffer, tokenCount);
        }
        finally
        {
            Marshal.FreeHGlobal(tokenBuffer);
        }
    }

    /// <summary>Decodes prompt tokens into the native context.</summary>
    private void SubmitTokenToEngine(IntPtr tokenBuffer,
        int tokenCount)
    {
        this._status.PromptTokenCount = tokenCount;

        int returnCode = this.GetLlamaBatchCode(tokenBuffer,
            numberOfTokens: tokenCount);

        if (returnCode != 0)
        {
            throw new InvalidOperationException(
                $"Decode failed with rc={returnCode}.");
        }

        this._status.Position += tokenCount;
    }

    /// <summary>Gets the longest configured stop sequence length.</summary>
    private int GetMaxStopSequenceLength()
    {
        int maxLength = 0;

        foreach (string sequence in this._stopSequences)
        {
            if (sequence.Length > maxLength)
            {
                maxLength = sequence.Length;
            }
        }

        return maxLength;
    }

    /// <summary>Creates a llama.cpp context for chat generation.</summary>
    private LlmSafeHandle CreateContext(LlmSessionSettings options)
    {
        LlamaContextParams defaultContext =
            this._native.ContextDefaultParams();
        defaultContext.n_ctx = (uint)options.ContextSize;
        defaultContext.n_threads = options.Threads;
        defaultContext.n_threads_batch = options.Threads;

        return this._native.CreateContext(
            this._sharedModel.ModelPointer, defaultContext);
    }

    /// <summary>Initializes sampler configuration and default status values.</summary>
    private void InitChatSession(LlmSessionSettings options)
    {
        this._native.InitChatSession(options, this._sampler);

        this._status.LoadDefaultValues();
    }
}
