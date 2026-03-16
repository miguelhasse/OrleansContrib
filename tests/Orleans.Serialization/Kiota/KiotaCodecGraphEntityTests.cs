using Kiota.Serialization.MemoryPack.Orleans.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Kiota.Testing;
using Orleans.Serialization.TestKit;

namespace Orleans.Serialization.Kiota.Tests;

public sealed class KiotaCodecGraphEntityTests(ITestOutputHelper output)
{
    public static TheoryData<KiotaCodecKind, GraphEntityKind, bool> GraphCodecCases =>
        new()
        {
            { KiotaCodecKind.Json, GraphEntityKind.User, false },
            { KiotaCodecKind.Json, GraphEntityKind.User, true },
            { KiotaCodecKind.Json, GraphEntityKind.Message, false },
            { KiotaCodecKind.Json, GraphEntityKind.Message, true },
            { KiotaCodecKind.Json, GraphEntityKind.Event, false },
            { KiotaCodecKind.Json, GraphEntityKind.Event, true },
            { KiotaCodecKind.Json, GraphEntityKind.Group, false },
            { KiotaCodecKind.Json, GraphEntityKind.Group, true },
            { KiotaCodecKind.Json, GraphEntityKind.Contact, false },
            { KiotaCodecKind.Json, GraphEntityKind.Contact, true },
            { KiotaCodecKind.Json, GraphEntityKind.DriveItem, false },
            { KiotaCodecKind.Json, GraphEntityKind.DriveItem, true },
            { KiotaCodecKind.Json, GraphEntityKind.Team, false },
            { KiotaCodecKind.Json, GraphEntityKind.Team, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.User, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.User, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Message, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Message, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Event, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Event, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Group, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Group, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Contact, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Contact, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.DriveItem, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.DriveItem, true },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Team, false },
            { KiotaCodecKind.MessagePack, GraphEntityKind.Team, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.User, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.User, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Message, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Message, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Event, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Event, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Group, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Group, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Contact, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Contact, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.DriveItem, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.DriveItem, true },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Team, false },
            { KiotaCodecKind.MemoryPack, GraphEntityKind.Team, true },
        };

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

    public static TheoryData<KiotaCodecKind> CodecKinds =>
        new()
        {
            { KiotaCodecKind.Json },
            { KiotaCodecKind.MessagePack },
            { KiotaCodecKind.MemoryPack },
        };

    [Theory]
    [MemberData(nameof(GraphCodecCases))]
    public void Object_serializer_round_trips_graph_entities(KiotaCodecKind codecKind, GraphEntityKind entityKind, bool compression)
    {
        var expected = GraphEntitySamples.Create(entityKind);

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsAssignableFrom<IParsable>(harness.ObjectSerializer.Deserialize(payload));

        GraphEntityAssert.Equal(expected, actual);
        Assert.NotSame(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GraphCodecCases))]
    public void Deep_copier_round_trips_graph_entities(KiotaCodecKind codecKind, GraphEntityKind entityKind, bool compression)
    {
        var expected = GraphEntitySamples.Create(entityKind);

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var copy = Assert.IsAssignableFrom<IParsable>(harness.ObjectDeepCopier.Copy(expected));

        GraphEntityAssert.Equal(expected, copy);
        Assert.NotSame(expected, copy);
    }

    [Theory]
    [MemberData(nameof(CodecKinds))]
    public void Compression_reduces_payload_size_for_large_graph_payloads(KiotaCodecKind codecKind)
    {
        const GraphEntityKind entityKind = GraphEntityKind.Message;

        using var uncompressedHarness = KiotaCodecHarnessFactory.Create(codecKind, compression: false);
        using var compressedHarness = KiotaCodecHarnessFactory.Create(codecKind, compression: true);

        var value = GraphEntitySamples.Create(entityKind);
        var uncompressed = uncompressedHarness.ObjectSerializer.SerializeToArray(value);
        var compressed = compressedHarness.ObjectSerializer.SerializeToArray(value);

        var roundTripped = Assert.IsAssignableFrom<IParsable>(compressedHarness.ObjectSerializer.Deserialize(compressed));

        GraphEntityAssert.Equal(value, roundTripped);
        Assert.True(compressed.Length < uncompressed.Length, $"{codecKind} compression should reduce payload size.");
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void TestKit_smoke_tests_cover_message_copy_semantics(KiotaCodecKind codecKind, bool compression)
    {
        var copierTester = CreateCopierTester(codecKind, compression, output);

        copierTester.Verify();
    }

    private static IMessageCopierTester CreateCopierTester(KiotaCodecKind codecKind, bool compression, ITestOutputHelper output) => codecKind switch
    {
        KiotaCodecKind.Json => new JsonMessageCopierTester(output, compression),
        KiotaCodecKind.MessagePack => new MessagePackMessageCopierTester(output, compression),
        KiotaCodecKind.MemoryPack => new MemoryPackMessageCopierTester(output, compression),
        _ => throw new ArgumentOutOfRangeException(nameof(codecKind), codecKind, "Unknown Kiota codec kind."),
    };

    private interface IMessageCopierTester : IDisposable
    {
        void Verify();
    }

    private abstract class MessageCopierTester(ITestOutputHelper output, bool compression)
        : CopierTester<Message, IDeepCopier<Message>>(new XunitV3OutputAdapter(output)), IMessageCopierTester
    {
        private readonly bool _compression = compression;

        protected abstract KiotaCodecKind CodecKind { get; }

        protected override void Configure(ISerializerBuilder serializerBuilder) =>
            KiotaCodecHarnessFactory.Register(serializerBuilder.Services, CodecKind, _compression);

        protected override IDeepCopier<Message> CreateCopier() => ServiceProvider.GetRequiredService<IDeepCopier<Message>>();

        protected override Message CreateValue() => GraphEntitySamples.CreateMessage();

        protected override Message[] TestValues => [GraphEntitySamples.CreateMessage()];

        protected override bool Equals(Message left, Message right) => GraphEntityAssert.AreEqual(left, right);

        protected override Action<Action<Message>> ValueProvider =>
            sink =>
            {
                foreach (var value in TestValues)
                {
                    sink(value);
                }
            };

        public void Verify()
        {
            CopiedValuesAreEqual();
            ReferencesAreAddedToCopyContext();
            CanCopyCollectionViaSerializer();
        }

        public void Dispose()
        {
        }
    }

    private sealed class JsonMessageCopierTester(ITestOutputHelper output, bool compression) : MessageCopierTester(output, compression)
    {
        protected override KiotaCodecKind CodecKind => KiotaCodecKind.Json;
    }

    private sealed class MessagePackMessageCopierTester(ITestOutputHelper output, bool compression) : MessageCopierTester(output, compression)
    {
        protected override KiotaCodecKind CodecKind => KiotaCodecKind.MessagePack;
    }

    private sealed class MemoryPackMessageCopierTester(ITestOutputHelper output, bool compression) : MessageCopierTester(output, compression)
    {
        protected override KiotaCodecKind CodecKind => KiotaCodecKind.MemoryPack;
    }
}
