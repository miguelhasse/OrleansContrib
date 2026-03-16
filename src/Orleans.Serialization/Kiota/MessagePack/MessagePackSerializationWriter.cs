using MessagePack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;

namespace Orleans.Serialization;

/// <summary>
/// Serializes Kiota models to MessagePack binary format by writing directly
/// into a caller-supplied <see cref="IBufferWriter{Byte}"/>.
/// </summary>
/// <remarks>
/// Objects are encoded as MessagePack maps with compact integer keys for known
/// typed properties and string keys for fallback/additional properties.
/// Collections become MessagePack arrays.
/// Scalar types map to their native MessagePack equivalents where possible.
/// Compact custom encodings are used for values without direct native support.
/// Enums are encoded as their underlying integer value.
///
/// <para>
/// Because MessagePack maps require an element count in the header, each call to
/// <see cref="WriteObjectValue{T}"/> serializes the object's properties into a
/// temporary <see cref="ArrayBufferWriter{Byte}"/>, determines the property count,
/// and then writes the map header followed by the buffered bytes into
/// <see cref="OutputBuffer"/>.  All other writes (scalars, arrays) go directly to
/// <see cref="OutputBuffer"/> without intermediate buffering.
/// </para>
///
/// <para>
/// <see cref="GetSerializedContent"/> is not supported; access the finished bytes
/// through <see cref="OutputBuffer"/> instead.
/// </para>
/// </remarks>
internal class MessagePackSerializationWriter : ISerializationWriter
{
    private readonly IBufferWriter<byte> _outputBuffer;
    private readonly ParsablePropertyKeyMap? _propertyKeyMap;
    private static readonly ConcurrentStack<ArrayBufferWriter<byte>> s_tempBufferPool = new();

    // Counts the number of top-level key-value pairs written to outputBuffer.
    // The parent writer reads this after serializing an object into a temp buffer
    // so it can emit the correct WriteMapHeader count.
    private int _propertyCount;

    public MessagePackSerializationWriter(IBufferWriter<byte> outputBuffer)
        : this(outputBuffer, propertyKeyMap: null)
    {
    }

    private MessagePackSerializationWriter(IBufferWriter<byte> outputBuffer, ParsablePropertyKeyMap? propertyKeyMap)
    {
        _outputBuffer = outputBuffer;
        _propertyKeyMap = propertyKeyMap;
    }

    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }

    public Action<IParsable>? OnAfterObjectSerialization { get; set; }

    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }

    /// <inheritdoc/>
    public Stream GetSerializedContent() => throw new NotImplementedException();

    // NOTE: MessagePackWriter is a ref struct and cannot be stored as a field.
    //       Every method that writes to outputBuffer creates a short-lived local
    //       MessagePackWriter, does its writes, calls Flush() to advance the
    //       IBufferWriter<byte> position, and lets the struct go out of scope.

    public void WriteStringValue(string? key, string? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value);
        w.Flush();
    }

    public void WriteBoolValue(string? key, bool? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteByteValue(string? key, byte? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteSbyteValue(string? key, sbyte? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteIntValue(string? key, int? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteLongValue(string? key, long? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteFloatValue(string? key, float? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteDoubleValue(string? key, double? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value);
        w.Flush();
    }

    public void WriteDecimalValue(string? key, decimal? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil();
        else WriteDecimal(ref w, value.Value);
        w.Flush();
    }

    public void WriteGuidValue(string? key, Guid? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil();
        else WriteGuid(ref w, value.Value);
        w.Flush();
    }

    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil();
        else WriteDateTimeOffset(ref w, value.Value);
        w.Flush();
    }

    public void WriteTimeSpanValue(string? key, TimeSpan? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil();
        else w.Write(value.Value.Ticks);
        w.Flush();
    }

    public void WriteDateValue(string? key, Date? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value.ToString());
        w.Flush();
    }

    public void WriteTimeValue(string? key, Time? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.Value.ToString());
        w.Flush();
    }

    public void WriteByteArrayValue(string? key, byte[]? value)
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil(); else w.Write(value.AsSpan());
        w.Flush();
    }

    public void WriteNullValue(string? key)
    {
        WriteKey(key);
        WriteNil(_outputBuffer);
    }

    public void WriteEnumValue<T>(string? key, T? value) where T : struct, Enum
    {
        WriteKey(key);
        var w = new MessagePackWriter(_outputBuffer);
        if (value is null) w.WriteNil();
        else w.Write(Convert.ToInt64(value.Value));
        w.Flush();
    }

    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge)
        where T : IParsable
    {
        if (value is null && (additionalValuesToMerge is null || additionalValuesToMerge.Length == 0))
        {
            WriteNullValue(key);
            return;
        }

        WriteBufferedMap(
            key,
            value is null ? null : ParsablePropertyKeyMap.For(value.GetType()),
            tempWriter =>
            {
                if (value is not null)
                {
                    OnBeforeObjectSerialization?.Invoke(value);
                    OnStartObjectSerialization?.Invoke(value, tempWriter);
                    value.Serialize(tempWriter);
                    OnAfterObjectSerialization?.Invoke(value);
                }

                if (additionalValuesToMerge is not null)
                {
                    foreach (var additional in additionalValuesToMerge)
                        additional?.Serialize(tempWriter);
                }
            });
    }

    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values) where T : IParsable
    {
        WriteKey(key);

        if (values is null)
        {
            WriteNil(_outputBuffer);
            return;
        }

        var w = new MessagePackWriter(_outputBuffer);
        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        w.WriteArrayHeader(count);
        w.Flush();

        foreach (var item in values)
            WriteBufferedMap(
                null,
                item is null ? null : ParsablePropertyKeyMap.For(item.GetType()),
                tempWriter => item?.Serialize(tempWriter));
    }

    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values)
    {
        WriteKey(key);

        if (values is null)
        {
            WriteNil(_outputBuffer);
            return;
        }

        var w = new MessagePackWriter(_outputBuffer);

        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        w.WriteArrayHeader(count);
        foreach (var item in values)
            WriteBoxedPrimitive(ref w, (object?)item);
        w.Flush();
    }

    public void WriteCollectionOfEnumValues<T>(string? key, IEnumerable<T?>? values) where T : struct, Enum
    {
        WriteKey(key);

        if (values is null)
        {
            WriteNil(_outputBuffer);
            return;
        }

        var w = new MessagePackWriter(_outputBuffer);

        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        w.WriteArrayHeader(count);

        foreach (var item in values)
        {
            if (!item.HasValue) w.WriteNil();
            else w.Write(Convert.ToInt64(item.Value));
        }
        w.Flush();
    }

    public void WriteAdditionalData(IDictionary<string, object> value)
    {
        foreach (var (k, v) in value)
            WriteAnyValue(k, v);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    /// <summary>
    /// If <paramref name="key"/> is non-null, writes the compact integer key
    /// for known typed properties or the original string key as fallback, then
    /// increments <see cref="_propertyCount"/>.
    /// </summary>
    private void WriteKey(string? key)
    {
        if (key is null) return;
        var w = new MessagePackWriter(_outputBuffer);
        if (_propertyKeyMap?.TryGetId(key, out var id) == true)
        {
            w.Write(id);
        }
        else
        {
            w.Write(key);
        }
        w.Flush();
        _propertyCount++;
    }

    /// <summary>
    /// Writes a single nil value to <paramref name="buf"/> after an optional key.
    /// </summary>
    private static void WriteNil(IBufferWriter<byte> buf)
    {
        var w = new MessagePackWriter(buf);
        w.WriteNil();
        w.Flush();
    }

    /// <summary>
    /// Writes a boxed primitive value into an already-open
    /// <paramref name="w"/>.  Does NOT flush — the caller must flush.
    /// </summary>
    private static void WriteBoxedPrimitive(ref MessagePackWriter w, object? value)
    {
        switch (value)
        {
            case null: w.WriteNil(); break;
            case string s: w.Write(s); break;
            case bool b: w.Write(b); break;
            case byte bt: w.Write(bt); break;
            case sbyte sb: w.Write(sb); break;
            case int i: w.Write(i); break;
            case long l: w.Write(l); break;
            case float f: w.Write(f); break;
            case double d: w.Write(d); break;
            case decimal dec: WriteDecimal(ref w, dec); break;
            case Guid g: WriteGuid(ref w, g); break;
            case DateTimeOffset dto: WriteDateTimeOffset(ref w, dto); break;
            case TimeSpan ts: w.Write(ts.Ticks); break;
            case DateTime dt: w.Write(dt); break;
            case Date date: w.Write(date.ToString()); break;
            case Time time: w.Write(time.ToString()); break;
            case byte[] bytes: w.Write(bytes.AsSpan()); break;
            default: w.Write(value.ToString() ?? string.Empty); break;
        }
    }

    /// <summary>
    /// Creates a child writer that inherits the serialization lifecycle callbacks,
    /// used to serialize nested objects into a temporary buffer.
    /// </summary>
    private void WriteBufferedMap(string? key, ParsablePropertyKeyMap? propertyKeyMap, Action<MessagePackSerializationWriter> writeProperties)
    {
        var tempBuffer = RentTempBuffer();
        try
        {
            var tempWriter = CreateChildWriter(tempBuffer, propertyKeyMap);
            writeProperties(tempWriter);

            WriteKey(key);
            var w = new MessagePackWriter(_outputBuffer);
            w.WriteMapHeader(tempWriter._propertyCount);
            w.WriteRaw(tempBuffer.WrittenSpan);
            w.Flush();
        }
        finally
        {
            ReturnTempBuffer(tempBuffer);
        }
    }

    private ArrayBufferWriter<byte> RentTempBuffer() =>
        s_tempBufferPool.TryPop(out var tempBuffer) ? tempBuffer : new ArrayBufferWriter<byte>();

    private void ReturnTempBuffer(ArrayBufferWriter<byte> tempBuffer)
    {
        tempBuffer.Clear();
        s_tempBufferPool.Push(tempBuffer);
    }

    private MessagePackSerializationWriter CreateChildWriter(IBufferWriter<byte> buffer, ParsablePropertyKeyMap? propertyKeyMap) => new(buffer, propertyKeyMap)
    {
        OnBeforeObjectSerialization = OnBeforeObjectSerialization,
        OnAfterObjectSerialization = OnAfterObjectSerialization,
        OnStartObjectSerialization = OnStartObjectSerialization,
    };

    private static void WriteDecimal(ref MessagePackWriter writer, decimal value)
    {
        var bytes = new byte[16];
        var bits = decimal.GetBits(value);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), bits[0]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), bits[1]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), bits[2]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, 4), bits[3]);
        writer.Write(bytes);
    }

    private static void WriteGuid(ref MessagePackWriter writer, Guid value)
    {
        var bytes = new byte[16];
        value.TryWriteBytes(bytes);
        writer.WriteExtensionFormat(new ExtensionResult(MessagePackExtensionTypeCodes.Guid, bytes));
    }

    private static void WriteDateTimeOffset(ref MessagePackWriter writer, DateTimeOffset value)
    {
        var bytes = new byte[10];
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(0, 8), value.UtcTicks);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8, 2), checked((short)value.Offset.TotalMinutes));
        writer.WriteExtensionFormat(new ExtensionResult(MessagePackExtensionTypeCodes.DateTimeOffset, bytes));
    }

    private void WriteAnyValue(string? key, object? value)
    {
        switch (value)
        {
            case null:
                WriteNullValue(key);
                break;
            case UntypedNull:
                WriteNullValue(key);
                break;
            case UntypedString us:
                WriteStringValue(key, us.GetValue());
                break;
            case UntypedBoolean ub:
                WriteBoolValue(key, ub.GetValue());
                break;
            case UntypedInteger ui:
                WriteIntValue(key, ui.GetValue());
                break;
            case UntypedLong ul:
                WriteLongValue(key, ul.GetValue());
                break;
            case UntypedFloat uf:
                WriteFloatValue(key, uf.GetValue());
                break;
            case UntypedDouble ud:
                WriteDoubleValue(key, ud.GetValue());
                break;
            case UntypedDecimal udec:
                WriteDecimalValue(key, udec.GetValue());
                break;
            case UntypedObject uobj:
                {
                    WriteBufferedMap(
                        key,
                        propertyKeyMap: null,
                        tempWriter =>
                        {
                            foreach (var (k, v) in uobj.GetValue())
                                tempWriter.WriteAnyValue(k, v);
                        });
                    break;
                }
            case UntypedArray uarr:
                {
                    var items = uarr.GetValue();
                    WriteKey(key);
                    var w = new MessagePackWriter(_outputBuffer);

                    if (!items.TryGetNonEnumeratedCount(out var count))
                    {
                        items = items.ToList();
                        count = items.Count();
                    }

                    w.WriteArrayHeader(count);
                    foreach (var item in items)
                        WriteAnyValue(null, item);
                    w.Flush();
                    break;
                }
            case IParsable parsable:
                WriteObjectValue(key, parsable);
                break;
            case string s:
                WriteStringValue(key, s);
                break;
            case bool b:
                WriteBoolValue(key, b);
                break;
            case byte bt:
                WriteByteValue(key, bt);
                break;
            case sbyte sb:
                WriteSbyteValue(key, sb);
                break;
            case int i:
                WriteIntValue(key, i);
                break;
            case long l:
                WriteLongValue(key, l);
                break;
            case float f:
                WriteFloatValue(key, f);
                break;
            case double d:
                WriteDoubleValue(key, d);
                break;
            case decimal dec:
                WriteDecimalValue(key, dec);
                break;
            case Guid g:
                WriteGuidValue(key, g);
                break;
            case DateTimeOffset dto:
                WriteDateTimeOffsetValue(key, dto);
                break;
            case TimeSpan ts:
                WriteTimeSpanValue(key, ts);
                break;
            case Date date:
                WriteDateValue(key, date);
                break;
            case Time time:
                WriteTimeValue(key, time);
                break;
            case byte[] bytes:
                WriteByteArrayValue(key, bytes);
                break;
            default:
                WriteStringValue(key, value.ToString());
                break;
        }
    }
}

internal static class MessagePackExtensionTypeCodes
{
    public const sbyte Guid = 1;
    public const sbyte DateTimeOffset = 2;
}

