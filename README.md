# Microsoft Orleans Contributions

Extensions and validation assets for Orleans serialization of Microsoft Kiota models.

## Overview

This repository currently contains Orleans serializers for Kiota `IParsable` models implemented on top of:

- `System.Text.Json`
- `MessagePack`
- `MemoryPack`

The repository also includes a dedicated xUnit v3 test project and a BenchmarkDotNet benchmark project that exercise the three codecs with compression disabled and enabled across multiple Graph entity shapes.

## Projects

| Project | Purpose |
|---|---|
| `src\Orleans.Serialization\Kiota\SystemTextJson` | Orleans Kiota codec backed by `System.Text.Json`. |
| `src\Orleans.Serialization\Kiota\MessagePack` | Orleans Kiota codec backed by MessagePack. |
| `src\Orleans.Serialization\Kiota\MemoryPack` | Orleans Kiota codec backed by MemoryPack. |
| `tests\Orleans.Serialization\Kiota` | xUnit v3 coverage for codec round-trips, deep-copy behavior, collection-focused test models, and sample Graph payloads. |
| `benchmarks\Orleans.Serialization\Kiota` | BenchmarkDotNet performance and compression measurements for the supported codecs across Graph entity kinds. |

## Solution contents

The solution file `OrleansContrib.slnx` includes:

- `Orleans.Serialization.Kiota.SystemTextJson`
- `Orleans.Serialization.Kiota.MessagePack`
- `Orleans.Serialization.Kiota.MemoryPack`
- `Orleans.Serialization.Kiota.Tests`
- `Orleans.Serialization.Kiota.Benchmarks`

## Common commands

Run these commands from the repository root.

### Build all codec packages

```powershell
dotnet build .\src\Orleans.Serialization\Kiota\SystemTextJson\Orleans.Serialization.Kiota.SystemTextJson.csproj -c Release
dotnet build .\src\Orleans.Serialization\Kiota\MessagePack\Orleans.Serialization.Kiota.MessagePack.csproj -c Release
dotnet build .\src\Orleans.Serialization\Kiota\MemoryPack\Orleans.Serialization.Kiota.MemoryPack.csproj -c Release
```

### Run tests

```powershell
dotnet test .\tests\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Tests.csproj -c Release
```

### Run benchmarks

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*"
```

## Benchmark report

Latest generated BenchmarkDotNet results are available under `BenchmarkDotNet.Artifacts\results`.

The newest general benchmark run produced `Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report-github.md`, plus matching CSV and HTML exports in the same folder. The newest payload-size snapshot remains `Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`, which adds the byte-count and compression-ratio columns.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.26200.8037` / .NET SDK `10.0.200`
- Intel Core i9-10900 CPU 2.80GHz

The latest benchmark snapshots compare:

- `Serialize`
- codec types
- compression disabled and enabled
- the current `User`, `Message`, `Chat`, `ChatMessage`, `Event`, `Group`, `Contact`, `DriveItem`, and `Team` Graph entity kinds
- payload size, compressed size, and compression ratio from the compression-focused run
- mean execution time
- memory allocation

`KiotaCodecCompressionBenchmarks` includes `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` directly in the generated report.

Entity collections covered by the latest benchmark set:

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

Latest `Serialize` comparison from the newest performance run (`mean / allocated`, compression off/on):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| Message | 7.92 us / 4.28 KB ; 58.15 us / 1.66 KB | 8.67 us / 5.34 KB ; 51.75 us / 3.21 KB | 7.78 us / 6.30 KB ; 60.95 us / 3.71 KB |
| User | 10.17 us / 2.84 KB ; 43.60 us / 2.52 KB | 19.97 us / 4.16 KB ; 54.64 us / 3.71 KB | 14.46 us / 5.48 KB ; 76.34 us / 3.86 KB |
| Chat | 36.13 us / 9.49 KB ; 124.06 us / 5.63 KB | 46.58 us / 13.61 KB ; 143.18 us / 10.94 KB | 39.29 us / 15.66 KB ; 155.25 us / 10.73 KB |
| ChatMessage | 12.16 us / 5.05 KB ; 75.92 us / 2.75 KB | 14.16 us / 7.32 KB ; 73.07 us / 5.88 KB | 12.40 us / 8.00 KB ; 88.37 us / 5.80 KB |
| Event | 6.54 us / 2.76 KB ; 47.01 us / 1.64 KB | 10.71 us / 4.68 KB ; 54.74 us / 3.85 KB | 8.82 us / 5.37 KB ; 61.16 us / 3.98 KB |
| Group | 14.19 us / 4.63 KB ; 46.17 us / 3.78 KB | 33.77 us / 6.44 KB ; 85.16 us / 5.14 KB | 23.73 us / 8.51 KB ; 96.19 us / 5.27 KB |
| Contact | 4.81 us / 1.55 KB ; 35.25 us / 1.16 KB | 5.93 us / 1.87 KB ; 33.87 us / 1.75 KB | 4.80 us / 2.38 KB ; 42.94 us / 1.86 KB |
| DriveItem | 13.83 us / 6.77 KB ; 54.67 us / 5.48 KB | 22.78 us / 11.19 KB ; 68.28 us / 10.20 KB | 18.03 us / 12.70 KB ; 79.62 us / 10.34 KB |
| Team | 19.77 us / 6.18 KB ; 80.34 us / 4.63 KB | 43.03 us / 8.17 KB ; 109.04 us / 6.45 KB | 30.63 us / 10.53 KB ; 120.53 us / 6.66 KB |

Latest payload-size excerpt from the newest compression-focused run (`uncompressed -> compressed bytes / ratio`):

| Entity | Collection Shape | Json | MessagePack | MemoryPack |
|---|---|---|---|---|
| Message | AttachmentHeavyCollections | 3411 -> 684 B / 79.95% | 2751 -> 542 B / 80.30% | 3341 -> 666 B / 80.07% |
| User | PrimitiveAndObjectCollections | 742 -> 382 B / 48.52% | 987 -> 508 B / 48.53% | 2290 -> 614 B / 73.19% |
| Chat | ConversationCollections | 5177 -> 1172 B / 77.36% | 4018 -> 1260 B / 68.64% | 6600 -> 1489 B / 77.44% |
| ChatMessage | InteractionCollections | 3235 -> 851 B / 73.69% | 2252 -> 760 B / 66.25% | 3230 -> 952 B / 70.53% |
| Event | SchedulingCollections | 1712 -> 544 B / 68.22% | 1418 -> 551 B / 61.14% | 2128 -> 679 B / 68.09% |
| Group | DirectoryCollections | 1294 -> 403 B / 68.86% | 1977 -> 628 B / 68.23% | 4053 -> 716 B / 82.33% |
| Contact | MostlyPrimitiveCollections | 902 -> 466 B / 48.34% | 559 -> 412 B / 26.30% | 1104 -> 544 B / 50.72% |
| DriveItem | HierarchicalCollections | 1868 -> 528 B / 71.73% | 1604 -> 573 B / 64.28% | 3155 -> 716 B / 77.31% |
| Team | NestedAggregateCollections | 2249 -> 641 B / 71.50% | 2556 -> 766 B / 70.03% | 4922 -> 911 B / 81.49% |

Across the full benchmark set, `Json` still leads most throughput measurements, `MessagePack` often keeps payload sizes closest to `Json`, and `MemoryPack` typically starts with larger uncompressed payloads but compresses especially well on the larger aggregate shapes.

## Documentation

- [SystemTextJson package](src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](benchmarks/Orleans.Serialization/Kiota/README.md)
