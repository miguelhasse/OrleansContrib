using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Concurrent;
using System.Reflection;

namespace Orleans.Serialization;

internal static class ParsableFactoryHelper
{
    // Cache factory delegates per-Type to avoid repeated reflection.
    private static readonly ConcurrentDictionary<Type, ParsableFactory<IParsable>> s_factoryCache = new();

    public static ParsableFactory<IParsable> Create(Type type) => s_factoryCache.GetOrAdd(type, t =>
    {
        var factoryMethod = t.GetMethod("CreateFromDiscriminatorValue", BindingFlags.Static | BindingFlags.Public, null, [typeof(IParseNode)], null) ??
            throw new InvalidOperationException($"Type '{t.FullName}' does not expose a public static CreateFromDiscriminatorValue(IParseNode) method.");
        return factoryMethod.CreateDelegate<ParsableFactory<IParsable>>();
    });
}
