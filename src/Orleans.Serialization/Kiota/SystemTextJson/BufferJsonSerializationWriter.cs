using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Orleans.Serialization;

internal class BufferJsonSerializationWriter(IBufferWriter<byte> bufferWriter, KiotaJsonSerializationContext kiotaJsonSerializationContext) : ISerializationWriter
{
    private readonly Utf8JsonWriter _jsonWriter = new(bufferWriter, new JsonWriterOptions
    {
        Encoder = kiotaJsonSerializationContext.Options.Encoder,
        Indented = kiotaJsonSerializationContext.Options.WriteIndented
    });

    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }

    public Action<IParsable>? OnAfterObjectSerialization { get; set; }

    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }

    public Stream GetSerializedContent() => throw new NotImplementedException();

    public void WriteStringValue(string? key, string? value)
    {
        if (value != null)
        {
            // we want to keep empty string because they are meaningful
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value, typeof(string), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteBoolValue(string? key, bool? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(bool?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteByteValue(string? key, byte? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(byte?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteSbyteValue(string? key, sbyte? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(sbyte?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteIntValue(string? key, int? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(int?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteFloatValue(string? key, float? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(float?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteLongValue(string? key, long? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(long?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteDoubleValue(string? key, double? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(double?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteDecimalValue(string? key, decimal? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(decimal?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteGuidValue(string? key, Guid? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(Guid?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            JsonSerializer.Serialize(_jsonWriter, value.Value, typeof(DateTimeOffset?), kiotaJsonSerializationContext);
        }
    }

    /// <inheritdoc/>
    public void WriteTimeSpanValue(string? key, TimeSpan? value)
    {
        if (value.HasValue)
            WriteStringValue(key, XmlConvert.ToString(value.Value));
    }

    /// <inheritdoc/>
    public void WriteDateValue(string? key, Date? value) => WriteStringValue(key, value?.ToString());

    /// <inheritdoc/>
    public void WriteTimeValue(string? key, Time? value) => WriteStringValue(key, value?.ToString());

    /// <inheritdoc/>
    public void WriteNullValue(string? key)
    {
        if (!string.IsNullOrEmpty(key))
            _jsonWriter.WritePropertyName(key!);
        _jsonWriter.WriteNullValue();
    }

    /// <inheritdoc/>
    public void WriteEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, T? value) where T : struct, Enum
    {
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);

            if (typeof(T).IsDefined(typeof(FlagsAttribute)))
            {
                var values = Enum.GetValues<T>();
                var valueNames = new StringBuilder();

                foreach (var x in values)
                {
                    if (value.Value.HasFlag(x) && EnumHelpers.GetEnumStringValue(x) is string valueName)
                    {
                        if (valueNames.Length > 0)
                            valueNames.Append(',');
                        valueNames.Append(valueName);
                    }
                }
                WriteStringValue(null, valueNames.ToString());
            }
            else WriteStringValue(null, EnumHelpers.GetEnumStringValue(value.Value));
        }
    }

    /// <inheritdoc/>
    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values) => WriteCollectionOfPrimitiveValuesInternal(key, values);

    private void WriteCollectionOfPrimitiveValuesInternal(string? key, IEnumerable? values)
    {
        if (values != null)
        { //empty array is meaningful
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            _jsonWriter.WriteStartArray();
            foreach (var collectionValue in values)
                WriteAnyValue(null, collectionValue);
            _jsonWriter.WriteEndArray();
        }
    }

    /// <inheritdoc/>
    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values) where T : IParsable
    {
        if (values != null)
        {
            // empty array is meaningful
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            _jsonWriter.WriteStartArray();
            foreach (var item in values)
                WriteObjectValue<T>(null, item);
            _jsonWriter.WriteEndArray();
        }
    }

    /// <inheritdoc/>
    public void WriteCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, IEnumerable<T?>? values) where T : struct, Enum
    {
        if (values != null)
        { //empty array is meaningful
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            _jsonWriter.WriteStartArray();
            foreach (var item in values)
                WriteEnumValue<T>(null, item);
            _jsonWriter.WriteEndArray();
        }
    }

    /// <inheritdoc/>
    private void WriteDictionaryValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, T values) where T : IDictionary
    {
        if (values != null)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);

            _jsonWriter.WriteStartObject();
            foreach (DictionaryEntry entry in values)
            {
                if (entry.Key is not string keyStr)
                    throw new InvalidOperationException($"Error serializing dictionary value with key {key}, only string keyed dictionaries are supported.");
                WriteAnyValue(keyStr, entry.Value);
            }
            _jsonWriter.WriteEndObject();
        }
    }

    /// <inheritdoc/>
    public void WriteByteArrayValue(string? key, byte[]? value)
    {
        //empty array is meaningful
        if (value != null)
        {
            if (string.IsNullOrEmpty(key))
                _jsonWriter.WriteBase64StringValue(value);
            else
                _jsonWriter.WriteBase64String(key!, value);
        }
    }

    /// <inheritdoc/>
    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge) where T : IParsable
    {
        var filteredAdditionalValuesToMerge = (IParsable[])Array.FindAll(additionalValuesToMerge, static x => x is not null);
        if (value != null || filteredAdditionalValuesToMerge.Length > 0)
        {
            // until interface exposes WriteUntypedValue()
            var serializingUntypedNode = value is UntypedNode;
            if (!serializingUntypedNode && !string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            if (value != null)
                OnBeforeObjectSerialization?.Invoke(value);

            if (serializingUntypedNode)
            {
                var untypedNode = value as UntypedNode;
                OnStartObjectSerialization?.Invoke(untypedNode!, this);
                WriteUntypedValue(key, untypedNode);
                OnAfterObjectSerialization?.Invoke(untypedNode!);
            }
            else
            {
                var serializingScalarValue = value is IComposedTypeWrapper;
                if (!serializingScalarValue)
                    _jsonWriter.WriteStartObject();
                if (value != null)
                {
                    OnStartObjectSerialization?.Invoke(value, this);
                    value.Serialize(this);
                }
                foreach (var additionalValueToMerge in filteredAdditionalValuesToMerge)
                {
                    OnBeforeObjectSerialization?.Invoke(additionalValueToMerge!);
                    OnStartObjectSerialization?.Invoke(additionalValueToMerge!, this);
                    additionalValueToMerge!.Serialize(this);
                    OnAfterObjectSerialization?.Invoke(additionalValueToMerge);
                }
                if (!serializingScalarValue)
                    _jsonWriter.WriteEndObject();
            }
            if (value != null) OnAfterObjectSerialization?.Invoke(value);
        }
    }

    /// <inheritdoc/>
    public void WriteAdditionalData(IDictionary<string, object> value)
    {
        if (value == null)
            return;

        foreach (var dataValue in value)
            WriteAnyValue(dataValue.Key, dataValue.Value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _jsonWriter.Dispose();
        GC.SuppressFinalize(this);
    }

    private void WriteNonParsableObjectValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string? key, T value)
    {
        if (!string.IsNullOrEmpty(key))
            _jsonWriter.WritePropertyName(key!);
        _jsonWriter.WriteStartObject();
        if (value == null)
            _jsonWriter.WriteNullValue();
        else
            foreach (var oProp in value.GetType().GetProperties())
                WriteAnyValue(oProp.Name, oProp.GetValue(value));
        _jsonWriter.WriteEndObject();
    }

    private void WriteAnyValue<T>(string? key, T value)
    {
        switch (value)
        {
            case string s:
                WriteStringValue(key, s);
                break;
            case bool b:
                WriteBoolValue(key, b);
                break;
            case byte b:
                WriteByteValue(key, b);
                break;
            case sbyte b:
                WriteSbyteValue(key, b);
                break;
            case int i:
                WriteIntValue(key, i);
                break;
            case float f:
                WriteFloatValue(key, f);
                break;
            case long l:
                WriteLongValue(key, l);
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
            case TimeSpan timeSpan:
                WriteTimeSpanValue(key, timeSpan);
                break;
            case UntypedNode node:
                WriteUntypedValue(key, node);
                break;
            case IParsable parseable:
                WriteObjectValue(key, parseable);
                break;
            case Date date:
                WriteDateValue(key, date);
                break;
            case DateTime dateTime:
                WriteDateTimeOffsetValue(key, new DateTimeOffset(dateTime));
                break;
            case Time time:
                WriteTimeValue(key, time);
                break;
            case JsonElement jsonElement:
                if (!string.IsNullOrEmpty(key))
                    _jsonWriter.WritePropertyName(key!);
                jsonElement.WriteTo(_jsonWriter);
                break;
            case IDictionary dictionary:
                WriteDictionaryValue(key, dictionary);
                break;
            case IEnumerable coll:
                WriteCollectionOfPrimitiveValuesInternal(key, coll);
                break;
            case object o:
                WriteNonParsableObjectValue(key, o);
                break;
            case null:
                WriteNullValue(key);
                break;
            default:
                throw new InvalidOperationException($"Error serializing additional data value with key {key}, unknown type {value?.GetType()}");
        }
    }

    private void WriteUntypedValue(string? key, UntypedNode? value)
    {
        switch (value)
        {
            case UntypedString untypedString:
                WriteStringValue(key, untypedString.GetValue());
                break;
            case UntypedBoolean untypedBoolean:
                WriteBoolValue(key, untypedBoolean.GetValue());
                break;
            case UntypedInteger untypedInteger:
                WriteIntValue(key, untypedInteger.GetValue());
                break;
            case UntypedLong untypedLong:
                WriteLongValue(key, untypedLong.GetValue());
                break;
            case UntypedDecimal untypedDecimal:
                WriteDecimalValue(key, untypedDecimal.GetValue());
                break;
            case UntypedFloat untypedFloat:
                WriteFloatValue(key, untypedFloat.GetValue());
                break;
            case UntypedDouble untypedDouble:
                WriteDoubleValue(key, untypedDouble.GetValue());
                break;
            case UntypedObject untypedObject:
                WriteUntypedObject(key, untypedObject);
                break;
            case UntypedArray array:
                WriteUntypedArray(key, array);
                break;
            case UntypedNull:
                WriteNullValue(key);
                break;
        }
    }

    private void WriteUntypedObject(string? key, UntypedObject? value)
    {
        if (value != null)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            _jsonWriter.WriteStartObject();
            foreach (var item in value.GetValue())
                WriteUntypedValue(item.Key, item.Value);
            _jsonWriter.WriteEndObject();
        }
    }

    private void WriteUntypedArray(string? key, UntypedArray? array)
    {
        if (array != null)
        {
            if (!string.IsNullOrEmpty(key))
                _jsonWriter.WritePropertyName(key!);
            _jsonWriter.WriteStartArray();
            foreach (var item in array.GetValue())
                WriteUntypedValue(null, item);
            _jsonWriter.WriteEndArray();
        }
    }
}
