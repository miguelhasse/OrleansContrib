# Microsoft Orleans Contributions

Extensions and validation assets for Orleans serialization of Microsoft Kiota models.

## Overview

This repository currently contains Orleans serializers for Kiota `IParsable` models implemented on top of:

- `System.Text.Json`
- `MessagePack`
- `MemoryPack`

The repository also includes an xUnit v3 test project and a BenchmarkDotNet project covering codec registration, round-trips, deep-copy behavior, and Graph entity shapes, with compression enabled and disabled.

## Projects

| Project | Purpose |
|---|---|
| `src\Orleans.Serialization\Kiota\SystemTextJson` | Orleans Kiota codec backed by `System.Text.Json`. |
| `src\Orleans.Serialization\Kiota\MessagePack` | Orleans Kiota codec backed by MessagePack. |
| `src\Orleans.Serialization\Kiota\MemoryPack` | Orleans Kiota codec backed by MemoryPack. |
| `tests\Orleans.Serialization\Kiota` | xUnit v3 coverage for codec registration, round-trips, deep-copy behavior, collection-focused test models, and sample Graph payloads. |
| `benchmarks\Orleans.Serialization\Kiota` | BenchmarkDotNet performance and compression measurements for the supported codecs across Graph entity kinds. |

Register a codec through Orleans' `ISerializerBuilder`. Use `AddKiotaSerializer` for System.Text.Json, `AddKiotaMessagePackSerializer` for MessagePack, or `AddKiotaMemoryPackSerializer` for MemoryPack. Each method accepts an optional supported-type predicate and compression flag; package guides document independent serialization and copy predicates.

## Solution contents

The solution file `OrleansContrib.slnx` includes:

- `Orleans.Serialization.Kiota.SystemTextJson`
- `Orleans.Serialization.Kiota.MessagePack`
- `Orleans.Serialization.Kiota.MemoryPack`
- `Orleans.Serialization.Kiota.Tests`
- `Orleans.Serialization.Kiota.Benchmarks`

## Common commands

Run these commands from the repository root.

### Build the solution

```powershell
dotnet build .\OrleansContrib.slnx -c Release
```

Builds all three codec packages, the test project, and the benchmark project.

### Run tests

```powershell
dotnet test .\tests\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Tests.csproj -c Release
```

### Run benchmarks

```powershell
dotnet run --project .\benchmarks\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Benchmarks.csproj -c Release -- --filter "*"
```

## Benchmarks

The BenchmarkDotNet project compares serialization, deserialization, and deep-copy throughput and allocations across the three codecs, with compression on and off. Its compression-focused benchmark also reports serialized payload sizes and compression ratios. Commands and report output details are in the [benchmark guide](benchmarks/Orleans.Serialization/Kiota/README.md).

## Documentation

- [SystemTextJson package](src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](tests/Orleans.Serialization/Kiota/README.md)
- [Kiota benchmarks](benchmarks/Orleans.Serialization/Kiota/README.md)
