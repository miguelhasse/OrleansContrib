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
    private readonly object? _value;
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
        var span = GetContiguousSpan(sequence, out var owner);
        try
        {
            var (value, _) = ReadTaggedValue(span);
            _value = value;
        }
        finally
        {
            owner?.Dispose();
        }
    }

    private MemoryPackParseNode(object? value, ParsablePropertyKeyMap? propertyKeyMap = null)
    {
        _value = value;
        _propertyKeyMap = propertyKeyMap;
    }

    private static ReadOnlySpan<byte> GetContiguousSpan(ReadOnlySequence<byte> sequence, out IMemoryOwner<byte>? owner)
    {
        if (sequence.IsSingleSegment)
        {
            owner = null;
            return sequence.FirstSpan;
        }

        owner = MemoryPool<byte>.Shared.Rent((int)sequence.Length);
        sequence.CopyTo(owner.Memory.Span);
        return owner.Memory.Span[..(int)sequence.Length];
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
                    var payload = span.Slice(pos, 16).ToArray();
                    pos += 16;
                    return (payload, pos);
                }

            case ValueTag.Guid:
                {
                    if (span.Length < pos + 16)
                        throw new InvalidOperationException("Unexpected end of MemoryPack Guid payload.");
                    var payload = span.Slice(pos, 16).ToArray();
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
                                map.StringEntries[key!] = stringVal;
                                break;
                            case MapKeyTag.Int:
                                int keyId = default;
                                pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref keyId);
                                var (intVal, intConsumed) = ReadTaggedValue(span.Slice(pos));
                                pos += intConsumed;
                                map.IntegerEntries[keyId] = intVal;
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

    public string? GetStringValue() => _value as string;

    public bool? GetBoolValue() => _value as bool?;

    public byte? GetByteValue() => _value is long l ? (byte)l : null;

    public sbyte? GetSbyteValue() => _value is long l ? (sbyte)l : null;

    public int? GetIntValue() => _value is long l ? (int)l : null;

    public long? GetLongValue() => _value as long?;

    public float? GetFloatValue() => _value is double d ? (float)d : null;

    public double? GetDoubleValue() => _value as double?;

    public decimal? GetDecimalValue()
    {
        if (_value is byte[] bytes && TryReadDecimal(bytes, out var decimalValue))
            return decimalValue;
        if (_value is string s)
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : null;
        if (_value is double d)
            return (decimal)d;
        return null;
    }

    public Guid? GetGuidValue()
        => _value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var g) => g,
            _ => null,
        };

    public DateTimeOffset? GetDateTimeOffsetValue()
        => _value switch
        {
            DateTimeOffset dto => dto,
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) => parsed,
            _ => null,
        };

    public TimeSpan? GetTimeSpanValue()
        => _value switch
        {
            long ticks => TimeSpan.FromTicks(ticks),
            string s when TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) => ts,
            _ => null,
        };

    public Date? GetDateValue()
    {
        if (_value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
            return new Date(d);
        return null;
    }

    public Time? GetTimeValue()
    {
        if (_value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var t))
            return new Time(t);
        return null;
    }

    public byte[]? GetByteArrayValue() => _value as byte[];

    public IParseNode? GetChildNode(string identifier)
    {
        if (_value is MemoryPackObjectMap map)
        {
            if (map.StringEntries.TryGetValue(identifier, out var child))
                return new MemoryPackParseNode(child);

            if (_propertyKeyMap?.TryGetId(identifier, out var keyId) == true &&
                map.IntegerEntries.TryGetValue(keyId, out child))
            {
                return new MemoryPackParseNode(child);
            }
        }
        return null;
    }

    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
    {
        if (_value is null) return default!;
        var typedNode = CreateTypedNode(typeof(T));
        var item = factory(typedNode);
        typedNode.OnBeforeAssignFieldValues?.Invoke(item);
        typedNode.AssignFieldValues(item);
        typedNode.OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    internal IParsable GetObjectValue(ParsableFactory<IParsable> factory, Type declaredType)
    {
        if (_value is null) return default!;
        var typedNode = CreateTypedNode(declaredType);
        var item = factory(typedNode);
        typedNode.OnBeforeAssignFieldValues?.Invoke(item);
        typedNode.AssignFieldValues(item);
        typedNode.OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    private void AssignFieldValues<T>(T item) where T : IParsable
    {
        if (_value is not MemoryPackObjectMap map)
            return;

        var deserializers = item.GetFieldDeserializers();
        var additionalData = (item as IAdditionalDataHolder)?.AdditionalData;
        var propertyKeyMap = ParsablePropertyKeyMap.For(item.GetType());

        foreach (var (key, val) in map.StringEntries)
        {
            var childNode = new MemoryPackParseNode(val);
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

            var childNode = new MemoryPackParseNode(val);
            if (deserializers.TryGetValue(key, out var deserializer))
                deserializer(childNode);
            else
                additionalData?.TryAdd(key, val!);
        }
    }

    private MemoryPackParseNode CreateTypedNode(Type type) => new(_value, ParsablePropertyKeyMap.For(type))
    {
        OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
        OnAfterAssignFieldValues = OnAfterAssignFieldValues,
    };

    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory)
        where T : IParsable
    {
        if (_value is null)
            return default!;

        if (_value is not List<object?> list)
            return [];

        var result = new List<T>(list.Count);
        foreach (var item in list)
        {
            var node = new MemoryPackParseNode(item);
            var parsed = node.GetObjectValue(factory);
            if (parsed is not null)
                result.Add(parsed);
        }

        return result;
    }

    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
    {
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
                if (item is byte[] decimalBytes && TryReadDecimal(decimalBytes, out var decimalValue))
                    return (T?)(object?)decimalValue;
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
    {
        if (bytes.Length == 16)
        {
            value = new decimal(
                [
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4)),
                ]);
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
}
