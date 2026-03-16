using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    private static readonly ServiceDescriptor ServiceDescriptor = new(typeof(KiotaMessagePackCodec), typeof(KiotaMessagePackCodec));

    public static ISerializerBuilder AddKiotaMessagePackSerializer(this ISerializerBuilder serializerBuilder, Action<OptionsBuilder<KiotaMessagePackOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaMessagePackOptions>());

        if (!services.Contains(ServiceDescriptor))
        {
            services.AddSingleton<KiotaMessagePackCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaMessagePackCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaMessagePackCodec>();
            services.AddFromExisting<ITypeFilter, KiotaMessagePackCodec>();
        }
        return serializerBuilder;
    }
}
