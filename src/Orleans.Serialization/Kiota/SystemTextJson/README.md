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
        serializer.AddKiotaSerializer(isSupported: null);
    });
});
```

The simple overload has the signature `AddKiotaSerializer(Func<Type, bool>? isSupported, bool compression = false)`. The `isSupported` predicate is applied to both serialization and deep-copy. Passing `null` registers no type selector.

### With Brotli compression

```csharp
serializer.AddKiotaSerializer(null, compression: true);
```

When compression is enabled, each serialized payload is Brotli-compressed before being written to the wire and decompressed on read.

### Restricting supported types

Pass a predicate to limit which types are handled by the Kiota codec:

```csharp
serializer.AddKiotaSerializer(
    type => typeof(IParsable).IsAssignableFrom(type) && type.Namespace?.StartsWith("MyApp.Client") == true,
    compression: true);
```

A non-null predicate registers a `DelegateCodecSelector` and a `DelegateCopierSelector` under the codec name `kiota-json`.

### Separate serialization and copy predicates

Use the full overload to filter serialization and copying independently, and to configure `KiotaJsonCodecOptions` directly:

```csharp
serializer.AddKiotaSerializer(
    isSerializable: type => typeof(IParsable).IsAssignableFrom(type),
    isCopyable: null,
    configureOptions: options => options.Configure(o => o.Compression = true));
```

- `isSerializable`: if `null`, no codec selector is registered.
- `isCopyable`: if `null`, no copier selector is registered.
- `configureOptions`: optional callback receiving `OptionsBuilder<KiotaJsonCodecOptions>`.

### Registration side effects

- `KiotaJsonCodec` is registered once as a singleton and mapped to `IGeneralizedCodec`, `IGeneralizedCopier` and `ITypeFilter`. Repeated calls do not register it again.
- The `kiota-json` alias is added to `WellKnownTypeAliases`.
- Selectors are added on every call that supplies a non-null predicate.

## Components

| File | Description |
|---|---|
| `KiotaJsonCodec` | Generalized Orleans codec and copier for all `IParsable` types. Registered with the well-known alias `kiota-json`. |
| `KiotaJsonCodecOptions` | Options class. Currently exposes `Compression` (`bool`, default `false`). |
| `SerializationHostingExtensions` | `AddKiotaSerializer` extension methods for `ISerializerBuilder`: a simple overload (`isSupported`, `compression`) and a full overload (`isSerializable`, `isCopyable`, `configureOptions`). |
| `BufferJsonSerializationWriter` | An `ISerializationWriter` that writes directly into an `IBufferWriter<byte>`, avoiding intermediate stream allocations. |
| `BrotliCodec` | Internal utility for one-shot Brotli compression (`BrotliEncoder.TryCompress`) and decompression (`BrotliDecoder.TryDecompress`) over `ReadOnlySequence<byte>`. |

## Requirements

- .NET 10+
- `Microsoft.Kiota.Serialization.Json`
- `Microsoft.Orleans.Serialization`

## Related documentation

- [Repository overview](../../../../README.md)
- [Kiota tests](../../../../tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](../../../../benchmarks/Orleans.Serialization/Kiota/README.md)
