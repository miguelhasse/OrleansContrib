using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Kiota.Testing;

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

    private static KiotaTestModel CreateTestModel() =>
        new()
        {
            StringProp = "orleans-kiota-test-model",
            IntProp = 42,
            DateProp = new DateTimeOffset(2026, 03, 16, 0, 0, 0, TimeSpan.Zero),
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
}
