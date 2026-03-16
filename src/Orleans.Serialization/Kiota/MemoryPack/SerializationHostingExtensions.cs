using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.Utilities.Internal;

namespace Orleans.Serialization;

public static class SerializationHostingExtensions
{
    private static readonly ServiceDescriptor ServiceDescriptor = new(typeof(KiotaMemoryPackCodec), typeof(KiotaMemoryPackCodec));

    public static ISerializerBuilder AddKiotaMemoryPackSerializer(this ISerializerBuilder serializerBuilder, Action<OptionsBuilder<KiotaMemoryPackOptions>>? configureOptions = null)
    {
        var services = serializerBuilder.Services;
        configureOptions?.Invoke(services.AddOptions<KiotaMemoryPackOptions>());

        if (!services.Contains(ServiceDescriptor))
        {
            services.AddSingleton<KiotaMemoryPackCodec>();
            services.AddFromExisting<IGeneralizedCodec, KiotaMemoryPackCodec>();
            services.AddFromExisting<IGeneralizedCopier, KiotaMemoryPackCodec>();
            services.AddFromExisting<ITypeFilter, KiotaMemoryPackCodec>();
        }
        return serializerBuilder;
    }
}
