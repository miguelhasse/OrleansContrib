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

The newest general benchmark run produced:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report.html`

The newest payload-size snapshot produced:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.html`

The performance report covers `Serialize`, `Deserialize`, and `DeepCopy` across all three codec kinds for the current Graph sample set with compression disabled and enabled. The compression report focuses on `Serialize` and adds payload-size columns, compression ratios, and allocation data.

`KiotaCodecCompressionBenchmarks` still adds comparison columns for `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` when you run the compression-focused filter.

Entity collections covered by the current benchmark set:

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

Latest `Serialize` excerpt from the newest performance run with compression disabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| Message | 7.92 us / 4.28 KB | 8.67 us / 5.34 KB | 7.78 us / 6.30 KB |
| User | 10.17 us / 2.84 KB | 19.97 us / 4.16 KB | 14.46 us / 5.48 KB |
| Chat | 36.13 us / 9.49 KB | 46.58 us / 13.61 KB | 39.29 us / 15.66 KB |
| ChatMessage | 12.16 us / 5.05 KB | 14.16 us / 7.32 KB | 12.40 us / 8.00 KB |
| Event | 6.54 us / 2.76 KB | 10.71 us / 4.68 KB | 8.82 us / 5.37 KB |
| Group | 14.19 us / 4.63 KB | 33.77 us / 6.44 KB | 23.73 us / 8.51 KB |
| Contact | 4.81 us / 1.55 KB | 5.93 us / 1.87 KB | 4.80 us / 2.38 KB |
| DriveItem | 13.83 us / 6.77 KB | 22.78 us / 11.19 KB | 18.03 us / 12.70 KB |
| Team | 19.77 us / 6.18 KB | 43.03 us / 8.17 KB | 30.63 us / 10.53 KB |

Latest `Serialize` excerpt from the newest performance run with compression enabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| Message | 58.15 us / 1.66 KB | 51.75 us / 3.21 KB | 60.95 us / 3.71 KB |
| User | 43.60 us / 2.52 KB | 54.64 us / 3.71 KB | 76.34 us / 3.86 KB |
| Chat | 124.06 us / 5.63 KB | 143.18 us / 10.94 KB | 155.25 us / 10.73 KB |
| ChatMessage | 75.92 us / 2.75 KB | 73.07 us / 5.88 KB | 88.37 us / 5.80 KB |
| Event | 47.01 us / 1.64 KB | 54.74 us / 3.85 KB | 61.16 us / 3.98 KB |
| Group | 46.17 us / 3.78 KB | 85.16 us / 5.14 KB | 96.19 us / 5.27 KB |
| Contact | 35.25 us / 1.16 KB | 33.87 us / 1.75 KB | 42.94 us / 1.86 KB |
| DriveItem | 54.67 us / 5.48 KB | 68.28 us / 10.20 KB | 79.62 us / 10.34 KB |
| Team | 80.34 us / 4.63 KB | 109.04 us / 6.45 KB | 120.53 us / 6.66 KB |

Latest payload-size excerpt from the newest compression-focused run (`uncompressed -> compressed bytes / ratio`):

| Collection Shape | Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|---|
| AttachmentHeavyCollections | Message | 3411 -> 684 B / 79.95% | 2751 -> 542 B / 80.30% | 3341 -> 666 B / 80.07% |
| PrimitiveAndObjectCollections | User | 742 -> 382 B / 48.52% | 987 -> 508 B / 48.53% | 2290 -> 614 B / 73.19% |
| ConversationCollections | Chat | 5177 -> 1172 B / 77.36% | 4018 -> 1260 B / 68.64% | 6600 -> 1489 B / 77.44% |
| InteractionCollections | ChatMessage | 3235 -> 851 B / 73.69% | 2252 -> 760 B / 66.25% | 3230 -> 952 B / 70.53% |
| SchedulingCollections | Event | 1712 -> 544 B / 68.22% | 1418 -> 551 B / 61.14% | 2128 -> 679 B / 68.09% |
| DirectoryCollections | Group | 1294 -> 403 B / 68.86% | 1977 -> 628 B / 68.23% | 4053 -> 716 B / 82.33% |
| MostlyPrimitiveCollections | Contact | 902 -> 466 B / 48.34% | 559 -> 412 B / 26.30% | 1104 -> 544 B / 50.72% |
| HierarchicalCollections | DriveItem | 1868 -> 528 B / 71.73% | 1604 -> 573 B / 64.28% | 3155 -> 716 B / 77.31% |
| NestedAggregateCollections | Team | 2249 -> 641 B / 71.50% | 2556 -> 766 B / 70.03% | 4922 -> 911 B / 81.49% |

These latest snapshots still show the same broad pattern across the full matrix: Json remains the throughput leader on most entities, MessagePack often keeps payload sizes close to Json, and MemoryPack trades larger uncompressed payloads for stronger compression ratios on the larger object graphs.

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
