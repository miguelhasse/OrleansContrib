using Microsoft.Graph.Models;
using Orleans.Serialization.Kiota.Testing;

namespace Orleans.Serialization.Kiota.Tests;

public sealed class KiotaCodecChatEntityTests
{
    public static TheoryData<KiotaCodecKind, bool> CodecCompressionCases =>
        new()
        {
            { KiotaCodecKind.Json, false },
            { KiotaCodecKind.Json, true },
            { KiotaCodecKind.MessagePack, false },
            { KiotaCodecKind.MessagePack, true },
            { KiotaCodecKind.MemoryPack, false },
            { KiotaCodecKind.MemoryPack, true },
        };

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Object_serializer_round_trips_chat(KiotaCodecKind codecKind, bool compression)
    {
        var expected = GraphEntitySamples.CreateChat();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsType<Chat>(harness.ObjectSerializer.Deserialize(payload));

        Assert.True(GraphEntityAssert.AreEqual(expected, actual));
        Assert.NotSame(expected, actual);
        AssertChatCollectionsDetached(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Deep_copier_round_trips_chat(KiotaCodecKind codecKind, bool compression)
    {
        var expected = GraphEntitySamples.CreateChat();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var actual = Assert.IsType<Chat>(harness.ObjectDeepCopier.Copy(expected));

        Assert.True(GraphEntityAssert.AreEqual(expected, actual));
        Assert.NotSame(expected, actual);
        AssertChatCollectionsDetached(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Object_serializer_round_trips_chat_message(KiotaCodecKind codecKind, bool compression)
    {
        var expected = GraphEntitySamples.CreateChatMessage();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsType<ChatMessage>(harness.ObjectSerializer.Deserialize(payload));

        Assert.True(GraphEntityAssert.AreEqual(expected, actual));
        Assert.NotSame(expected, actual);
        AssertChatMessageCollectionsDetached(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Deep_copier_round_trips_chat_message(KiotaCodecKind codecKind, bool compression)
    {
        var expected = GraphEntitySamples.CreateChatMessage();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var actual = Assert.IsType<ChatMessage>(harness.ObjectDeepCopier.Copy(expected));

        Assert.True(GraphEntityAssert.AreEqual(expected, actual));
        Assert.NotSame(expected, actual);
        AssertChatMessageCollectionsDetached(expected, actual);
    }

    private static void AssertChatCollectionsDetached(Chat expected, Chat actual)
    {
        Assert.NotSame(expected.LastMessagePreview, actual.LastMessagePreview);
        Assert.NotSame(expected.Viewpoint, actual.Viewpoint);
        Assert.NotSame(expected.Members, actual.Members);
        Assert.NotSame(expected.Messages, actual.Messages);

        for (var index = 0; index < expected.Members!.Count; index++)
        {
            Assert.NotSame(expected.Members[index], actual.Members![index]);
        }

        for (var index = 0; index < expected.Messages!.Count; index++)
        {
            Assert.NotSame(expected.Messages[index], actual.Messages![index]);
        }

        AssertChatMessageCollectionsDetached(expected.Messages[0], actual.Messages![0]);
    }

    private static void AssertChatMessageCollectionsDetached(ChatMessage expected, ChatMessage actual)
    {
        Assert.NotSame(expected.Body, actual.Body);
        Assert.NotSame(expected.From, actual.From);
        Assert.NotSame(expected.Attachments, actual.Attachments);
        Assert.NotSame(expected.HostedContents, actual.HostedContents);
        Assert.NotSame(expected.Mentions, actual.Mentions);
        Assert.NotSame(expected.MessageHistory, actual.MessageHistory);
        Assert.NotSame(expected.Reactions, actual.Reactions);
        Assert.NotSame(expected.Replies, actual.Replies);

        for (var index = 0; index < expected.Attachments!.Count; index++)
        {
            Assert.NotSame(expected.Attachments[index], actual.Attachments![index]);
        }

        for (var index = 0; index < expected.HostedContents!.Count; index++)
        {
            Assert.NotSame(expected.HostedContents[index], actual.HostedContents![index]);
            Assert.NotSame(expected.HostedContents[index].ContentBytes, actual.HostedContents[index].ContentBytes);
        }

        for (var index = 0; index < expected.Mentions!.Count; index++)
        {
            Assert.NotSame(expected.Mentions[index], actual.Mentions![index]);
        }

        for (var index = 0; index < expected.MessageHistory!.Count; index++)
        {
            Assert.NotSame(expected.MessageHistory[index], actual.MessageHistory![index]);
        }

        for (var index = 0; index < expected.Reactions!.Count; index++)
        {
            Assert.NotSame(expected.Reactions[index], actual.Reactions![index]);
        }

        for (var index = 0; index < expected.Replies!.Count; index++)
        {
            Assert.NotSame(expected.Replies[index], actual.Replies![index]);
        }

        Assert.NotSame(expected.Replies[0].Body, actual.Replies![0].Body);
        Assert.NotSame(expected.Replies[0].From, actual.Replies[0].From);
    }
}
