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

- uncompressed payload bytes
- compressed payload bytes
- compression ratio

This makes the generated report easier to compare across codec types and entity kinds while BenchmarkDotNet continues to report mean time and allocations.

The short-run report generated during validation currently includes compressed serialization comparisons for:

- `User`
- `Message`
- `Event`
- `Group`
- `Contact`
- `DriveItem`
- `Team`

Representative `SerializeCompressed` results:

| Codec | Entity | Mean | Uncompressed | Compressed | Ratio | Allocated |
|---|---|---:|---:|---:|---:|---:|
| Json | User | 39.95 us | 742 B | 382 B | 48.52% | 2.52 KB |
| Json | Message | 61.41 us | 3411 B | 684 B | 79.95% | 1.66 KB |
| MessagePack | Group | 186.29 us | 8743 B | 1837 B | 78.99% | 56.66 KB |
| MemoryPack | Team | 235.48 us | 14032 B | 2587 B | 81.56% | 108.84 KB |

Example output files:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
