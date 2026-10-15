using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    /// <summary>
    /// Adds the Kiota MessagePack serializer to the provided serializer builder and configures supported types and
    /// optional compression.
    /// </summary>
    /// <remarks>Delegates to AddKiotaMessagePackSerializer and sets the Compression option on the serializer
    /// options.</remarks>
    /// <param name="serializerBuilder">Serializer builder to extend with the Kiota MessagePack serializer.</param>
    /// <param name="isSupported">Predicate that determines which types the serializer supports for read and write operations.</param>
    /// <param name="compression">True to enable MessagePack compression; false to disable.</param>
    /// <returns>The same ISerializerBuilder instance for chaining.</returns>
    public static ISerializerBuilder AddKiotaMessagePackSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSupported,
        bool compression = false)
        => serializerBuilder.AddKiotaMessagePackSerializer(
            isSupported,
            isSupported,
            optionsBuilder => optionsBuilder.Configure(options =>
            {
                options.Compression = compression;
            }));

    /// <summary>
    /// Adds Kiota MessagePack serializer and related services to the serializer builder.
    /// </summary>
    /// <remarks>Registers KiotaMessagePackCodec, maps its well-known alias, and conditionally registers codec
    /// and copier selectors when predicates are provided.</remarks>
    /// <param name="serializerBuilder">Serializer builder to add MessagePack services to.</param>
    /// <param name="isSerializable">Optional predicate that selects types supported by the MessagePack codec.</param>
    /// <param name="isCopyable">Optional predicate that selects types supported by the copier.</param>
    /// <param name="configureOptions">Optional callback to configure KiotaMessagePackOptions.</param>
    /// <returns>The original ISerializerBuilder instance for chaining.</returns>
    public static ISerializerBuilder AddKiotaMessagePackSerializer(
        this ISerializerBuilder serializerBuilder,
        Func<Type, bool>? isSerializable,
        Func<Type, bool>? isCopyable,
        Action<OptionsBuilder<KiotaMessagePackOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaMessagePackOptions>());

        if (isSerializable != null)
        {
            services.AddSingleton<ICodecSelector>(new DelegateCodecSelector
            {
                CodecName = KiotaMessagePackCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isSerializable
            });
        }

        if (isCopyable != null)
        {
            services.AddSingleton<ICopierSelector>(new DelegateCopierSelector
            {
                CopierName = KiotaMessagePackCodec.WellKnownAlias,
                IsSupportedTypeDelegate = isCopyable
            });
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(KiotaMessagePackCodec)
            && descriptor.ImplementationType == typeof(KiotaMessagePackCodec)))
        {
            services.AddSingleton<KiotaMessagePackCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaMessagePackCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaMessagePackCodec>();
            services.AddFromExisting<ITypeFilter, KiotaMessagePackCodec>();
            serializerBuilder.Configure(options => options.WellKnownTypeAliases[KiotaMessagePackCodec.WellKnownAlias] = typeof(KiotaMessagePackCodec));
        }
        return serializerBuilder;
    }
}
