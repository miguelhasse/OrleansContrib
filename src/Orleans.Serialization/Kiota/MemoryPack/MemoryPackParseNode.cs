using MemoryPack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;

namespace Orleans.Serialization;

/// <summary>
/// Wraps a deserialized MemoryPack value and exposes it as a Kiota <see cref="IParseNode"/>.
/// </summary>
/// <remarks>
/// The raw MemoryPack bytes use a custom tag-prefixed keyed-map format (see
/// <see cref="MemoryPackSerializationWriter"/> for the encoding details).
/// The bytes are decoded into an in-memory object tree on construction:
/// maps become <see cref="Dictionary{String,Object}"/>, arrays become <see cref="List{Object}"/>,
/// and scalars are their natural .NET equivalents.
/// </remarks>
internal class MemoryPackParseNode : IParseNode
{
    private object? _value;
    private readonly ParsablePropertyKeyMap? _propertyKeyMap;

    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }
    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

    public MemoryPackParseNode(ReadOnlyMemory<byte> memory)
    {
        var (value, _) = ReadTaggedValue(memory.Span);
        _value = value;
    }

    public MemoryPackParseNode(ReadOnlySequence<byte> sequence)
    {
        var span = sequence.IsSingleSegment
            ? sequence.FirstSpan
            : sequence.ToArray().AsSpan();

        var (value, _) = ReadTaggedValue(span);
        _value = value;
    }

    private MemoryPackParseNode(object? value, ParsablePropertyKeyMap? propertyKeyMap = null)
    {
        _value = value;
        _propertyKeyMap = propertyKeyMap;
    }

    private void ResetValue(object? value)
    {
        _value = value;
    }

    private static (object? Value, int Consumed) ReadTaggedValue(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
            throw new InvalidOperationException("Unexpected end of MemoryPack buffer.");

        var tag = (ValueTag)span[0];
        var pos = 1; // advance past the tag byte

        switch (tag)
        {
            case ValueTag.Null:
                return (null, 1);

            case ValueTag.Bool:
                {
                    bool v = default;
                    pos += MemoryPackSerializer.Deserialize<bool>(span.Slice(pos), ref v);
                    return (v, pos);
                }

            case ValueTag.Byte:
                {
                    byte v = default;
                    pos += MemoryPackSerializer.Deserialize<byte>(span.Slice(pos), ref v);
                    return ((long)v, pos); // store all integers as long (consistent with MessagePack)
                }

            case ValueTag.SByte:
                {
                    sbyte v = default;
                    pos += MemoryPackSerializer.Deserialize<sbyte>(span.Slice(pos), ref v);
                    return ((long)v, pos);
                }

            case ValueTag.Int:
                {
                    int v = default;
                    pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref v);
                    return ((long)v, pos);
                }

            case ValueTag.Long:
                {
                    long v = default;
                    pos += MemoryPackSerializer.Deserialize<long>(span.Slice(pos), ref v);
                    return (v, pos);
                }

            case ValueTag.Float:
                {
                    float v = default;
                    pos += MemoryPackSerializer.Deserialize<float>(span.Slice(pos), ref v);
                    return ((double)v, pos); // store as double (consistent with MessagePack)
                }

            case ValueTag.Double:
                {
                    double v = default;
                    pos += MemoryPackSerializer.Deserialize<double>(span.Slice(pos), ref v);
                    return (v, pos);
                }

            case ValueTag.String:
                {
                    string? v = null;
                    pos += MemoryPackSerializer.Deserialize<string>(span.Slice(pos), ref v);
                    return (v, pos);
                }

            case ValueTag.Bytes:
                {
                    byte[]? v = null;
                    pos += MemoryPackSerializer.Deserialize<byte[]>(span.Slice(pos), ref v);
                    return (v, pos);
                }

            case ValueTag.Decimal:
                {
                    if (span.Length < pos + 16)
                        throw new InvalidOperationException("Unexpected end of MemoryPack decimal payload.");
                    var payload = span.Slice(pos, 16);
                    pos += 16;
                    if (!TryReadDecimal(payload, out var value))
                        throw new InvalidOperationException("Invalid MemoryPack decimal payload.");
                    return (value, pos);
                }

            case ValueTag.Guid:
                {
                    if (span.Length < pos + 16)
                        throw new InvalidOperationException("Unexpected end of MemoryPack Guid payload.");
                    var payload = span.Slice(pos, 16);
                    pos += 16;
                    return (new Guid(payload), pos);
                }

            case ValueTag.DateTimeOffset:
                {
                    if (span.Length < pos + 10)
                        throw new InvalidOperationException("Unexpected end of MemoryPack DateTimeOffset payload.");
                    var utcTicks = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(pos, 8));
                    var offsetMinutes = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(pos + 8, 2));
                    pos += 10;
                    return (new DateTimeOffset(utcTicks, TimeSpan.Zero).ToOffset(TimeSpan.FromMinutes(offsetMinutes)), pos);
                }

            case ValueTag.TimeSpan:
                {
                    long ticks = default;
                    pos += MemoryPackSerializer.Deserialize<long>(span.Slice(pos), ref ticks);
                    return (ticks, pos);
                }

            case ValueTag.Map:
                {
                    int count = default;
                    pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref count);
                    var map = new MemoryPackObjectMap(count);
                    for (int i = 0; i < count; i++)
                    {
                        if (span.Length <= pos)
                            throw new InvalidOperationException("Unexpected end of MemoryPack map key.");

                        var keyTag = (MapKeyTag)span[pos++];
                        switch (keyTag)
                        {
                            case MapKeyTag.String:
                                string? key = null;
                                pos += MemoryPackSerializer.Deserialize<string>(span.Slice(pos), ref key);
                                var (stringVal, stringConsumed) = ReadTaggedValue(span.Slice(pos));
                                pos += stringConsumed;
                                map.AddStringEntry(key!, stringVal);
                                break;
                            case MapKeyTag.Int:
                                int keyId = default;
                                pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref keyId);
                                var (intVal, intConsumed) = ReadTaggedValue(span.Slice(pos));
                                pos += intConsumed;
                                map.AddIntegerEntry(keyId, intVal);
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown MemoryPack map key tag: 0x{(byte)keyTag:X2}");
                        }
                    }
                    return (map, pos);
                }

            case ValueTag.Array:
                {
                    int count = default;
                    pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref count);
                    var list = new List<object?>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var (item, consumed) = ReadTaggedValue(span.Slice(pos));
                        pos += consumed;
                        list.Add(item);
                    }
                    return (list, pos);
                }

            default:
                throw new InvalidOperationException($"Unknown MemoryPack value tag: 0x{(byte)tag:X2}");
        }
    }

    private void EnsureMaterialized() { }

    public string? GetStringValue()
    {
        EnsureMaterialized();
        return _value as string;
    }

    public bool? GetBoolValue()
    {
        EnsureMaterialized();
        return _value as bool?;
    }

    public byte? GetByteValue()
    {
        EnsureMaterialized();
        return _value is long l ? (byte)l : null;
    }

    public sbyte? GetSbyteValue()
    {
        EnsureMaterialized();
        return _value is long l ? (sbyte)l : null;
    }

    public int? GetIntValue()
    {
        EnsureMaterialized();
        return _value is long l ? (int)l : null;
    }

    public long? GetLongValue()
    {
        EnsureMaterialized();
        return _value as long?;
    }

    public float? GetFloatValue()
    {
        EnsureMaterialized();
        return _value is double d ? (float)d : null;
    }

    public double? GetDoubleValue()
    {
        EnsureMaterialized();
        return _value as double?;
    }

    public decimal? GetDecimalValue()
    {
        EnsureMaterialized();
        if (_value is decimal value)
            return value;
        if (_value is byte[] bytes && TryReadDecimal(bytes, out var decimalValue))
            return decimalValue;
        if (_value is string s)
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : null;
        if (_value is double d)
            return (decimal)d;
        return null;
    }

    public Guid? GetGuidValue()
    {
        EnsureMaterialized();
        return _value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var g) => g,
            _ => null,
        };
    }

    public DateTimeOffset? GetDateTimeOffsetValue()
    {
        EnsureMaterialized();
        return _value switch
        {
            DateTimeOffset dto => dto,
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) => parsed,
            _ => null,
        };
    }

    public TimeSpan? GetTimeSpanValue()
    {
        EnsureMaterialized();
        return _value switch
        {
            long ticks => TimeSpan.FromTicks(ticks),
            string s when TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) => ts,
            _ => null,
        };
    }

    public Date? GetDateValue()
    {
        EnsureMaterialized();
        if (_value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
            return new Date(d);
        return null;
    }

    public Time? GetTimeValue()
    {
        EnsureMaterialized();
        if (_value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var t))
            return new Time(t);
        return null;
    }

    public byte[]? GetByteArrayValue()
    {
        EnsureMaterialized();
        return _value as byte[];
    }

    public IParseNode? GetChildNode(string identifier)
    {
        EnsureMaterialized();
        if (_value is MemoryPackObjectMap map)
        {
            if (map.TryGetStringEntry(identifier, out var child))
                return new MemoryPackParseNode(child);

            if (_propertyKeyMap?.TryGetId(identifier, out var keyId) == true &&
                map.TryGetIntegerEntry(keyId, out child))
            {
                return new MemoryPackParseNode(child);
            }
        }
        return null;
    }

    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
    {
        if (_value is null)
            return default!;

        var typedNode = CreateTypedNode(typeof(T));
        var item = factory(typedNode);
        typedNode.OnBeforeAssignFieldValues?.Invoke(item);
        typedNode.AssignFieldValues(item);
        typedNode.OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    internal IParsable GetObjectValue(ParsableFactory<IParsable> factory, Type declaredType)
    {
        if (_value is null)
            return default!;

        var typedNode = CreateTypedNode(declaredType);
        var item = factory(typedNode);
        typedNode.OnBeforeAssignFieldValues?.Invoke(item);
        typedNode.AssignFieldValues(item);
        typedNode.OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    private void AssignFieldValues<T>(T item) where T : IParsable
    {
        EnsureMaterialized();
        if (_value is not MemoryPackObjectMap map)
            return;

        var deserializers = item.GetFieldDeserializers();
        var additionalData = (item as IAdditionalDataHolder)?.AdditionalData;
        var propertyKeyMap = ParsablePropertyKeyMap.For(item.GetType());
        var childNode = new MemoryPackParseNode((object?)null);

        foreach (var (key, val) in map.StringEntries)
        {
            childNode.ResetValue(val);
            if (deserializers.TryGetValue(key, out var deserializer))
                deserializer(childNode);
            else
                additionalData?.TryAdd(key, val!);
        }

        foreach (var (keyId, val) in map.IntegerEntries)
        {
            if (!propertyKeyMap.TryGetKey(keyId, out var key) || key is null)
            {
                throw new InvalidOperationException($"Unknown MemoryPack property key id '{keyId}' for type '{item.GetType().FullName}'.");
            }

            childNode.ResetValue(val);
            if (deserializers.TryGetValue(key, out var deserializer))
                deserializer(childNode);
            else
                additionalData?.TryAdd(key, val!);
        }
    }

    private MemoryPackParseNode CreateTypedNode(Type type)
    {
        var propertyKeyMap = ParsablePropertyKeyMap.For(type);
        var node = new MemoryPackParseNode(_value, propertyKeyMap);

        node.OnBeforeAssignFieldValues = OnBeforeAssignFieldValues;
        node.OnAfterAssignFieldValues = OnAfterAssignFieldValues;
        return node;
    }

    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory)
        where T : IParsable
    {
        EnsureMaterialized();
        if (_value is null)
            return default!;

        if (_value is not List<object?> list)
            return [];

        var result = new List<T>(list.Count);
        var node = new MemoryPackParseNode((object?)null);
        foreach (var item in list)
        {
            node.ResetValue(item);
            var parsed = node.GetObjectValue(factory);
            if (parsed is not null)
                result.Add(parsed);
        }

        return result;
    }

    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
    {
        EnsureMaterialized();
        if (_value is null)
            return default!;

        if (_value is not List<object?> list)
            return [];

        var result = new List<T>(list.Count);
        foreach (var item in list)
            result.Add(ConvertValue<T>(item)!);

        return result;
    }

    public IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum
    {
        EnsureMaterialized();
        if (_value is null)
            return default!;

        if (_value is not List<object?> list)
            return [];

        var result = new List<T?>(list.Count);
        foreach (var item in list)
        {
            if (item is null)
            {
                result.Add(null);
            }
            else if (item is string s && Enum.TryParse<T>(s, true, out var fromStr))
            {
                result.Add(fromStr);
            }
            else if (item is long l)
            {
                result.Add((T)Enum.ToObject(typeof(T), l));
            }
            else
            {
                result.Add(null);
            }
        }

        return result;
    }

    public T? GetEnumValue<T>() where T : struct, Enum
    {
        EnsureMaterialized();
        if (_value is string s && Enum.TryParse<T>(s, true, out var fromStr))
            return fromStr;
        if (_value is long l)
            return (T)Enum.ToObject(typeof(T), l);
        return null;
    }

    private static T? ConvertValue<T>(object? item)
    {
        if (item is null)
            return default;

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            if (targetType == typeof(string))
                return (T?)(object?)Convert.ToString(item, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return item is bool b ? (T?)(object?)b : default;
            if (targetType == typeof(byte))
                return item is long lb ? (T?)(object?)(byte)lb : default;
            if (targetType == typeof(sbyte))
                return item is long lsb ? (T?)(object?)(sbyte)lsb : default;
            if (targetType == typeof(int))
                return item is long li ? (T?)(object?)(int)li : default;
            if (targetType == typeof(long))
                return item is long ll ? (T?)(object?)ll : default;
            if (targetType == typeof(float))
                return item is double df ? (T?)(object?)(float)df : default;
            if (targetType == typeof(double))
                return item is double dd ? (T?)(object?)dd : default;
            if (targetType == typeof(decimal))
            {
                if (item is decimal decimalValue)
                    return (T?)(object?)decimalValue;
                if (item is byte[] decimalBytes && TryReadDecimal(decimalBytes, out var parsedDecimal))
                    return (T?)(object?)parsedDecimal;
                if (item is string sd)
                    return decimal.TryParse(sd, NumberStyles.Any,
                               CultureInfo.InvariantCulture, out var r)
                           ? (T?)(object?)r : default;
                if (item is double ddec)
                    return (T?)(object?)(decimal)ddec;
                return default;
            }
            if (targetType == typeof(Guid))
            {
                if (item is Guid guid)
                    return (T?)(object?)guid;
                return item is string sg && Guid.TryParse(sg, out var g)
                    ? (T?)(object?)g : default;
            }
            if (targetType == typeof(DateTimeOffset))
            {
                if (item is DateTimeOffset dto)
                    return (T?)(object?)dto;
                return item is string sdto &&
                    DateTimeOffset.TryParse(sdto, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out dto)
                    ? (T?)(object?)dto : default;
            }
            if (targetType == typeof(TimeSpan))
            {
                if (item is long ticks)
                    return (T?)(object?)TimeSpan.FromTicks(ticks);
                return item is string sts &&
                    TimeSpan.TryParse(sts, CultureInfo.InvariantCulture, out var ts)
                    ? (T?)(object?)ts : default;
            }
            if (targetType == typeof(byte[]))
                return (T?)(object?)(item as byte[]);
        }
        catch (OverflowException) { /* fall through */ }
        catch (FormatException) { /* fall through */ }

        return default;
    }

    private static bool TryReadDecimal(byte[] bytes, out decimal value)
        => TryReadDecimal(bytes.AsSpan(), out value);

    private static bool TryReadDecimal(ReadOnlySpan<byte> bytes, out decimal value)
    {
        if (bytes.Length == 16)
        {
            var flags = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(12, 4));
            value = new decimal(
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                (flags & unchecked((int)0x80000000)) != 0,
                (byte)((flags >> 16) & 0x7F));
            return true;
        }

        value = default;
        return false;
    }
}

internal sealed class MemoryPackObjectMap
{
    public MemoryPackObjectMap(int capacity)
    {
        StringEntries = new Dictionary<string, object?>(capacity, StringComparer.Ordinal);
        IntegerEntries = new Dictionary<int, object?>(capacity);
    }

    public Dictionary<string, object?> StringEntries { get; }

    public Dictionary<int, object?> IntegerEntries { get; }

    public void AddStringEntry(string key, object? value)
    {
        StringEntries[key] = value;
    }

    public void AddIntegerEntry(int key, object? value)
    {
        IntegerEntries[key] = value;
    }

    public bool TryGetStringEntry(string key, out object? value)
        => StringEntries.TryGetValue(key, out value);

    public bool TryGetIntegerEntry(int key, out object? value)
        => IntegerEntries.TryGetValue(key, out value);
}
