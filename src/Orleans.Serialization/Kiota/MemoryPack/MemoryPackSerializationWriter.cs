using MemoryPack;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Buffers;
using System.Globalization;

namespace Orleans.Serialization;

/// <summary>
/// Tag byte written before every value in the custom keyed-map binary format.
/// </summary>
internal enum ValueTag : byte
{
    Null = 0x00,
    Bool = 0x01,
    Byte = 0x02,
    SByte = 0x03,
    Int = 0x04,
    Long = 0x05,
    Float = 0x06,
    Double = 0x07,
    String = 0x08,
    Bytes = 0x09,
    Map = 0x0A, // keyed map: [int32 count] ([string key] [TaggedValue])*
    Array = 0x0B, // array:     [int32 count] ([TaggedValue])*
}

/// <summary>
/// Serializes Kiota models to a custom keyed-map binary format that uses
/// MemoryPack primitives for all individual value encodings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Wire format</b><br/>
/// Every value is prefixed with a one-byte <see cref="ValueTag"/>.
/// Objects (maps) are encoded as <c>[Tag.Map][int32 propertyCount]([serialized-key][TaggedValue])*</c>.
/// Arrays are encoded as <c>[Tag.Array][int32 count]([TaggedValue])*</c>.
/// Scalars follow the tag immediately using MemoryPack's standard encoding for
/// that type (e.g. 4 raw bytes for <c>int</c>, UTF-16 length-prefixed for <c>string</c>).
/// </para>
/// <para>
/// Because maps need a property count before any property bytes can be emitted,
/// <see cref="WriteObjectValue{T}"/> first serializes properties into a temporary
/// <see cref="ArrayBufferWriter{Byte}"/>, then writes the map header followed by
/// the buffered bytes into <see cref="OutputBuffer"/>.
/// </para>
/// <para>
/// <see cref="GetSerializedContent"/> works when <see cref="OutputBuffer"/> is an
/// <see cref="ArrayBufferWriter{Byte}"/> (which is always the case when the writer
/// is obtained from <see cref="MemoryPackSerializationWriterFactory"/>).
/// </para>
/// </remarks>
internal class MemoryPackSerializationWriter(IBufferWriter<byte> outputBuffer) : ISerializationWriter
{
    // Counts the key-value pairs written directly to outputBuffer.
    // Parent writers read this after serializing an object into a temp buffer
    // so they can emit the correct Map property count.
    private int _propertyCount;

    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }

    public Action<IParsable>? OnAfterObjectSerialization { get; set; }

    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }

    public Stream GetSerializedContent() => throw new NotSupportedException();

    public void WriteStringValue(string? key, string? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.String);
        WriteSerializedValue(value);
    }

    public void WriteBoolValue(string? key, bool? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Bool);
        WriteSerializedValue(value.Value);
    }

    public void WriteByteValue(string? key, byte? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Byte);
        WriteSerializedValue(value.Value);
    }

    public void WriteSbyteValue(string? key, sbyte? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.SByte);
        WriteSerializedValue(value.Value);
    }

    public void WriteIntValue(string? key, int? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Int);
        WriteSerializedValue(value.Value);
    }

    public void WriteLongValue(string? key, long? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Long);
        WriteSerializedValue(value.Value);
    }

    public void WriteFloatValue(string? key, float? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Float);
        WriteSerializedValue(value.Value);
    }

    public void WriteDoubleValue(string? key, double? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Double);
        WriteSerializedValue(value.Value);
    }

    public void WriteDecimalValue(string? key, decimal? value)
    {
        // Decimal has no native MemoryPack type; use string to preserve precision.
        WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    }

    public void WriteGuidValue(string? key, Guid? value)
        => WriteStringValue(key, value?.ToString());

    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value)
        => WriteStringValue(key, value?.ToString("o", CultureInfo.InvariantCulture));

    public void WriteTimeSpanValue(string? key, TimeSpan? value)
        => WriteStringValue(key, value?.ToString("c", CultureInfo.InvariantCulture));

    public void WriteDateValue(string? key, Date? value)
        => WriteStringValue(key, value?.ToString());

    public void WriteTimeValue(string? key, Time? value)
        => WriteStringValue(key, value?.ToString());

    public void WriteByteArrayValue(string? key, byte[]? value)
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Bytes);
        WriteSerializedValue(value);
    }

    public void WriteNullValue(string? key)
    {
        WriteKey(key);
        WriteTag(ValueTag.Null);
    }

    public void WriteEnumValue<T>(string? key, T? value) where T : struct, Enum
    {
        WriteKey(key);
        if (value is null) { WriteTag(ValueTag.Null); return; }
        WriteTag(ValueTag.Long);
        var enumValue = Convert.ToInt64(value.Value);
        WriteSerializedValue(enumValue);
    }

    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge)
        where T : IParsable
    {
        if (value is null && (additionalValuesToMerge is null || additionalValuesToMerge.Length == 0))
        {
            WriteKey(key);
            WriteTag(ValueTag.Null);
            return;
        }

        // Serialize all properties into a temporary buffer so we can determine
        // the property count required by the Map header before emitting any bytes.
        var tempBuf = new ArrayBufferWriter<byte>();
        var tempWriter = CreateChildWriter(tempBuf);

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

        // Write: key (if any), Tag.Map, property count, then the buffered bytes.
        WriteKey(key);
        WriteTag(ValueTag.Map);
        WriteSerializedValue(tempWriter._propertyCount);
        WriteRawBytes(tempBuf.WrittenSpan);
    }

    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values)
        where T : IParsable
    {
        WriteKey(key);

        if (values is null)
        {
            WriteTag(ValueTag.Null);
            return;
        }

        WriteTag(ValueTag.Array);
        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        WriteSerializedValue(count);

        foreach (var item in values)
        {
            var tempBuf = new ArrayBufferWriter<byte>();
            var tempWriter = CreateChildWriter(tempBuf);
            item?.Serialize(tempWriter);
            WriteTag(ValueTag.Map);
            WriteSerializedValue(tempWriter._propertyCount);
            WriteRawBytes(tempBuf.WrittenSpan);
        }
    }

    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values)
    {
        WriteKey(key);

        if (values is null)
        {
            WriteTag(ValueTag.Null);
            return;
        }

        WriteTag(ValueTag.Array);

        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        WriteSerializedValue(count);
        foreach (var item in values)
            WriteAnyValue(null, (object?)item);
    }

    public void WriteCollectionOfEnumValues<T>(string? key, IEnumerable<T?>? values)
        where T : struct, Enum
    {
        WriteKey(key);

        if (values is null)
        {
            WriteTag(ValueTag.Null);
            return;
        }

        WriteTag(ValueTag.Array);

        if (!values.TryGetNonEnumeratedCount(out var count))
        {
            values = values.ToList();
            count = values.Count();
        }

        WriteSerializedValue(count);

        foreach (var item in values)
        {
            if (!item.HasValue)
                WriteTag(ValueTag.Null);
            else
            {
                WriteTag(ValueTag.Long);
                var enumValue = Convert.ToInt64(item.Value);
                WriteSerializedValue(enumValue);
            }
        }
    }

    public void WriteAdditionalData(IDictionary<string, object> value)
    {
        foreach (var (k, v) in value)
            WriteAnyValue(k, v);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    private void WriteTag(ValueTag tag)
    {
        var span = outputBuffer.GetSpan(1);
        span[0] = (byte)tag;
        outputBuffer.Advance(1);
    }

    /// <summary>
    /// If <paramref name="key"/> is non-null, serializes the key into the output
    /// buffer and increments <see cref="_propertyCount"/>.
    /// </summary>
    private void WriteKey(string? key)
    {
        if (key is null) return;
        WriteSerializedValue(key);
        _propertyCount++;
    }

    private void WriteRawBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return;
        var dest = outputBuffer.GetSpan(bytes.Length);
        bytes.CopyTo(dest);
        outputBuffer.Advance(bytes.Length);
    }

    private void WriteSerializedValue<T>(T value)
    {
        var destination = outputBuffer;
        MemoryPackSerializer.Serialize<T, IBufferWriter<byte>>(in destination, in value);
    }

    private MemoryPackSerializationWriter CreateChildWriter(IBufferWriter<byte> buffer) => new(buffer)
    {
        OnBeforeObjectSerialization = OnBeforeObjectSerialization,
        OnAfterObjectSerialization = OnAfterObjectSerialization,
        OnStartObjectSerialization = OnStartObjectSerialization,
    };

    private void WriteAnyValue(string? key, object? value)
    {
        switch (value)
        {
            case null:
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
                    var tempBuf = new ArrayBufferWriter<byte>();
                    var tempWriter = CreateChildWriter(tempBuf);
                    foreach (var (k, v) in uobj.GetValue())
                        tempWriter.WriteAnyValue(k, v);
                    WriteKey(key);
                    WriteTag(ValueTag.Map);
                    WriteSerializedValue(tempWriter._propertyCount);
                    WriteRawBytes(tempBuf.WrittenSpan);
                    break;
                }
            case UntypedArray uarr:
                {
                    var items = uarr.GetValue();
                    WriteKey(key);
                    WriteTag(ValueTag.Array);

                    if (!items.TryGetNonEnumeratedCount(out var count))
                    {
                        items = items.ToList();
                        count = items.Count();
                    }

                    WriteSerializedValue(count);
                    foreach (var item in items)
                        WriteAnyValue(null, item);
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
