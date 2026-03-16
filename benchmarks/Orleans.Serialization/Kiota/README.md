# Orleans.Serialization.Kiota.Benchmarks

BenchmarkDotNet benchmarks for the Orleans Kiota serialization codecs.

## Overview

This project benchmarks the three supported codecs:

- `KiotaJsonCodec`
- `KiotaMessagePackCodec`
- `KiotaMemoryPackCodec`

Benchmarks are parameterized to run with compression disabled and enabled.

## Benchmark classes

| Benchmark | Purpose |
|---|---|
| `KiotaCodecPerformanceBenchmarks` | Measures `Serialize`, `Deserialize`, and `DeepCopy` across codecs, compression modes, and the current Graph sample set. |
| `KiotaCodecCompressionBenchmarks` | Measures the cost and size trade-offs of compression for a larger `Message` payload. |

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

Example output files:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecCompressionBenchmarks-report.csv`

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
