# Orleans.Serialization.Kiota.SystemTextJson

An Orleans serialization codec for Microsoft Kiota `IParsable` models using `System.Text.Json`.

## Overview

Kiota-generated clients expose models through `IParsable`, which Orleans does not serialize out of the box. This package registers a generalized Orleans codec (`KiotaJsonCodec`) that:

- accepts concrete `IParsable` types
- serializes them with Kiota's JSON serializer
- deep-copies them through the same round-trip path
- optionally Brotli-compresses the payload before it is written to Orleans wire format

## Registration

Call `AddKiotaSerializer` on the Orleans `ISerializerBuilder` during host setup:

```csharp
siloBuilder.Services.AddOrleans(orleans =>
{
    orleans.Services.AddSerializer(serializer =>
    {
        serializer.AddKiotaSerializer();
    });
});
```

### With Brotli compression

```csharp
serializer.AddKiotaSerializer(options =>
{
    options.Configure(o => o.Compression = true);
});
```

## Components

| File | Description |
|---|---|
| `KiotaJsonCodec` | Generalized Orleans codec and copier for all supported `IParsable` types. Registered with alias `kiota-json`. |
| `KiotaJsonCodecOptions` | Options type with the `Compression` flag. |
| `SerializationHostingExtensions` | `AddKiotaSerializer` registration extension for `ISerializerBuilder`. |
| `BufferJsonSerializationWriter` | Kiota JSON writer that targets an `IBufferWriter<byte>`. |
| `BrotliCodec` | Internal helper for one-shot Brotli compression and decompression. |

## Requirements

- .NET 10+
- `Microsoft.Kiota.Serialization.Json`
- `Microsoft.Orleans.Serialization`

## Related documentation

- [Repository overview](../../../../README.md)
- [Kiota tests](../../../../tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](../../../../benchmarks/Orleans.Serialization/Kiota/README.md)
