using LlmLocalSharp.Core.Clients;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Ut;

[TestClass]
public class ChatHistoryTests
{
    [TestMethod]
    public void AddUserMessage_AppendsToMessages()
    {
        ChatHistory history = new();
        history.AddUserMessage("Hello");

        Assert.HasCount(1, history.Messages);
        Assert.AreEqual(ChatMessageRole.User, history.Messages[0].Role);
        Assert.AreEqual("Hello", history.Messages[0].Content);
    }

    [TestMethod]
    public void AddAssistantMessage_AppendsToMessages()
    {
        ChatHistory history = new();
        history.AddAssistantMessage("Hi there!");

        Assert.HasCount(1, history.Messages);
        Assert.AreEqual(ChatMessageRole.Assistant, history.Messages[0].Role);
        Assert.AreEqual("Hi there!", history.Messages[0].Content);
    }

    [TestMethod]
    public void AddSystemMessage_AlwaysAtIndexZero()
    {
        ChatHistory history = new();
        history.AddUserMessage("Hello");
        history.AddSystemMessage("You are a helpful assistant.");

        Assert.HasCount(2, history.Messages);
        Assert.AreEqual(ChatMessageRole.System, history.Messages[0].Role);
        Assert.AreEqual("You are a helpful assistant.", history.Messages[0].Content);
        Assert.AreEqual(ChatMessageRole.User, history.Messages[1].Role);
    }

    [TestMethod]
    public void AddSystemMessage_ReplacesExistingSystemMessage()
    {
        ChatHistory history = new();
        history.AddSystemMessage("First system prompt.");
        history.AddUserMessage("Hello");
        history.AddSystemMessage("Updated system prompt.");

        Assert.HasCount(2, history.Messages);
        Assert.AreEqual(ChatMessageRole.System, history.Messages[0].Role);
        Assert.AreEqual("Updated system prompt.", history.Messages[0].Content);
    }

    [TestMethod]
    public void TurnCount_CountsUserMessagesOnly()
    {
        ChatHistory history = new();
        history.AddSystemMessage("System prompt.");
        history.AddUserMessage("Question 1");
        history.AddAssistantMessage("Answer 1");
        history.AddUserMessage("Question 2");
        history.AddAssistantMessage("Answer 2");

        Assert.AreEqual(2, history.TurnCount);
    }

    [TestMethod]
    public void TrimOldestTurns_PreservesSystemMessage()
    {
        ChatHistory history = new();
        history.AddSystemMessage("System prompt.");
        history.AddUserMessage("Question 1");
        history.AddAssistantMessage("Answer 1");
        history.AddUserMessage("Question 2");
        history.AddAssistantMessage("Answer 2");

        bool removed = history.TrimOldestTurns(1);

        Assert.IsTrue(removed);
        Assert.HasCount(3, history.Messages);
        Assert.AreEqual(ChatMessageRole.System, history.Messages[0].Role);
        Assert.AreEqual("System prompt.", history.Messages[0].Content);
        Assert.AreEqual(ChatMessageRole.User, history.Messages[1].Role);
        Assert.AreEqual("Question 2", history.Messages[1].Content);
    }

    [TestMethod]
    public void TrimOldestTurns_RemovesUserAndAssistantPair()
    {
        ChatHistory history = new();
        history.AddUserMessage("Q1");
        history.AddAssistantMessage("A1");
        history.AddUserMessage("Q2");
        history.AddAssistantMessage("A2");
        history.AddUserMessage("Q3");

        bool removed = history.TrimOldestTurns(2);

        Assert.IsTrue(removed);
        Assert.HasCount(1, history.Messages);
        Assert.AreEqual("Q3", history.Messages[0].Content);
    }

    [TestMethod]
    public void TrimOldestTurns_ZeroRemoves_ReturnsFalse()
    {
        ChatHistory history = new();
        history.AddUserMessage("Q1");

        bool removed = history.TrimOldestTurns(0);

        Assert.IsFalse(removed);
        Assert.HasCount(1, history.Messages);
    }

    [TestMethod]
    public void Clear_RemovesAllMessages()
    {
        ChatHistory history = new();
        history.AddSystemMessage("System");
        history.AddUserMessage("Hello");
        history.AddAssistantMessage("Hi");

        history.Clear();

        Assert.IsEmpty(history.Messages);
        Assert.AreEqual(0, history.TurnCount);
    }

    [TestMethod]
    public void MessagesOrder_IsPreserved()
    {
        ChatHistory history = new();
        history.AddSystemMessage("System");
        history.AddUserMessage("Q1");
        history.AddAssistantMessage("A1");
        history.AddUserMessage("Q2");
        history.AddAssistantMessage("A2");

        Assert.HasCount(5, history.Messages);
        Assert.AreEqual(ChatMessageRole.System, history.Messages[0].Role);
        Assert.AreEqual(ChatMessageRole.User, history.Messages[1].Role);
        Assert.AreEqual(ChatMessageRole.Assistant, history.Messages[2].Role);
        Assert.AreEqual(ChatMessageRole.User, history.Messages[3].Role);
        Assert.AreEqual(ChatMessageRole.Assistant, history.Messages[4].Role);
    }
}
