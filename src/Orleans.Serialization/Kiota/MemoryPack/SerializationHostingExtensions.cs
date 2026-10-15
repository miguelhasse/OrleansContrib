using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    /// <summary>
    /// Adds a Kiota MemoryPack serializer to the serializer builder using a type predicate and optional compression.
    /// </summary>
    /// <remarks>Delegates to AddKiotaMemoryPackSerializer, applying the same predicate for readers and
    /// writers and setting Compression accordingly.</remarks>
    /// <param name="serializerBuilder">Serializer builder to extend.</param>
    /// <param name="isSupported">Predicate determining supported types; when null, no type filtering.</param>
    /// <param name="compression">Enable compression for serialized output.</param>
    /// <returns>The configured ISerializerBuilder.</returns>
    public static ISerializerBuilder AddKiotaMemoryPackSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSupported,
        bool compression = false)
        => serializerBuilder.AddKiotaMemoryPackSerializer(
            isSupported,
            isSupported,
            optionsBuilder => optionsBuilder.Configure(options =>
            {
                options.Compression = compression;
            }));

    /// <summary>
    /// Register Kiota MemoryPack codec, copier, and type filter services with the serializer builder and add the codec
    /// alias to well-known type aliases.
    /// </summary>
    /// <remarks>Registers KiotaMemoryPackCodec as a singleton and maps IGeneralizedCodec, IGeneralizedCopier,
    /// and ITypeFilter to that instance. Adds the codec alias to the builder's WellKnownTypeAliases. Registers delegate
    /// selectors when predicates are supplied. Avoids duplicate registrations if services already present.</remarks>
    /// <param name="serializerBuilder">Serializer builder used to register services and configure well-known type aliases.</param>
    /// <param name="isSerializable">Optional predicate to identify types supported by the MemoryPack serializer; when provided registers a codec
    /// selector for the codec alias.</param>
    /// <param name="isCopyable">Optional predicate to identify types supported by the MemoryPack copier; when provided registers a copier
    /// selector for the codec alias.</param>
    /// <param name="configureOptions">Optional action to configure KiotaMemoryPackOptions using an OptionsBuilder.</param>
    /// <returns>The original ISerializerBuilder instance for chaining.</returns>
    public static ISerializerBuilder AddKiotaMemoryPackSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSerializable,
        Func<Type, bool>? isCopyable,
        Action<OptionsBuilder<KiotaMemoryPackOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaMemoryPackOptions>());

        if (isSerializable != null)
        {
            services.AddSingleton<ICodecSelector>(new DelegateCodecSelector
            {
                CodecName = KiotaMemoryPackCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isSerializable
            });
        }

        if (isCopyable != null)
        {
            services.AddSingleton<ICopierSelector>(new DelegateCopierSelector
            {
                CopierName = KiotaMemoryPackCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isCopyable
            });
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(KiotaMemoryPackCodec)
            && descriptor.ImplementationType == typeof(KiotaMemoryPackCodec)))
        {
            services.AddSingleton<KiotaMemoryPackCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaMemoryPackCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaMemoryPackCodec>();
            services.AddFromExisting<ITypeFilter, KiotaMemoryPackCodec>();
            serializerBuilder.Configure(options => options.WellKnownTypeAliases[KiotaMemoryPackCodec.WellKnownAlias] = typeof(KiotaMemoryPackCodec));
        }
        return serializerBuilder;
    }
}
