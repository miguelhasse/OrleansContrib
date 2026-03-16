using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Concurrent;

namespace Orleans.Serialization;

internal sealed class ParsablePropertyKeyMap
{
    private static readonly ConcurrentDictionary<Type, ParsablePropertyKeyMap> s_cache = new();

    private readonly Dictionary<string, int> _keyToId;
    private readonly string[] _idToKey;

    private ParsablePropertyKeyMap(string[] keys)
    {
        _idToKey = keys;
        _keyToId = new Dictionary<string, int>(keys.Length, StringComparer.Ordinal);
        for (var i = 0; i < keys.Length; i++)
        {
            _keyToId[keys[i]] = i;
        }
    }

    public static ParsablePropertyKeyMap For(Type type) => s_cache.GetOrAdd(type, static t =>
    {
        var factory = ParsableFactoryHelper.Create(t);
        var instance = factory(EmptyParseNode.Instance)
            ?? throw new InvalidOperationException($"Type '{t.FullName}' could not be instantiated to build its property key map.");

        var keys = instance.GetFieldDeserializers()
            .Keys
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        return new ParsablePropertyKeyMap(keys);
    });

    public bool TryGetId(string key, out int id) => _keyToId.TryGetValue(key, out id);

    public bool TryGetKey(int id, out string? key)
    {
        if ((uint)id < (uint)_idToKey.Length)
        {
            key = _idToKey[id];
            return true;
        }

        key = null;
        return false;
    }
}
