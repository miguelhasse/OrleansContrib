# Orleans.Serialization.Kiota.MemoryPack

An Orleans serialization codec for Microsoft Kiota `IParsable` models using MemoryPack.

## Overview

Kiota-generated clients expose models through `IParsable`, which Orleans does not serialize out of the box. This package registers a generalized Orleans codec (`KiotaMemoryPackCodec`) that:

- accepts concrete `IParsable` types
- serializes them with Kiota's MemoryPack serializer
- deep-copies them through the same round-trip path
- optionally Brotli-compresses the payload before it is written to Orleans wire format

## Registration

Call `AddKiotaMemoryPackSerializer` on the Orleans `ISerializerBuilder` during host setup:

```csharp
siloBuilder.Services.AddOrleans(orleans =>
{
    orleans.Services.AddSerializer(serializer =>
    {
        serializer.AddKiotaMemoryPackSerializer(isSupported: null);
    });
});
```

The simple overload accepts `Func<Type, bool>? isSupported` and an optional `compression` flag. The predicate applies to both serialization and deep-copy; `null` registers no type selectors.

### With Brotli compression

```csharp
serializer.AddKiotaMemoryPackSerializer(isSupported: null, compression: true);
```

### Restricting supported types

```csharp
serializer.AddKiotaMemoryPackSerializer(
    type => typeof(IParsable).IsAssignableFrom(type) && type.Namespace?.StartsWith("MyApp.Client") == true,
    compression: true);
```

For separate serialization and copy predicates, call `AddKiotaMemoryPackSerializer`:

```csharp
serializer.AddKiotaMemoryPackSerializer(
    isSerializable: type => typeof(IParsable).IsAssignableFrom(type),
    isCopyable: null);
```

The codec and its `IGeneralizedCodec`, `IGeneralizedCopier`, and `ITypeFilter` mappings are registered once. The `kiota-memorypack` well-known alias is added to `WellKnownTypeAliases`. A selector is registered only when its predicate is non-null.

## Components

| File | Description |
|---|---|
| `KiotaMemoryPackCodec` | Generalized Orleans codec and copier for concrete Kiota `IParsable` types. Registered with alias `kiota-memorypack`. |
| `KiotaMemoryPackOptions` | Options class with the `Compression` flag, disabled by default. |
| `SerializationHostingExtensions` | Simple and full `AddKiotaMemoryPackSerializer` registration overloads. |
| `MemoryPackSerializationWriter` / `MemoryPackParseNode` | Kiota writer and parse node backed by MemoryPack. |
| `BrotliCodec` | Shared utility for one-shot Brotli compression and decompression. |

## Requirements

- .NET 10+
- `Microsoft.Kiota.Serialization.MemoryPack`
- `Microsoft.Orleans.Serialization`

## Related documentation

- [Repository overview](../../../../README.md)
- [Kiota tests](../../../../tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](../../../../benchmarks/Orleans.Serialization/Kiota/README.md)
