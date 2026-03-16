using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    private static readonly ServiceDescriptor ServiceDescriptor = new(typeof(KiotaJsonCodec), typeof(KiotaJsonCodec));

    public static ISerializerBuilder AddKiotaJsonSerializer(this ISerializerBuilder serializerBuilder, Action<OptionsBuilder<KiotaJsonCodecOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaJsonCodecOptions>());

        if (!services.Contains(ServiceDescriptor))
        {
            services.AddSingleton<KiotaJsonCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaJsonCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaJsonCodec>();
            services.AddFromExisting<ITypeFilter, KiotaJsonCodec>();
        }
        return serializerBuilder;
    }
}
