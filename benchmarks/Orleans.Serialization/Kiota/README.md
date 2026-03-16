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

The generated compression report now includes a `Collection Shape` column and keeps rows grouped by entity first so the three codec results are easier to compare side by side within the same entity family.

The short-run report generated during validation currently includes compressed serialization comparisons for every Graph sample entity, grouped by collection shape and then by codec within each entity:

| Collection Shape | Entity | Codec | Mean | Uncompressed | Compressed | Ratio | Allocated |
|---|---|---|---:|---:|---:|---:|---:|
| PrimitiveAndObjectCollections | User | Json | 48.91 us | 742 B | 382 B | 48.52% | 2.52 KB |
| PrimitiveAndObjectCollections | User | MessagePack | 139.96 us | 4878 B | 1455 B | 70.17% | 26.68 KB |
| PrimitiveAndObjectCollections | User | MemoryPack | 152.79 us | 7041 B | 1694 B | 75.94% | 26.91 KB |
| AttachmentHeavyCollections | Message | Json | 63.83 us | 3411 B | 684 B | 79.95% | 1.66 KB |
| AttachmentHeavyCollections | Message | MessagePack | 78.89 us | 3654 B | 821 B | 77.53% | 18.86 KB |
| AttachmentHeavyCollections | Message | MemoryPack | 96.17 us | 4496 B | 997 B | 77.82% | 22.2 KB |
| SchedulingCollections | Event | Json | 53.47 us | 1712 B | 544 B | 68.22% | 1.64 KB |
| SchedulingCollections | Event | MessagePack | 82.30 us | 2615 B | 965 B | 63.10% | 17.28 KB |
| SchedulingCollections | Event | MemoryPack | 117.21 us | 3652 B | 1167 B | 68.04% | 18 KB |
| DirectoryCollections | Group | Json | 49.03 us | 1294 B | 403 B | 68.86% | 3.78 KB |
| DirectoryCollections | Group | MessagePack | 168.39 us | 8743 B | 1837 B | 78.99% | 56.66 KB |
| DirectoryCollections | Group | MemoryPack | 183.65 us | 12283 B | 2106 B | 82.85% | 65.19 KB |
| MostlyPrimitiveCollections | Contact | Json | 37.31 us | 902 B | 466 B | 48.34% | 1.16 KB |
| MostlyPrimitiveCollections | Contact | MessagePack | 53.23 us | 1248 B | 696 B | 44.23% | 6.75 KB |
| MostlyPrimitiveCollections | Contact | MemoryPack | 58.96 us | 1991 B | 854 B | 57.11% | 6.75 KB |
| HierarchicalCollections | DriveItem | Json | 55.92 us | 1868 B | 528 B | 71.73% | 5.48 KB |
| HierarchicalCollections | DriveItem | MessagePack | 126.17 us | 4008 B | 882 B | 77.99% | 31.3 KB |
| HierarchicalCollections | DriveItem | MemoryPack | 168.31 us | 6384 B | 1068 B | 83.27% | 45.21 KB |
| NestedAggregateCollections | Team | Json | 108.20 us | 2249 B | 641 B | 71.50% | 4.63 KB |
| NestedAggregateCollections | Team | MessagePack | 231.69 us | 10044 B | 2257 B | 77.53% | 79.7 KB |
| NestedAggregateCollections | Team | MemoryPack | 250.37 us | 14032 B | 2587 B | 81.56% | 108.84 KB |

Example output files:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
