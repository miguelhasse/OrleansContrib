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

The most recent validation run produced:

- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report-github.md`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts\results\Orleans.Serialization.Kiota.Benchmarks.KiotaCodecPerformanceBenchmarks-report.html`

That report covers `Serialize`, `Deserialize`, and `DeepCopy` across all codec kinds, all sample entity kinds, and both compression modes.

`KiotaCodecCompressionBenchmarks` still adds comparison columns for `Collection Shape`, `Uncompressed Bytes`, `Compressed Bytes`, and `Compression Ratio` when you run the compression-focused filter.

Latest `Serialize` excerpt with compression disabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 9.432 us / 2.84 KB | 25.277 us / 29.98 KB | 19.977 us / 32.09 KB |
| Message | 8.024 us / 4.28 KB | 9.865 us / 21.6 KB | 9.006 us / 25.58 KB |
| Chat | 34.639 us / 9.49 KB | 59.228 us / 100.08 KB | 51.622 us / 106.83 KB |
| ChatMessage | 11.983 us / 5.05 KB | 16.148 us / 25.09 KB | 15.414 us / 31.55 KB |
| Event | 6.513 us / 2.76 KB | 12.596 us / 18.87 KB | 10.799 us / 20.41 KB |
| Group | 14.650 us / 4.63 KB | 42.360 us / 63.35 KB | 34.181 us / 75.08 KB |
| Contact | 4.866 us / 1.55 KB | 6.807 us / 7.27 KB | 5.828 us / 7.84 KB |
| DriveItem | 13.833 us / 6.77 KB | 23.887 us / 34.33 KB | 22.897 us / 50.36 KB |
| Team | 21.915 us / 6.18 KB | 49.072 us / 87.26 KB | 45.400 us / 119.98 KB |

Latest `Serialize` excerpt with compression enabled (`mean / allocated`):

| Entity | Json | MessagePack | MemoryPack |
|---|---|---|---|
| User | 37.848 us / 2.52 KB | 121.906 us / 26.68 KB | 130.072 us / 26.92 KB |
| Message | 56.704 us / 1.66 KB | 70.191 us / 18.86 KB | 82.835 us / 22.2 KB |
| Chat | 127.390 us / 5.63 KB | 206.301 us / 92.93 KB | 236.326 us / 96.13 KB |
| ChatMessage | 80.765 us / 2.75 KB | 91.718 us / 22.56 KB | 107.829 us / 27.88 KB |
| Event | 47.135 us / 1.64 KB | 79.824 us / 17.28 KB | 98.975 us / 18.01 KB |
| Group | 46.320 us / 3.78 KB | 165.173 us / 56.66 KB | 185.463 us / 65.19 KB |
| Contact | 34.975 us / 1.16 KB | 51.255 us / 6.75 KB | 66.047 us / 6.76 KB |
| DriveItem | 53.836 us / 5.48 KB | 95.077 us / 31.3 KB | 120.819 us / 45.21 KB |
| Team | 73.457 us / 4.63 KB | 193.695 us / 79.7 KB | 218.494 us / 108.84 KB |

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota tests](../../../tests/Orleans.Serialization/Kiota/README.md)
