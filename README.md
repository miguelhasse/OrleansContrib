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

Latest generated BenchmarkDotNet compression results are available under `BenchmarkDotNet.Artifacts\results`.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 / .NET SDK `10.0.200`
- Intel Core i9-10900

The expanded `KiotaCodecCompressionBenchmarks` compares:

- codec types
- Graph entity kinds
- Graph entity collection shapes
- compressed vs. uncompressed payload sizes
- compression ratio
- mean execution time
- memory allocation

The generated BenchmarkDotNet report includes `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` columns, with rows grouped by `EntityKind` first and `CodecKind` second so each entity family can be compared codec-by-codec.

Short-run `SerializeCompressed` comparison excerpt from the latest generated report:

| Collection Shape | Entity | Json Mean / Ratio / Allocated | MessagePack Mean / Ratio / Allocated | MemoryPack Mean / Ratio / Allocated |
|---|---|---|---|---|
| PrimitiveAndObjectCollections | User | 38.05 us / 48.52% / 2.52 KB | 122.93 us / 70.17% / 26.68 KB | 138.30 us / 75.91% / 26.92 KB |
| AttachmentHeavyCollections | Message | 60.89 us / 79.95% / 1.66 KB | 68.61 us / 77.53% / 18.86 KB | 81.35 us / 77.77% / 22.2 KB |
| ConversationCollections | Chat | 126.42 us / 77.36% / 5.63 KB | 220.62 us / 75.01% / 92.93 KB | 255.36 us / 79.21% / 96.13 KB |
| InteractionCollections | ChatMessage | 75.14 us / 73.69% / 2.75 KB | 100.76 us / 72.82% / 22.56 KB | 102.61 us / 75.96% / 27.88 KB |
| SchedulingCollections | Event | 48.51 us / 68.22% / 1.64 KB | 80.27 us / 63.10% / 17.28 KB | 98.72 us / 67.99% / 18.01 KB |
| DirectoryCollections | Group | 50.61 us / 68.86% / 3.78 KB | 194.11 us / 78.99% / 56.66 KB | 184.55 us / 82.83% / 65.19 KB |
| MostlyPrimitiveCollections | Contact | 41.30 us / 48.34% / 1.16 KB | 81.78 us / 44.23% / 6.75 KB | 91.07 us / 57.02% / 6.76 KB |
| HierarchicalCollections | DriveItem | 60.03 us / 71.73% / 5.48 KB | 118.50 us / 77.99% / 31.3 KB | 130.19 us / 83.23% / 45.21 KB |
| NestedAggregateCollections | Team | 73.83 us / 71.50% / 4.63 KB | 218.65 us / 77.53% / 79.7 KB | 233.72 us / 81.55% / 108.84 KB |

## Documentation

- [SystemTextJson package](src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](benchmarks/Orleans.Serialization/Kiota/README.md)
