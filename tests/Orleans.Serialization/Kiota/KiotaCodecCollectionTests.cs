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

    public static TheoryData<KiotaCodecKind, GraphEntityKind, bool> CollectionEntityCases =>
        new()
        {
            { KiotaCodecKind.Json, GraphEntityKind.User, false },
            { KiotaCodecKind.Json, GraphEntityKind.User, true },
            { KiotaCodecKind.Json, GraphEntityKind.Message, false },
            { KiotaCodecKind.Json, GraphEntityKind.Message, true },
            { KiotaCodecKind.Json, GraphEntityKind.Group, false },
            { KiotaCodecKind.Json, GraphEntityKind.Group, true },
            { KiotaCodecKind.Json, GraphEntityKind.Contact, false },
            { KiotaCodecKind.Json, GraphEntityKind.Contact, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.User, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.User, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Message, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Message, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Group, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Group, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Contact, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Contact, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.User, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.User, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Message, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Message, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Group, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Group, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Contact, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Contact, true },
        };

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
}
