using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Serialization;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace Orleans.Serialization;

/// <summary>
/// An Orleans generalized codec that serializes <see cref="IParsable"/> Kiota models using MemoryPack.
/// </summary>
/// <remarks>
/// <para>Implements <see cref="IGeneralizedCodec"/> so Orleans uses this codec for any
/// <see cref="IParsable"/> concrete type during grain messaging and persistence.</para>
/// <para>Implements <see cref="IGeneralizedCopier"/> so deep-copy (used by the in-process
/// silo for local calls) also goes through MemoryPack round-trip.</para>
/// <para>Implements <see cref="ITypeFilter"/> so Orleans allows Kiota model types through
/// its type safety checks.</para>
/// </remarks>
[Alias(WellKnownAlias)]
public sealed class KiotaMemoryPackCodec(IOptions<KiotaMemoryPackOptions>? options) : IGeneralizedCodec, IGeneralizedCopier, ITypeFilter
{
    private static readonly Type SelfType = typeof(KiotaMemoryPackCodec);
    private readonly bool _useCompression = options?.Value.Compression ?? false;

    /// <summary>
    /// The well-known type alias for this codec.
    /// </summary>
    public const string WellKnownAlias = "kiota-mempack";

    // Cached factory delegates keyed by concrete Kiota model type.
    private static readonly ConcurrentDictionary<Type, ParsableFactory<IParsable>> _factoryCache = new();

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) => IsSupportedType(type) ? true : null;

    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => !type.IsAbstract && !type.IsInterface && (type == SelfType || typeof(IParsable).IsAssignableFrom(type));

    /// <inheritdoc/>
    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            return;

        if (value is not IParsable parsable)
        {
            throw new NotSupportedException($"Type '{value?.GetType()?.FullName ?? "null"}' is not a Kiota IParsable type.");
        }

        // The schema type when serializing the field is the type of the codec.
        // In practice it could be any unique type as long as this codec is registered as the handler.
        // By checking against the codec type in IsSupportedType, the codec could also just be registered as an IGenericCodec.
        // Note that the codec is responsible for serializing the type of the value itself.
        writer.WriteFieldHeader(fieldIdDelta, expectedType, SelfType, WireType.TagDelimited);

        // Write the compression flag
        BoolCodec.WriteField(ref writer, 0, _useCompression);

        // Write the type name
        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeaderExpected(1, WireType.LengthPrefixed);
        writer.Session.TypeCodec.WriteLengthPrefixed(ref writer, value.GetType());

        // Write the serialized payload
        // Note that the Utf8JsonWriter and PooledBuffer could be pooled as long as they're correctly
        // reset at the end of each use.
        var bufferWriter = new BufferWriterBox<PooledBuffer>(new PooledBuffer());

        try
        {
            using (var kiotaWriter = new MemoryPackSerializationWriter(bufferWriter))
                kiotaWriter.WriteObjectValue(null, parsable);

            ReferenceCodec.MarkValueField(writer.Session);
            writer.WriteFieldHeaderExpected(2, WireType.LengthPrefixed);

            if (_useCompression)
            {
                using var compressed = BrotliCodec.Compress(bufferWriter.Value.AsReadOnlySequence());
                writer.WriteVarUInt32((uint)compressed.Memory.Length);
                writer.Write(compressed.Memory.Span);
            }
            else
            {
                writer.WriteVarUInt32((uint)bufferWriter.Value.Length);
                bufferWriter.Value.CopyTo(ref writer);
            }
        }
        finally
        {
            bufferWriter.Value.Dispose();
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc/>
    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.IsReference)
            return ReferenceCodec.ReadReference(ref reader, field.FieldType);

        if (field.FieldType != SelfType)
            throw new FieldTypeInvalidException();

        field.EnsureWireTypeTagDelimited();

        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
        object? result = null;
        Type? type = null;
        bool compressed = false;

        while (true)
        {
            var header = reader.ReadFieldHeader();

            if (header.IsEndBaseOrEndObject)
            {
                break;
            }

            switch (header.FieldIdDelta)
            {
                case 0:
                    compressed = BoolCodec.ReadValue(ref reader, header);
                    break;
                case 1:
                    ReferenceCodec.MarkValueField(reader.Session);
                    type = reader.Session.TypeCodec.ReadLengthPrefixed(ref reader);
                    break;
                case 2:
                    if (type is null)
                    {
                        throw new RequiredFieldMissingException("Serialized value is missing its type field.");
                    }

                    ReferenceCodec.MarkValueField(reader.Session);
                    var length = reader.ReadVarUInt32();

                    var tempBuffer = new PooledBuffer();
                    MemoryPackParseNode? parseNode = null;

                    try
                    {
                        reader.ReadBytes(ref tempBuffer, (int)length);

                        if (!typeof(IParsable).IsAssignableFrom(type))
                            throw new IllegalTypeException(type!.Name);

                        if (tempBuffer.Length > 0)
                        {
                            if (compressed)
                            {
                                using var decompressed = BrotliCodec.Decompress(tempBuffer.AsReadOnlySequence());
                                parseNode = new MemoryPackParseNode(decompressed.Memory);
                            }
                            else
                            {
                                parseNode = new MemoryPackParseNode(tempBuffer.ToArray());
                            }
                        }
                        else if (Nullable.GetUnderlyingType(type) is null)
                        {
                            parseNode = new MemoryPackParseNode(ReadOnlyMemory<byte>.Empty);
                        }

                        if (parseNode is not null)
                        {
                            result = parseNode.GetObjectValue(ParsableFactoryHelper.Create(type));
                        }
                    }
                    finally
                    {
                        tempBuffer.Dispose();
                    }
                    break;
                default:
                    reader.ConsumeUnknownField(header);
                    break;
            }
        }

        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
        return result!;
    }

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context)
    {
        if (context.TryGetCopy(input, out object? result))
            return result;

        if (input is not IParsable parsable)
        {
            if (input is null)
                return null;

            throw new NotSupportedException($"Type '{input?.GetType()?.FullName ?? "null"}' is not a Kiota IParsable type.");
        }

        var type = input.GetType();
        var bufferWriter = new BufferWriterBox<PooledBuffer>(new PooledBuffer());

        try
        {
            using (var kiotaWriter = new MemoryPackSerializationWriter(bufferWriter))
                kiotaWriter.WriteObjectValue(null, parsable);

            var node = new MemoryPackParseNode(bufferWriter.Value.ToArray());
            result = node.GetObjectValue(ParsableFactoryHelper.Create(type))
                ?? throw new InvalidOperationException($"Deep copy returned null for type '{type.FullName}'.");
        }
        finally
        {
            bufferWriter.Value.Dispose();
        }

        context.RecordCopy(input, result);
        return result;
    }
}
