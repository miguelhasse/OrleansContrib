using MemoryPack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
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

    private MemoryPackParseNode(object? value)
    {
        _value = value;
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

            case ValueTag.Map:
                {
                    int count = default;
                    pos += MemoryPackSerializer.Deserialize<int>(span.Slice(pos), ref count);
                    var dict = new Dictionary<string, object?>(count, StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        string? key = null;
                        pos += MemoryPackSerializer.Deserialize<string>(span.Slice(pos), ref key);
                        var (val, consumed) = ReadTaggedValue(span.Slice(pos));
                        pos += consumed;
                        dict[key!] = val;
                    }
                    return (dict, pos);
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
        if (_value is string s)
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : null;
        if (_value is double d)
            return (decimal)d;
        return null;
    }

    public Guid? GetGuidValue()
        => _value is string s && Guid.TryParse(s, out var g) ? g : null;

    public DateTimeOffset? GetDateTimeOffsetValue()
        => _value is string s && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
               DateTimeStyles.RoundtripKind, out var dt) ? dt : null;

    public TimeSpan? GetTimeSpanValue()
        => _value is string s && TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) ? ts : null;

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
        if (_value is Dictionary<string, object?> dict &&
            dict.TryGetValue(identifier, out var child))
            return new MemoryPackParseNode(child);
        return null;
    }

    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
    {
        if (_value is null) return default!;
        var item = factory(this);
        OnBeforeAssignFieldValues?.Invoke(item);
        AssignFieldValues(item);
        OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    private void AssignFieldValues<T>(T item) where T : IParsable
    {
        if (_value is not Dictionary<string, object?> dict)
            return;

        var deserializers = item.GetFieldDeserializers();
        var additionalData = (item as IAdditionalDataHolder)?.AdditionalData;

        foreach (var (key, val) in dict)
        {
            var childNode = new MemoryPackParseNode(val);
            if (deserializers.TryGetValue(key, out var deserializer))
                deserializer(childNode);
            else
                additionalData?.TryAdd(key, val!);
        }
    }

    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory)
        where T : IParsable
    {
        if (_value is not List<object?> list)
            yield break;

        foreach (var item in list)
        {
            var node = new MemoryPackParseNode(item);
            var parsed = node.GetObjectValue(factory);
            if (parsed is not null)
                yield return parsed;
        }
    }

    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
    {
        if (_value is not List<object?> list)
            yield break;

        foreach (var item in list)
            yield return ConvertValue<T>(item)!;
    }

    public IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum
    {
        if (_value is not List<object?> list)
            yield break;

        foreach (var item in list)
        {
            if (item is null) { yield return null; continue; }
            if (item is string s && Enum.TryParse<T>(s, true, out var fromStr))
                yield return fromStr;
            else if (item is long l)
                yield return (T)Enum.ToObject(typeof(T), l);
            else
                yield return null;
        }
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
                if (item is string sd)
                    return decimal.TryParse(sd, NumberStyles.Any,
                               CultureInfo.InvariantCulture, out var r)
                           ? (T?)(object?)r : default;
                if (item is double ddec)
                    return (T?)(object?)(decimal)ddec;
                return default;
            }
            if (targetType == typeof(Guid))
                return item is string sg && Guid.TryParse(sg, out var g)
                       ? (T?)(object?)g : default;
            if (targetType == typeof(DateTimeOffset))
                return item is string sdto &&
                       DateTimeOffset.TryParse(sdto, CultureInfo.InvariantCulture,
                           DateTimeStyles.RoundtripKind, out var dto)
                       ? (T?)(object?)dto : default;
            if (targetType == typeof(TimeSpan))
                return item is string sts &&
                       TimeSpan.TryParse(sts, CultureInfo.InvariantCulture, out var ts)
                       ? (T?)(object?)ts : default;
            if (targetType == typeof(byte[]))
                return (T?)(object?)(item as byte[]);
        }
        catch (OverflowException) { /* fall through */ }
        catch (FormatException) { /* fall through */ }

        return default;
    }
}
