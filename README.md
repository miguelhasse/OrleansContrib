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

Latest generated BenchmarkDotNet results are available under `BenchmarkDotNet.Artifacts\results`. The most recent validation run produced `Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report-github.md`.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.26200.8037` / .NET SDK `10.0.200`
- Intel Core i9-10900 CPU 2.80GHz

The latest `KiotaCodecPerformanceBenchmarks` run compares:

- `Serialize`, `Deserialize`, and `DeepCopy`
- codec types
- compression disabled and enabled
- Graph entity kinds
- mean execution time
- memory allocation

`KiotaCodecCompressionBenchmarks` is still available for payload-size-focused runs, but the most recent published artifact is the full performance report.

`Serialize` comparison excerpt from the latest generated performance report (`mean`, compression off/on):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 9.432 / 37.848 us | 25.277 / 121.906 us | 19.977 / 130.072 us |
| Message | 8.024 / 56.704 us | 9.865 / 70.191 us | 9.006 / 82.835 us |
| Chat | 34.639 / 127.390 us | 59.228 / 206.301 us | 51.622 / 236.326 us |
| ChatMessage | 11.983 / 80.765 us | 16.148 / 91.718 us | 15.414 / 107.829 us |
| Event | 6.513 / 47.135 us | 12.596 / 79.824 us | 10.799 / 98.975 us |
| Group | 14.650 / 46.320 us | 42.360 / 165.173 us | 34.181 / 185.463 us |
| Contact | 4.866 / 34.975 us | 6.807 / 51.255 us | 5.828 / 66.047 us |
| DriveItem | 13.833 / 53.836 us | 23.887 / 95.077 us | 22.897 / 120.819 us |
| Team | 21.915 / 73.457 us | 49.072 / 193.695 us | 45.400 / 218.494 us |

The full report also captures allocation data for every `Serialize`, `Deserialize`, and `DeepCopy` run, plus HTML and CSV exports in the same results folder.

## Documentation

- [SystemTextJson package](src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](benchmarks/Orleans.Serialization/Kiota/README.md)
