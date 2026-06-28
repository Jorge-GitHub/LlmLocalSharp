using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Clients.Templates;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Ut.ProvidersTests.LlamaUts;

[TestClass]
public class ChatTemplateTests
{
    // ──────────── Llama2 ────────────

    [TestMethod]
    public void Llama2_FormatUserTurn_ProducesCorrectFormat()
    {
        Llama2ChatTemplate template = new();

        string result = template.FormatUserTurn("Hello");

        Assert.AreEqual("[INST] Hello [/INST]\n", result);
    }

    [TestMethod]
    public void Llama2_FormatPrompt_WithSystemAndUserAndAssistant()
    {
        Llama2ChatTemplate template = new();
        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatMessageRole.System, "You are helpful."),
            new ChatMessage(ChatMessageRole.User, "Hello"),
            new ChatMessage(ChatMessageRole.Assistant, "Hi there!"),
            new ChatMessage(ChatMessageRole.User, "How are you?")
        };

        string result = template.FormatPrompt(messages);

        Assert.Contains("<<SYS>>", result);
        Assert.Contains("You are helpful.", result);
        Assert.Contains("<</SYS>>", result);
        Assert.Contains("Hello [/INST]", result);
        Assert.Contains("Hi there! </s>", result);
        Assert.Contains("[INST] How are you? [/INST]", result);
    }

    [TestMethod]
    public void Llama2_FormatPrompt_WithoutSystem_NoSysTags()
    {
        Llama2ChatTemplate template = new();
        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatMessageRole.User, "Hello")
        };

        string result = template.FormatPrompt(messages);

        Assert.DoesNotContain("<<SYS>>", result);
        Assert.AreEqual("[INST] Hello [/INST]\n", result);
    }

    // ──────────── ChatML ────────────

    [TestMethod]
    public void ChatMl_FormatUserTurn_ProducesCorrectFormat()
    {
        ChatMlTemplate template = new();

        string result = template.FormatUserTurn("Hello");

        Assert.AreEqual("<|im_start|>user\nHello<|im_end|>\n<|im_start|>assistant\n", result);
    }

    [TestMethod]
    public void ChatMl_FormatPrompt_WithAllRoles()
    {
        ChatMlTemplate template = new();
        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatMessageRole.System, "Be concise."),
            new ChatMessage(ChatMessageRole.User, "Hi"),
            new ChatMessage(ChatMessageRole.Assistant, "Hello!")
        };

        string result = template.FormatPrompt(messages);

        Assert.Contains("<|im_start|>system\nBe concise.<|im_end|>", result);
        Assert.Contains("<|im_start|>user\nHi<|im_end|>", result);
        Assert.Contains("<|im_start|>assistant\nHello!<|im_end|>", result);
        Assert.EndsWith("<|im_start|>assistant\n", result);
    }

    // ──────────── Mistral ────────────

    [TestMethod]
    public void Mistral_FormatUserTurn_ProducesCorrectFormat()
    {
        MistralChatTemplate template = new();

        string result = template.FormatUserTurn("Hello");

        Assert.AreEqual("[INST] Hello [/INST]\n", result);
    }

    [TestMethod]
    public void Mistral_FormatPrompt_SystemInsideFirstInst()
    {
        MistralChatTemplate template = new();
        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatMessageRole.System, "Be helpful."),
            new ChatMessage(ChatMessageRole.User, "Hello")
        };

        string result = template.FormatPrompt(messages);

        // Mistral puts system content directly in the first [INST], no <<SYS>> wrapper
        Assert.DoesNotContain("<<SYS>>", result);
        Assert.Contains("[INST] Be helpful.", result);
        Assert.Contains("Hello [/INST]", result);
    }

    // ──────────── Factory ────────────

    [TestMethod]
    public void Factory_ReturnsCorrectTemplateTypes()
    {
        IChatTemplate llama2 = ChatTemplateFactory.Create(ChatTemplateType.Llama2);
        IChatTemplate chatMl = ChatTemplateFactory.Create(ChatTemplateType.ChatMl);
        IChatTemplate mistral = ChatTemplateFactory.Create(ChatTemplateType.Mistral);
        IChatTemplate none = ChatTemplateFactory.Create(ChatTemplateType.None);

        Assert.IsInstanceOfType<Llama2ChatTemplate>(llama2);
        Assert.IsInstanceOfType<ChatMlTemplate>(chatMl);
        Assert.IsInstanceOfType<MistralChatTemplate>(mistral);
        Assert.IsInstanceOfType<Llama2ChatTemplate>(none); // None defaults to Llama2
    }
}
