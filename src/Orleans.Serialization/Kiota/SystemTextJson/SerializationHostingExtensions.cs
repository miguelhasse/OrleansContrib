using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    /// <summary>
    /// Adds a Kiota serializer to the serializer builder using a type predicate and optional compression.
    /// </summary>
    /// <remarks>Applies the same isSupported predicate to both serializer and deserializer registrations and
    /// sets options.Compression to the specified value.</remarks>
    /// <param name="serializerBuilder">The serializer builder to configure.</param>
    /// <param name="isSupported">Predicate that determines whether a given Type is supported for both serialization and deserialization; may be
    /// null.</param>
    /// <param name="compression">True to enable compression in the configured serializer options; false to leave compression disabled.</param>
    /// <returns>The provided ISerializerBuilder instance to support fluent chaining.</returns>
    public static ISerializerBuilder AddKiotaSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSupported,
        bool compression = false)
        => serializerBuilder.AddKiotaSerializer(
            isSupported,
            isSupported,
            optionsBuilder => optionsBuilder.Configure(options =>
            {
                options.Compression = compression;
            }));

    /// <summary>
    /// Adds the Kiota JSON codec and related selectors to the serializer builder and registers its well-known type
    /// alias.
    /// </summary>
    /// <remarks>Invokes configureOptions if provided. Registers KiotaJsonCodec and maps it to
    /// IGeneralizedCodec, IGeneralizedCopier, and ITypeFilter, and adds selectors only if not already
    /// registered.</remarks>
    /// <param name="serializerBuilder">Serializer builder to configure with the Kiota JSON codec and related services.</param>
    /// <param name="isSerializable">Predicate that determines whether a Type is supported for serialization by the Kiota JSON codec; if null no
    /// serialization selector is registered.</param>
    /// <param name="isCopyable">Predicate that determines whether a Type is supported for copying by the Kiota JSON codec; if null no copier
    /// selector is registered.</param>
    /// <param name="configureOptions">Optional callback to configure KiotaJsonCodec options via OptionsBuilder.</param>
    /// <returns>The serializer builder instance for chaining.</returns>
    public static ISerializerBuilder AddKiotaSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSerializable,
        Func<Type, bool>? isCopyable,
        Action<OptionsBuilder<KiotaJsonCodecOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaJsonCodecOptions>());

        if (isSerializable != null)
        {
            services.AddSingleton<ICodecSelector>(new DelegateCodecSelector
            {
                CodecName = KiotaJsonCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isSerializable
            });
        }

        if (isCopyable != null)
        {
            services.AddSingleton<ICopierSelector>(new DelegateCopierSelector
            {
                CopierName = KiotaJsonCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isCopyable
            });
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(KiotaJsonCodec)
            && descriptor.ImplementationType == typeof(KiotaJsonCodec)))
        {
            services.AddSingleton<KiotaJsonCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaJsonCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaJsonCodec>();
            services.AddFromExisting<ITypeFilter, KiotaJsonCodec>();
            serializerBuilder.Configure(options => options.WellKnownTypeAliases[KiotaJsonCodec.WellKnownAlias] = typeof(KiotaJsonCodec));
        }
        return serializerBuilder;
    }
}
