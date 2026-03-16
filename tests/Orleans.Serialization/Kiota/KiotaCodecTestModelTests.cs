using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Kiota.Testing;
using System.Buffers;
using MessagePack;
using System.Collections.Generic;
using MemoryPack;

namespace Orleans.Serialization.Kiota.Tests;

public sealed class KiotaCodecTestModelTests
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
    public void Object_serializer_round_trips_kiota_test_model(KiotaCodecKind codecKind, bool compression)
    {
        var expected = CreateTestModel();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsType<KiotaTestModel>(harness.ObjectSerializer.Deserialize(payload));

        Assert.Equal(expected, actual);
        Assert.NotSame(expected, actual);
        Assert.NotSame(expected.Nested, actual.Nested);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Deep_copier_round_trips_kiota_test_model(KiotaCodecKind codecKind, bool compression)
    {
        var expected = CreateTestModel();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var copy = Assert.IsType<KiotaTestModel>(harness.ObjectDeepCopier.Copy(expected));

        Assert.Equal(expected, copy);
        Assert.NotSame(expected, copy);
        Assert.NotSame(expected.Nested, copy.Nested);
    }

    [Theory]
    [MemberData(nameof(CodecCompressionCases))]
    public void Object_serializer_round_trips_as_interface_type(KiotaCodecKind codecKind, bool compression)
    {
        IParsable expected = CreateTestModel();

        using var harness = KiotaCodecHarnessFactory.Create(codecKind, compression);

        var payload = harness.ObjectSerializer.SerializeToArray(expected);
        var actual = Assert.IsType<KiotaTestModel>(harness.ObjectSerializer.Deserialize(payload));

        Assert.Equal((KiotaTestModel)expected, actual);
    }

    [Fact]
    public void MessagePack_writer_uses_integer_keys_for_typed_properties()
    {
        var writerType = typeof(KiotaMessagePackCodec).Assembly.GetType("Orleans.Serialization.MessagePackSerializationWriter")
            ?? throw new InvalidOperationException("Could not locate MessagePackSerializationWriter.");
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = Assert.IsAssignableFrom<ISerializationWriter>(Activator.CreateInstance(writerType, buffer));

        writer.WriteObjectValue(null, CreateTestModel());

        var reader = new MessagePackReader(buffer.WrittenMemory);
        Assert.Equal(MessagePackType.Map, reader.NextMessagePackType);

        var propertyCount = reader.ReadMapHeader();
        Assert.Equal(7, propertyCount);

        for (var i = 0; i < propertyCount; i++)
        {
            Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
            _ = reader.ReadInt32();
            reader.Skip();
        }
    }

    [Fact]
    public void MemoryPack_writer_uses_integer_keys_for_typed_properties()
    {
        var writerType = typeof(KiotaMemoryPackCodec).Assembly.GetType("Orleans.Serialization.MemoryPackSerializationWriter")
            ?? throw new InvalidOperationException("Could not locate MemoryPackSerializationWriter.");
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = Assert.IsAssignableFrom<ISerializationWriter>(Activator.CreateInstance(writerType, buffer));

        writer.WriteObjectValue(null, CreateTestModel());

        var span = buffer.WrittenSpan;
        var pos = 0;

        Assert.Equal(0x0A, span[pos++]);
        var rootCount = ReadValue<int>(span, ref pos);
        Assert.Equal(7, rootCount);

        for (var i = 0; i < rootCount; i++)
        {
            Assert.Equal(0x01, span[pos++]);
            var keyId = ReadValue<int>(span, ref pos);
            Assert.InRange(keyId, 0, rootCount - 1);
            SkipTaggedValue(span, ref pos);
        }
    }

    [Fact]
    public void MemoryPack_writer_uses_explicit_scalar_tags()
    {
        var writerType = typeof(KiotaMemoryPackCodec).Assembly.GetType("Orleans.Serialization.MemoryPackSerializationWriter")
            ?? throw new InvalidOperationException("Could not locate MemoryPackSerializationWriter.");
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = Assert.IsAssignableFrom<ISerializationWriter>(Activator.CreateInstance(writerType, buffer));

        writer.WriteObjectValue(null, CreateTestModel());

        var span = buffer.WrittenSpan;
        var pos = 0;

        Assert.Equal(0x0A, span[pos++]);
        var rootCount = ReadValue<int>(span, ref pos);
        Assert.Equal(7, rootCount);

        var rootTags = new Dictionary<int, byte>(rootCount);

        for (var i = 0; i < rootCount; i++)
        {
            Assert.Equal(0x01, span[pos++]);
            var keyId = ReadValue<int>(span, ref pos);
            rootTags[keyId] = span[pos];

            if (keyId == 4)
            {
                Assert.Equal((byte)0x0A, span[pos++]);
                var nestedCount = ReadValue<int>(span, ref pos);
                Assert.Equal(2, nestedCount);

                Assert.Equal((byte)0x01, span[pos++]);
                Assert.Equal(0, ReadValue<int>(span, ref pos));
                Assert.Equal((byte)0x0D, span[pos++]);
                pos += 16;

                Assert.Equal((byte)0x01, span[pos++]);
                Assert.Equal(1, ReadValue<int>(span, ref pos));
                SkipTaggedValue(span, ref pos);
            }
            else
            {
                SkipTaggedValue(span, ref pos);
            }
        }

        Assert.Equal((byte)0x0E, rootTags[0]);
        Assert.Equal((byte)0x0C, rootTags[1]);
        Assert.Equal((byte)0x0F, rootTags[2]);
    }

    [Fact]
    public void MessagePack_writer_uses_compact_scalar_shapes()
    {
        var writerType = typeof(KiotaMessagePackCodec).Assembly.GetType("Orleans.Serialization.MessagePackSerializationWriter")
            ?? throw new InvalidOperationException("Could not locate MessagePackSerializationWriter.");
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = Assert.IsAssignableFrom<ISerializationWriter>(Activator.CreateInstance(writerType, buffer));

        writer.WriteObjectValue(null, CreateTestModel());

        var reader = new MessagePackReader(buffer.WrittenMemory);
        var rootCount = reader.ReadMapHeader();
        var rootValueTypes = new Dictionary<int, MessagePackType>(rootCount);

        for (var i = 0; i < rootCount; i++)
        {
            var key = reader.ReadInt32();
            var valueType = reader.NextMessagePackType;
            rootValueTypes[key] = valueType;

            if (key == 4)
            {
                var nestedCount = reader.ReadMapHeader();
                Assert.Equal(2, nestedCount);

                var nestedIdKey = reader.ReadInt32();
                Assert.Equal(0, nestedIdKey);
                Assert.Equal(MessagePackType.Extension, reader.NextMessagePackType);
                reader.ReadExtensionFormat();

                var nestedLabelKey = reader.ReadInt32();
                Assert.Equal(1, nestedLabelKey);
                Assert.Equal(MessagePackType.String, reader.NextMessagePackType);
                reader.ReadString();
            }
            else
            {
                reader.Skip();
            }
        }

        Assert.Equal(MessagePackType.Extension, rootValueTypes[0]);
        Assert.Equal(MessagePackType.Binary, rootValueTypes[1]);
        Assert.Equal(MessagePackType.Integer, rootValueTypes[2]);
    }

    [Fact]
    public void MessagePack_writer_uses_extension_types_for_guid_and_datetimeoffset()
    {
        var writerType = typeof(KiotaMessagePackCodec).Assembly.GetType("Orleans.Serialization.MessagePackSerializationWriter")
            ?? throw new InvalidOperationException("Could not locate MessagePackSerializationWriter.");
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = Assert.IsAssignableFrom<ISerializationWriter>(Activator.CreateInstance(writerType, buffer));

        writer.WriteObjectValue(null, CreateTestModel());

        var reader = new MessagePackReader(buffer.WrittenMemory);
        var rootCount = reader.ReadMapHeader();

        for (var i = 0; i < rootCount; i++)
        {
            var key = reader.ReadInt32();
            if (key == 0)
            {
                var extension = reader.ReadExtensionFormat();
                Assert.Equal(MessagePackExtensionTypeCodes.DateTimeOffset, extension.TypeCode);
                Assert.Equal(10u, extension.Header.Length);
            }
            else if (key == 4)
            {
                var nestedCount = reader.ReadMapHeader();
                Assert.Equal(2, nestedCount);

                Assert.Equal(0, reader.ReadInt32());
                var extension = reader.ReadExtensionFormat();
                Assert.Equal(MessagePackExtensionTypeCodes.Guid, extension.TypeCode);
                Assert.Equal(16u, extension.Header.Length);

                Assert.Equal(1, reader.ReadInt32());
                reader.Skip();
            }
            else
            {
                reader.Skip();
            }
        }
    }

    private static KiotaTestModel CreateTestModel() =>
        new()
        {
            StringProp = "orleans-kiota-test-model",
            IntProp = 42,
            DateProp = new DateTimeOffset(2026, 03, 16, 0, 0, 0, TimeSpan.Zero),
            DecimalProp = 12345.6789m,
            DurationProp = TimeSpan.FromMinutes(135),
            Nested = new KiotaTestSubModel
            {
                Id = Guid.Parse("2de43f21-fdaa-4a74-ab93-2d422385f6a2"),
                Label = "nested-sub-model",
            },
            Tags =
            [
                "kiota",
                "orleans",
                "codec",
                "compression",
            ],
        };

    private static T ReadValue<T>(ReadOnlySpan<byte> span, ref int pos)
    {
        T? value = default;
        pos += MemoryPackSerializer.Deserialize<T>(span[pos..], ref value);
        return value!;
    }

    private static void SkipTaggedValue(ReadOnlySpan<byte> span, ref int pos)
    {
        var tag = span[pos++];
        switch (tag)
        {
            case 0x00:
                return;
            case 0x01:
                _ = ReadValue<bool>(span, ref pos);
                return;
            case 0x02:
                _ = ReadValue<byte>(span, ref pos);
                return;
            case 0x03:
                _ = ReadValue<sbyte>(span, ref pos);
                return;
            case 0x04:
                _ = ReadValue<int>(span, ref pos);
                return;
            case 0x05:
                _ = ReadValue<long>(span, ref pos);
                return;
            case 0x06:
                _ = ReadValue<float>(span, ref pos);
                return;
            case 0x07:
                _ = ReadValue<double>(span, ref pos);
                return;
            case 0x08:
                _ = ReadValue<string>(span, ref pos);
                return;
            case 0x09:
                _ = ReadValue<byte[]>(span, ref pos);
                return;
            case 0x0A:
                {
                    var count = ReadValue<int>(span, ref pos);
                    for (var i = 0; i < count; i++)
                    {
                        var keyTag = span[pos++];
                        if (keyTag == 0x00)
                        {
                            _ = ReadValue<string>(span, ref pos);
                        }
                        else
                        {
                            Assert.Equal((byte)0x01, keyTag);
                            _ = ReadValue<int>(span, ref pos);
                        }

                        SkipTaggedValue(span, ref pos);
                    }
                    return;
                }
            case 0x0B:
                {
                    var count = ReadValue<int>(span, ref pos);
                    for (var i = 0; i < count; i++)
                    {
                        SkipTaggedValue(span, ref pos);
                    }
                    return;
                }
            case 0x0C:
            case 0x0D:
                pos += 16;
                return;
            case 0x0E:
                pos += 10;
                return;
            case 0x0F:
                _ = ReadValue<long>(span, ref pos);
                return;
            default:
                throw new InvalidOperationException($"Unknown MemoryPack test tag 0x{tag:X2}.");
        }
    }
}
