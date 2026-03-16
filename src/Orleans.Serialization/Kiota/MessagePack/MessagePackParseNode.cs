using MessagePack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;

namespace Orleans.Serialization;

/// <summary>
/// Wraps a deserialized MessagePack value and exposes it as a Kiota <see cref="IParseNode"/>.
/// </summary>
/// <remarks>
/// The raw MessagePack bytes are decoded into an in-memory object tree on construction:
/// maps become <see cref="MessagePackObjectMap"/>, arrays become <see cref="List{Object}"/>,
/// and scalars are their natural .NET equivalents.
/// </remarks>
internal class MessagePackParseNode : IParseNode
{
    private readonly object? _value;
    private readonly ParsablePropertyKeyMap? _propertyKeyMap;

    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }

    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

    public MessagePackParseNode(ReadOnlyMemory<byte> memory)
    {
        var reader = new MessagePackReader(memory);
        _value = ReadValue(ref reader);
    }

    public MessagePackParseNode(ReadOnlySequence<byte> sequence)
    {
        var reader = new MessagePackReader(sequence);
        _value = ReadValue(ref reader);
    }

    private MessagePackParseNode(object? value, ParsablePropertyKeyMap? propertyKeyMap = null)
    {
        _value = value;
        _propertyKeyMap = propertyKeyMap;
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

    private static MessagePackObjectMap ReadMap(ref MessagePackReader reader)
    {
        var count = reader.ReadMapHeader();
        var map = new MessagePackObjectMap(count);
        for (var i = 0; i < count; i++)
        {
            switch (reader.NextMessagePackType)
            {
                case MessagePackType.String:
                    var stringKey = reader.ReadString()
                        ?? throw new InvalidOperationException("MessagePack map key cannot be null.");
                    map.StringEntries[stringKey] = ReadValue(ref reader);
                    break;
                case MessagePackType.Integer:
                    var integerKey = ReadMapKeyId(ref reader);
                    map.IntegerEntries[integerKey] = ReadValue(ref reader);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected MessagePack map key type: {reader.NextMessagePackType}");
            }
        }
        return map;
    }

    private static int ReadMapKeyId(ref MessagePackReader reader)
    {
        var raw = ReadInteger(ref reader);
        return raw is long value && value is >= 0 and <= int.MaxValue
            ? (int)value
            : throw new InvalidOperationException("MessagePack integer map key must fit in a non-negative Int32.");
    }

    private static object? ReadExtension(ref MessagePackReader reader)
    {
        var header = reader.ReadExtensionFormatHeader();
        if (header.TypeCode == -1)
            return reader.ReadDateTime(header);

        var data = reader.ReadRaw(header.Length);
        return header.TypeCode switch
        {
            MessagePackExtensionTypeCodes.Guid => ReadGuidExtension(data),
            MessagePackExtensionTypeCodes.DateTimeOffset => ReadDateTimeOffsetExtension(data),
            _ => null,
        };
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
            byte[] { Length: 16 } bytes => new Guid(bytes),
            string s when Guid.TryParse(s, out var g) => g,
            _ => null,
        };

    public DateTimeOffset? GetDateTimeOffsetValue()
        => TryGetDateTimeOffset(_value, out var result) ? result : null;

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
        if (_value is MessagePackObjectMap map)
        {
            if (map.StringEntries.TryGetValue(identifier, out var child))
                return new MessagePackParseNode(child);

            if (_propertyKeyMap?.TryGetId(identifier, out var keyId) == true &&
                map.IntegerEntries.TryGetValue(keyId, out child))
            {
                return new MessagePackParseNode(child);
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
        if (_value is not MessagePackObjectMap map)
            return;

        var deserializers = item.GetFieldDeserializers();
        var additionalData = (item as IAdditionalDataHolder)?.AdditionalData;
        var propertyKeyMap = ParsablePropertyKeyMap.For(item.GetType());

        foreach (var (key, val) in map.StringEntries)
        {
            var childNode = new MessagePackParseNode(val);
            if (deserializers.TryGetValue(key, out var deserializer))
                deserializer(childNode);
            else
                additionalData?.TryAdd(key, val!);
        }

        foreach (var (keyId, val) in map.IntegerEntries)
        {
            if (!propertyKeyMap.TryGetKey(keyId, out var key) || key is null)
            {
                throw new InvalidOperationException($"Unknown MessagePack property key id '{keyId}' for type '{item.GetType().FullName}'.");
            }

            var childNode = new MessagePackParseNode(val);
            if (deserializers.TryGetValue(key, out var deserializer))
            {
                deserializer(childNode);
            }
            else
            {
                additionalData?.TryAdd(key, val!);
            }
        }
    }

    private MessagePackParseNode CreateTypedNode(Type type) => new(_value, ParsablePropertyKeyMap.For(type))
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
            var node = new MessagePackParseNode(item);
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
                if (item is byte[] { Length: 16 } guidBytes)
                    return (T?)(object?)new Guid(guidBytes);
                return item is string sg && Guid.TryParse(sg, out var g)
                    ? (T?)(object?)g : default;
            }
            if (targetType == typeof(DateTimeOffset))
            {
                if (TryGetDateTimeOffset(item, out var dto))
                    return (T?)(object?)dto;
                return default;
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
        catch (OverflowException) { /* fall through to default */ }
        catch (FormatException) { /* fall through to default */ }

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

    private static bool TryGetDateTimeOffset(object? value, out DateTimeOffset result)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffset:
                result = dateTimeOffset;
                return true;
            case List<object?> list when
                list.Count == 2 &&
                list[0] is DateTime dateTime &&
                list[1] is long offsetMinutes:
                    result = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
                        .ToOffset(TimeSpan.FromMinutes(offsetMinutes));
                    return true;
            case DateTime dateTime:
                result = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                return true;
            case string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed):
                result = parsed;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static Guid ReadGuidExtension(ReadOnlySequence<byte> data)
    {
        var bytes = data.ToArray();
        if (bytes.Length != 16)
            throw new InvalidOperationException($"MessagePack Guid extension payload must be 16 bytes, got {bytes.Length}.");

        return new Guid(bytes);
    }

    private static DateTimeOffset ReadDateTimeOffsetExtension(ReadOnlySequence<byte> data)
    {
        var bytes = data.ToArray();
        if (bytes.Length != 10)
            throw new InvalidOperationException($"MessagePack DateTimeOffset extension payload must be 10 bytes, got {bytes.Length}.");

        var utcTicks = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(0, 8));
        var offsetMinutes = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(8, 2));
        return new DateTimeOffset(utcTicks, TimeSpan.Zero).ToOffset(TimeSpan.FromMinutes(offsetMinutes));
    }
}

internal sealed class MessagePackObjectMap
{
    public MessagePackObjectMap(int capacity)
    {
        StringEntries = new Dictionary<string, object?>(capacity, StringComparer.Ordinal);
        IntegerEntries = new Dictionary<int, object?>(capacity);
    }

    public Dictionary<string, object?> StringEntries { get; }

    public Dictionary<int, object?> IntegerEntries { get; }
}
