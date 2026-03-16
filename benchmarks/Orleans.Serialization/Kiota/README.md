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

BenchmarkDotNet writes reports under the repository-level `BenchmarkDotNet.Artifacts` directory.

`KiotaCodecCompressionBenchmarks` also adds comparison columns for:

- collection shape
- uncompressed payload bytes
- compressed payload bytes
- compression ratio

This makes the generated report easier to compare across codec types for each entity and for each collection shape while BenchmarkDotNet continues to report mean time and allocations.

The generated compression report now includes a `Collection Shape` column and keeps rows grouped by entity first so the three codec results are easier to compare side by side within the same entity family, including the `ConversationCollections` and `InteractionCollections` shapes introduced by the chat samples.

The short-run report generated during validation currently includes compressed serialization comparisons for every Graph sample entity, grouped by collection shape and then by codec within each entity:

| Collection Shape | Entity | Codec | Mean | Uncompressed | Compressed | Ratio | Allocated |
|---|---|---|---:|---:|---:|---:|---:|
| PrimitiveAndObjectCollections | User | Json | 38.05 us | 742 B | 382 B | 48.52% | 2.52 KB |
| PrimitiveAndObjectCollections | User | MessagePack | 122.93 us | 4878 B | 1455 B | 70.17% | 26.68 KB |
| PrimitiveAndObjectCollections | User | MemoryPack | 138.30 us | 7044 B | 1697 B | 75.91% | 26.92 KB |
| AttachmentHeavyCollections | Message | Json | 60.89 us | 3411 B | 684 B | 79.95% | 1.66 KB |
| AttachmentHeavyCollections | Message | MessagePack | 68.61 us | 3654 B | 821 B | 77.53% | 18.86 KB |
| AttachmentHeavyCollections | Message | MemoryPack | 81.35 us | 4499 B | 1000 B | 77.77% | 22.2 KB |
| ConversationCollections | Chat | Json | 126.42 us | 5177 B | 1172 B | 77.36% | 5.63 KB |
| ConversationCollections | Chat | MessagePack | 220.62 us | 9821 B | 2454 B | 75.01% | 92.93 KB |
| ConversationCollections | Chat | MemoryPack | 255.36 us | 13899 B | 2889 B | 79.21% | 96.13 KB |
| InteractionCollections | ChatMessage | Json | 75.14 us | 3235 B | 851 B | 73.69% | 2.75 KB |
| InteractionCollections | ChatMessage | MessagePack | 100.76 us | 3594 B | 977 B | 72.82% | 22.56 KB |
| InteractionCollections | ChatMessage | MemoryPack | 102.61 us | 5013 B | 1205 B | 75.96% | 27.88 KB |
| SchedulingCollections | Event | Json | 48.51 us | 1712 B | 544 B | 68.22% | 1.64 KB |
| SchedulingCollections | Event | MessagePack | 80.27 us | 2615 B | 965 B | 63.10% | 17.28 KB |
| SchedulingCollections | Event | MemoryPack | 98.72 us | 3655 B | 1170 B | 67.99% | 18.01 KB |
| DirectoryCollections | Group | Json | 50.61 us | 1294 B | 403 B | 68.86% | 3.78 KB |
| DirectoryCollections | Group | MessagePack | 194.11 us | 8743 B | 1837 B | 78.99% | 56.66 KB |
| DirectoryCollections | Group | MemoryPack | 184.55 us | 12286 B | 2109 B | 82.83% | 65.19 KB |
| MostlyPrimitiveCollections | Contact | Json | 41.30 us | 902 B | 466 B | 48.34% | 1.16 KB |
| MostlyPrimitiveCollections | Contact | MessagePack | 81.78 us | 1248 B | 696 B | 44.23% | 6.75 KB |
| MostlyPrimitiveCollections | Contact | MemoryPack | 91.07 us | 1994 B | 857 B | 57.02% | 6.76 KB |
| HierarchicalCollections | DriveItem | Json | 60.03 us | 1868 B | 528 B | 71.73% | 5.48 KB |
| HierarchicalCollections | DriveItem | MessagePack | 118.50 us | 4008 B | 882 B | 77.99% | 31.3 KB |
| HierarchicalCollections | DriveItem | MemoryPack | 130.19 us | 6387 B | 1071 B | 83.23% | 45.21 KB |
| NestedAggregateCollections | Team | Json | 73.83 us | 2249 B | 641 B | 71.50% | 4.63 KB |
| NestedAggregateCollections | Team | MessagePack | 218.65 us | 10044 B | 2257 B | 77.53% | 79.7 KB |
| NestedAggregateCollections | Team | MemoryPack | 233.72 us | 14035 B | 2590 B | 81.55% | 108.84 KB |

Example output files:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
