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

Latest generated BenchmarkDotNet results are available under `BenchmarkDotNet.Artifacts\results`. The most recent focused post-tuning validation run produced `Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`, plus matching CSV and HTML exports in the same folder.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.26200.8037` / .NET SDK `10.0.200`
- Intel Core i9-10900 CPU 2.80GHz

The latest focused validation run compares:

- `Serialize`
- codec types
- compression disabled and enabled
- the tuned `User`, `Chat`, and `Team` Graph entity kinds
- payload size with and without compression
- mean execution time
- memory allocation

This focused `KiotaCodecCompressionBenchmarks` run includes `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` directly in the generated report.

Latest focused `Serialize` comparison (`mean / allocated`, compression off/on):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 10.01 us / 2.84 KB ; 39.08 us / 2.52 KB | 20.53 us / 4.16 KB ; 58.06 us / 3.71 KB | 15.27 us / 5.48 KB ; 77.77 us / 3.86 KB |
| Chat | 35.81 us / 9.49 KB ; 129.63 us / 5.63 KB | 47.77 us / 13.61 KB ; 146.86 us / 10.94 KB | 38.87 us / 15.66 KB ; 157.48 us / 10.73 KB |
| Team | 21.69 us / 6.18 KB ; 77.27 us / 4.63 KB | 41.94 us / 8.17 KB ; 112.24 us / 6.45 KB | 32.85 us / 10.53 KB ; 122.04 us / 6.66 KB |

Payload-size excerpt from the same focused run (`uncompressed -> compressed bytes / ratio`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 742 -> 382 B / 48.52% | 987 -> 508 B / 48.53% | 2290 -> 614 B / 73.19% |
| Chat | 5177 -> 1172 B / 77.36% | 4018 -> 1260 B / 68.64% | 6600 -> 1489 B / 77.44% |
| Team | 2249 -> 641 B / 71.50% | 2556 -> 766 B / 70.03% | 4922 -> 911 B / 81.49% |

These final focused numbers show the codec tuning closed a large part of the earlier MessagePack and MemoryPack overhead, especially on `MessagePack` payload size, but `Json` still leads on throughput for these three representative entities.

## Documentation

- [SystemTextJson package](src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](benchmarks/Orleans.Serialization/Kiota/README.md)
