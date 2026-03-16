using MessagePack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
using System.Globalization;

namespace Orleans.Serialization;

/// <summary>
/// Wraps a deserialized MessagePack value and exposes it as a Kiota <see cref="IParseNode"/>.
/// </summary>
/// <remarks>
/// The raw MessagePack bytes are decoded into an in-memory object tree on construction:
/// maps become <see cref="Dictionary{String,Object}"/>, arrays become <see cref="List{Object}"/>,
/// and scalars are their natural .NET equivalents.
/// </remarks>
internal class MessagePackParseNode : IParseNode
{
    private readonly object? _value;

    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }

    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

    public MessagePackParseNode(ReadOnlyMemory<byte> memory)
    {
        var reader = new MessagePackReader(memory);
        _value = ReadValue(ref reader);
    }

    private MessagePackParseNode(object? value)
    {
        _value = value;
    }

    private static object? ReadValue(ref MessagePackReader reader)
    {
        // Handle nil first so we don't need to re-check in every branch.
        if (reader.TryReadNil())
            return null;

        return reader.NextMessagePackType switch
        {
            MessagePackType.Boolean => (object?)reader.ReadBoolean(),
            MessagePackType.Integer => ReadInteger(ref reader),
            MessagePackType.Float => reader.ReadDouble(),
            MessagePackType.String => reader.ReadString(),
            MessagePackType.Binary => ReadBinary(ref reader),
            MessagePackType.Array => ReadArray(ref reader),
            MessagePackType.Map => ReadMap(ref reader),
            MessagePackType.Extension => ReadExtension(ref reader),
            _ => throw new InvalidOperationException(
                     $"Unexpected MessagePack type: {reader.NextMessagePackType}")
        };
    }

    private static object ReadInteger(ref MessagePackReader reader)
    {
        // Use the raw code to decide whether we need the unsigned path.
        var code = reader.NextCode;
        if (code == MessagePackCode.UInt64)
            return (object)(long)reader.ReadUInt64(); // may lose high bit for very large values
        return reader.ReadInt64();
    }

    private static object? ReadBinary(ref MessagePackReader reader)
    {
        var seq = reader.ReadBytes();
        return seq.HasValue ? seq.Value.ToArray() : null;
    }

    private static List<object?> ReadArray(ref MessagePackReader reader)
    {
        var count = reader.ReadArrayHeader();
        var list = new List<object?>(count);
        for (var i = 0; i < count; i++)
            list.Add(ReadValue(ref reader));
        return list;
    }

    private static Dictionary<string, object?> ReadMap(ref MessagePackReader reader)
    {
        var count = reader.ReadMapHeader();
        var dict = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadString()
                      ?? throw new InvalidOperationException("MessagePack map key cannot be null.");
            dict[key] = ReadValue(ref reader);
        }
        return dict;
    }

    private static object? ReadExtension(ref MessagePackReader reader)
    {
        reader.ReadExtensionFormat(); // skip unknown extensions
        return null;
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
            return new MessagePackParseNode(child);
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
            var childNode = new MessagePackParseNode(val);
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
            var node = new MessagePackParseNode(item);
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
        catch (OverflowException) { /* fall through to default */ }
        catch (FormatException) { /* fall through to default */ }

        return default;
    }
}
