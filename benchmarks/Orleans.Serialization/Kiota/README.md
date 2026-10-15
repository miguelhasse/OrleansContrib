# Orleans.Serialization.Kiota.Benchmarks

BenchmarkDotNet benchmarks for the Orleans Kiota serialization codecs.

## Overview

This project benchmarks the three supported codecs:

- `KiotaJsonCodec`
- `KiotaMessagePackCodec`
- `KiotaMemoryPackCodec`

Benchmarks are parameterized to run with compression disabled and enabled across the current Graph entity sample set.

## Benchmark classes

| Benchmark | Purpose |
|---|---|
| `KiotaCodecPerformanceBenchmarks` | Measures `Serialize`, `Deserialize`, and `DeepCopy` across codecs, compression modes, and the current Graph sample set. |
| `KiotaCodecCompressionBenchmarks` | Measures compressed vs. uncompressed serialization across codec types and entity kinds, including payload size, compression ratio, and allocation reporting. |

## Run benchmarks

From the repository root:

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*"
```

### Smaller subsets

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*Compression*"
```

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*Serialize*"
```

## Results

BenchmarkDotNet writes generated reports under the repository-level `BenchmarkDotNet.Artifacts` directory. The performance benchmark measures `Serialize`, `Deserialize`, and `DeepCopy`; the compression benchmark compares compressed and uncompressed serialization. Its report includes serialized payload sizes, compression ratios, and collection shapes. Reports are generated when you run the commands above; generated results are not maintained in this README.

The current Graph entity sample set is:

| Entity | Collection Shape |
|---|---|
| `User` | `PrimitiveAndObjectCollections` |
| `Message` | `AttachmentHeavyCollections` |
| `Chat` | `ConversationCollections` |
| `ChatMessage` | `InteractionCollections` |
| `Event` | `SchedulingCollections` |
| `Group` | `DirectoryCollections` |
| `Contact` | `MostlyPrimitiveCollections` |
| `DriveItem` | `HierarchicalCollections` |
| `Team` | `NestedAggregateCollections` |

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
