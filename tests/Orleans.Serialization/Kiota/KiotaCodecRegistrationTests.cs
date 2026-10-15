using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Configuration;
using Orleans.Serialization.Kiota.Testing;
using Orleans.Serialization.Serializers;
using System.Reflection;

namespace Orleans.Serialization.Kiota.Tests;

public sealed class KiotaCodecRegistrationTests
{
    public static TheoryData<KiotaCodecKind> CodecKinds =>
    [
        KiotaCodecKind.Json,
        KiotaCodecKind.MessagePack,
        KiotaCodecKind.MemoryPack,
    ];

    [Theory]
    [MemberData(nameof(CodecKinds))]
    public void Simple_registration_adds_codec_and_copier_selectors(KiotaCodecKind codecKind)
    {
        var registration = GetRegistration(codecKind);
        Func<Type, bool> isSupported = type => type == typeof(KiotaTestModel);

        using var serviceProvider = CreateServiceProvider(
            registration,
            isSupported,
            isCopyable: null,
            compression: true,
            useFullOverload: false);

        var codecSelector = Assert.Single(serviceProvider.GetServices<ICodecSelector>(), selector => selector.CodecName == registration.Alias);
        Assert.True(codecSelector.IsSupportedType(typeof(KiotaTestModel)));
        Assert.False(codecSelector.IsSupportedType(typeof(KiotaCollectionTestModel)));

        var copierSelector = Assert.Single(serviceProvider.GetServices<ICopierSelector>(), selector => selector.CopierName == registration.Alias);
        Assert.True(copierSelector.IsSupportedType(typeof(KiotaTestModel)));
        Assert.False(copierSelector.IsSupportedType(typeof(KiotaCollectionTestModel)));

        Assert.Single(serviceProvider.GetServices<IGeneralizedCodec>(), codec => codec.GetType() == registration.CodecType);
        Assert.Single(serviceProvider.GetServices<IGeneralizedCopier>(), copier => copier.GetType() == registration.CodecType);

        var optionsServiceType = typeof(IOptions<>).MakeGenericType(registration.OptionsType);
        var options = serviceProvider.GetRequiredService(optionsServiceType);
        var optionsValue = optionsServiceType.GetProperty("Value")!.GetValue(options)!;
        Assert.True((bool)registration.OptionsType.GetProperty("Compression")!.GetValue(optionsValue)!);

        var aliases = serviceProvider.GetRequiredService<IOptions<TypeManifestOptions>>().Value.WellKnownTypeAliases;
        Assert.True(aliases.TryGetValue(registration.Alias, out var codecType));
        Assert.Equal(registration.CodecType, codecType);
    }

    [Theory]
    [MemberData(nameof(CodecKinds))]
    public void Full_registration_applies_serialization_and_copy_predicates_independently(KiotaCodecKind codecKind)
    {
        var registration = GetRegistration(codecKind);
        Func<Type, bool> isSerializable = type => type == typeof(KiotaTestModel);
        Func<Type, bool> isCopyable = type => type == typeof(KiotaCollectionTestModel);

        using var serviceProvider = CreateServiceProvider(
            registration,
            isSerializable,
            isCopyable,
            compression: false,
            useFullOverload: true);

        var codecSelector = Assert.Single(serviceProvider.GetServices<ICodecSelector>(), selector => selector.CodecName == registration.Alias);
        Assert.True(codecSelector.IsSupportedType(typeof(KiotaTestModel)));
        Assert.False(codecSelector.IsSupportedType(typeof(KiotaCollectionTestModel)));

        var copierSelector = Assert.Single(serviceProvider.GetServices<ICopierSelector>(), selector => selector.CopierName == registration.Alias);
        Assert.False(copierSelector.IsSupportedType(typeof(KiotaTestModel)));
        Assert.True(copierSelector.IsSupportedType(typeof(KiotaCollectionTestModel)));
    }

    [Theory]
    [MemberData(nameof(CodecKinds))]
    public void Repeated_registration_does_not_duplicate_codec_services(KiotaCodecKind codecKind)
    {
        var registration = GetRegistration(codecKind);

        using var serviceProvider = CreateServiceProvider(
            registration,
            isSerializable: null,
            isCopyable: null,
            compression: false,
            useFullOverload: true,
            registerTwice: true);

        Assert.DoesNotContain(serviceProvider.GetServices<ICodecSelector>(), selector => selector.CodecName == registration.Alias);
        Assert.DoesNotContain(serviceProvider.GetServices<ICopierSelector>(), selector => selector.CopierName == registration.Alias);
        Assert.Single(serviceProvider.GetServices<IGeneralizedCodec>(), codec => codec.GetType() == registration.CodecType);
        Assert.Single(serviceProvider.GetServices<IGeneralizedCopier>(), copier => copier.GetType() == registration.CodecType);
    }

    private static ServiceProvider CreateServiceProvider(
        CodecRegistration registration,
        Func<Type, bool>? isSerializable,
        Func<Type, bool>? isCopyable,
        bool compression,
        bool useFullOverload,
        bool registerTwice = false)
    {
        var services = new ServiceCollection();
        services.AddSerializer(serializerBuilder =>
        {
            var extensionType = registration.CodecType.Assembly.GetType("Orleans.Serialization.SerializationHostingExtensions", throwOnError: true)!;
            var parameterCount = useFullOverload ? 4 : 3;
            var method = Assert.Single(extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static),
                candidate => candidate.Name == (useFullOverload ? registration.FullRegistrationMethod : "AddKiotaSerializer")
                    && candidate.GetParameters().Length == parameterCount);
            var arguments = useFullOverload
                ? new object?[] { serializerBuilder, isSerializable, isCopyable, null }
                : [serializerBuilder, isSerializable, compression];

            method.Invoke(null, arguments);
            if (registerTwice)
            {
                method.Invoke(null, arguments);
            }
        });

        return services.BuildServiceProvider();
    }

    private static CodecRegistration GetRegistration(KiotaCodecKind codecKind) => codecKind switch
    {
        KiotaCodecKind.Json => new(
            typeof(KiotaJsonCodec),
            typeof(KiotaJsonCodecOptions),
            KiotaJsonCodec.WellKnownAlias,
            "AddKiotaSerializer"),
        KiotaCodecKind.MessagePack => new(
            typeof(KiotaMessagePackCodec),
            typeof(KiotaMessagePackOptions),
            KiotaMessagePackCodec.WellKnownAlias,
            "AddKiotaMessagePackSerializer"),
        KiotaCodecKind.MemoryPack => new(
            typeof(KiotaMemoryPackCodec),
            typeof(KiotaMemoryPackOptions),
            KiotaMemoryPackCodec.WellKnownAlias,
            "AddKiotaMemoryPackSerializer"),
        _ => throw new ArgumentOutOfRangeException(nameof(codecKind), codecKind, "Unknown Kiota codec kind."),
    };

    private sealed record CodecRegistration(Type CodecType, Type OptionsType, string Alias, string FullRegistrationMethod);
}
