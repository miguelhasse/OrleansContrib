using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Kiota.Testing;

namespace Orleans.Serialization.Kiota.Tests;

public sealed class KiotaCodecCollectionTests
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

    public static TheoryData<KiotaCodecKind, GraphEntityKind, bool> CollectionEntityCases => CreateCollectionEntityCases();

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Object_serializer_round_trips_collection_test_model(KiotaCodecKind codecKind, bool compression)
    {
        var expected = CreateCollectionTestModel();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsType<KiotaCollectionTestModel>(harness.ObjectSerializer.Deserialize(payload));

        AssertCollectionModelEqual(expected, actual);
        Assert.NotSame(expected, actual);
        Assert.NotSame(expected.PrimaryItem, actual.PrimaryItem);
        Assert.NotSame(expected.Items, actual.Items);
        Assert.All(actual.Items!, item => Assert.NotNull(item));
        Assert.NotNull(actual.EmptyItems);
        Assert.Empty(actual.EmptyItems!);
        Assert.NotNull(actual.EmptyTags);
        Assert.Empty(actual.EmptyTags!);
        Assert.NotNull(actual.EmptyStates);
        Assert.Empty(actual.EmptyStates!);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Deep_copier_round_trips_collection_test_model(KiotaCodecKind codecKind, bool compression)
    {
        var expected = CreateCollectionTestModel();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var copy = Assert.IsType<KiotaCollectionTestModel>(harness.ObjectDeepCopier.Copy(expected));

        AssertCollectionModelEqual(expected, copy);
        Assert.NotSame(expected, copy);
        Assert.NotSame(expected.PrimaryItem, copy.PrimaryItem);
        Assert.NotSame(expected.Items, copy.Items);
        Assert.All(copy.Items!, item => Assert.NotSame(expected.Items!.Single(source => source.Id == item.Id), item));
        Assert.NotNull(copy.EmptyItems);
        Assert.Empty(copy.EmptyItems!);
    }

    [Theory]
    [MemberData(nameof(CollectionEntityCases))]
    public void Object_serializer_round_trips_collection_graph_entities(KiotaCodecKind codecKind, GraphEntityKind entityKind, bool compression)
    {
        var expected = GraphEntitySamples.Create(entityKind);

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsAssignableFrom<IParsable>(harness.ObjectSerializer.Deserialize(payload));

        GraphEntityAssert.Equal(expected, actual);
        Assert.NotSame(expected, actual);
        AssertGraphEntityCollectionsDetached(expected, actual, entityKind);
    }

    [Theory]
    [MemberData(nameof(CollectionEntityCases))]
    public void Deep_copier_round_trips_collection_graph_entities(KiotaCodecKind codecKind, GraphEntityKind entityKind, bool compression)
    {
        var expected = GraphEntitySamples.Create(entityKind);

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var copy = Assert.IsAssignableFrom<IParsable>(harness.ObjectDeepCopier.Copy(expected));

        GraphEntityAssert.Equal(expected, copy);
        Assert.NotSame(expected, copy);
        AssertGraphEntityCollectionsDetached(expected, copy, entityKind);
    }

    private static KiotaCollectionTestModel CreateCollectionTestModel() =>
        new()
        {
            Name = "collection-test-model",
            PrimaryItem = new KiotaCollectionItemModel
            {
                Id = Guid.Parse("3d7f2503-c5fc-4742-b8db-62650d7659df"),
                Label = "primary-item",
                Status = KiotaTestStatus.Active,
                Aliases = ["primary", "lead"],
            },
            Tags = ["kiota", "collections", "orleans", "compression"],
            Items =
            [
                new KiotaCollectionItemModel
                {
                    Id = Guid.Parse("8db5ee9f-6b05-422c-af7e-10fb67af08f6"),
                    Label = "item-one",
                    Status = KiotaTestStatus.Draft,
                    Aliases = ["one", "draft"],
                },
                new KiotaCollectionItemModel
                {
                    Id = Guid.Parse("ba02dd0a-caa9-4a1f-bdf9-4b2125b29a91"),
                    Label = "item-two",
                    Status = KiotaTestStatus.Archived,
                    Aliases = ["two", "archive"],
                },
            ],
            States = [KiotaTestStatus.Draft, KiotaTestStatus.Active, KiotaTestStatus.Archived],
            EmptyTags = [],
            EmptyItems = [],
            EmptyStates = [],
        };

    private static void AssertCollectionModelEqual(KiotaCollectionTestModel expected, KiotaCollectionTestModel actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.PrimaryItem, actual.PrimaryItem);
        Assert.Equal(expected.Tags, actual.Tags);
        Assert.Equal(expected.Items, actual.Items);
        Assert.Equal(expected.States, actual.States);
        Assert.Equal(expected.EmptyTags, actual.EmptyTags);
        Assert.Equal(expected.EmptyItems, actual.EmptyItems);
        Assert.Equal(expected.EmptyStates, actual.EmptyStates);
    }

    private static TheoryData<KiotaCodecKind, GraphEntityKind, bool> CreateCollectionEntityCases()
    {
        var cases = new TheoryData<KiotaCodecKind, GraphEntityKind, bool>();

        foreach (var codecKind in Enum.GetValues<KiotaCodecKind>())
        {
            foreach (var entityKind in Enum.GetValues<GraphEntityKind>())
            {
                cases.Add(codecKind, entityKind, false);
                cases.Add(codecKind, entityKind, true);
            }
        }

        return cases;
    }

    private static void AssertGraphEntityCollectionsDetached(IParsable expected, IParsable actual, GraphEntityKind entityKind)
    {
        switch (entityKind)
        {
            case GraphEntityKind.User:
                AssertUserCollectionsDetached(Assert.IsType<User>(expected), Assert.IsType<User>(actual));
                break;
            case GraphEntityKind.Message:
                AssertMessageCollectionsDetached(Assert.IsType<Message>(expected), Assert.IsType<Message>(actual));
                break;
            case GraphEntityKind.Chat:
                AssertChatCollectionsDetached(Assert.IsType<Chat>(expected), Assert.IsType<Chat>(actual));
                break;
            case GraphEntityKind.ChatMessage:
                AssertChatMessageCollectionsDetached(Assert.IsType<ChatMessage>(expected), Assert.IsType<ChatMessage>(actual));
                break;
            case GraphEntityKind.Event:
                AssertEventCollectionsDetached(Assert.IsType<Event>(expected), Assert.IsType<Event>(actual));
                break;
            case GraphEntityKind.Group:
                AssertGroupCollectionsDetached(Assert.IsType<Group>(expected), Assert.IsType<Group>(actual));
                break;
            case GraphEntityKind.Contact:
                AssertContactCollectionsDetached(Assert.IsType<Contact>(expected), Assert.IsType<Contact>(actual));
                break;
            case GraphEntityKind.DriveItem:
                AssertDriveItemCollectionsDetached(Assert.IsType<DriveItem>(expected), Assert.IsType<DriveItem>(actual));
                break;
            case GraphEntityKind.Team:
                AssertTeamCollectionsDetached(Assert.IsType<Team>(expected), Assert.IsType<Team>(actual));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entityKind), entityKind, "Unknown graph entity kind.");
        }
    }

    private static void AssertUserCollectionsDetached(User expected, User actual)
    {
        Assert.NotSame(expected.BusinessPhones, actual.BusinessPhones);
        Assert.NotSame(expected.Identities, actual.Identities);
        Assert.NotSame(expected.Manager, actual.Manager);

        for (var index = 0; index < expected.Identities!.Count; index++)
        {
            Assert.NotSame(expected.Identities[index], actual.Identities![index]);
        }
    }

    private static void AssertMessageCollectionsDetached(Message expected, Message actual)
    {
        Assert.NotSame(expected.ToRecipients, actual.ToRecipients);
        Assert.NotSame(expected.ReplyTo, actual.ReplyTo);
        Assert.NotSame(expected.InternetMessageHeaders, actual.InternetMessageHeaders);
        Assert.NotSame(expected.Attachments, actual.Attachments);

        for (var index = 0; index < expected.ToRecipients!.Count; index++)
        {
            Assert.NotSame(expected.ToRecipients[index], actual.ToRecipients![index]);
        }

        for (var index = 0; index < expected.ReplyTo!.Count; index++)
        {
            Assert.NotSame(expected.ReplyTo[index], actual.ReplyTo![index]);
        }

        for (var index = 0; index < expected.InternetMessageHeaders!.Count; index++)
        {
            Assert.NotSame(expected.InternetMessageHeaders[index], actual.InternetMessageHeaders![index]);
        }

        for (var index = 0; index < expected.Attachments!.Count; index++)
        {
            Assert.NotSame(expected.Attachments[index], actual.Attachments![index]);
        }

        var expectedFirstAttachment = Assert.IsType<FileAttachment>(expected.Attachments[0]);
        var actualFirstAttachment = Assert.IsType<FileAttachment>(actual.Attachments![0]);
        Assert.NotSame(expectedFirstAttachment.ContentBytes, actualFirstAttachment.ContentBytes);
    }

    private static void AssertEventCollectionsDetached(Event expected, Event actual)
    {
        Assert.NotSame(expected.Attendees, actual.Attendees);
        Assert.NotSame(expected.Locations, actual.Locations);

        for (var index = 0; index < expected.Attendees!.Count; index++)
        {
            Assert.NotSame(expected.Attendees[index], actual.Attendees![index]);
        }

        for (var index = 0; index < expected.Locations!.Count; index++)
        {
            Assert.NotSame(expected.Locations[index], actual.Locations![index]);
        }
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

    private static void AssertGroupCollectionsDetached(Group expected, Group actual)
    {
        Assert.NotSame(expected.GroupTypes, actual.GroupTypes);
        Assert.NotSame(expected.Members, actual.Members);
        Assert.NotSame(expected.Owners, actual.Owners);

        for (var index = 0; index < expected.Members!.Count; index++)
        {
            Assert.NotSame(expected.Members[index], actual.Members![index]);
        }

        for (var index = 0; index < expected.Owners!.Count; index++)
        {
            Assert.NotSame(expected.Owners[index], actual.Owners![index]);
        }
    }

    private static void AssertContactCollectionsDetached(Contact expected, Contact actual)
    {
        Assert.NotSame(expected.EmailAddresses, actual.EmailAddresses);
        Assert.NotSame(expected.BusinessPhones, actual.BusinessPhones);
        Assert.NotSame(expected.HomePhones, actual.HomePhones);
        Assert.NotSame(expected.Categories, actual.Categories);
        Assert.NotSame(expected.Children, actual.Children);

        for (var index = 0; index < expected.EmailAddresses!.Count; index++)
        {
            Assert.NotSame(expected.EmailAddresses[index], actual.EmailAddresses![index]);
        }
    }

    private static void AssertDriveItemCollectionsDetached(DriveItem expected, DriveItem actual)
    {
        var expectedChildren = Assert.IsAssignableFrom<IReadOnlyList<DriveItem>>(expected.Children);
        var actualChildren = Assert.IsAssignableFrom<IReadOnlyList<DriveItem>>(actual.Children);

        Assert.NotSame(expectedChildren, actualChildren);

        for (var index = 0; index < expectedChildren.Count; index++)
        {
            Assert.NotSame(expectedChildren[index], actualChildren[index]);
        }

        var expectedNestedFolder = expectedChildren[1];
        var actualNestedFolder = actualChildren[1];

        Assert.NotSame(expectedNestedFolder.Children, actualNestedFolder.Children);
        Assert.NotSame(expectedNestedFolder.Children![0], actualNestedFolder.Children![0]);
    }

    private static void AssertTeamCollectionsDetached(Team expected, Team actual)
    {
        Assert.NotSame(expected.Group, actual.Group);
        AssertGroupCollectionsDetached(expected.Group!, actual.Group!);
    }
}
